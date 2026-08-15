using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.Models;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class DataPointRepository : IDataPointRepository
{
    private readonly AppDbContext _context;

    public DataPointRepository(AppDbContext context)
    {
        _context = context;
    }

    // Bu metotlar sadece context'e ekleme/çıkarma/güncelleme yapıyor asıl işlemler serviste olacak.
    public void CreateDataPoint(DataPoint dataPoint) => _context.DataPoints.Add(dataPoint);
    public void RemoveDataPoint(DataPoint dataPoint)  => _context.DataPoints.Remove(dataPoint);
    public void UpdateDataPoint(DataPoint dataPoint) => _context.DataPoints.Update(dataPoint);

    public async Task<DataPoint?> GetDataPointByIdAsync(int id, bool trackChanges)
    {
        IQueryable<DataPoint> query = trackChanges 
            ? _context.DataPoints 
            : _context.DataPoints.AsNoTracking();

        return await query.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<DataPoint>> GetDataPointsAsync(int datasetId, DateTime? startDate = null, DateTime? endDate = null, int? limit = null, int? maxPoints = null)
    {
        var query = _context.DataPoints
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId);

        if (startDate.HasValue)
        {
            query = query.Where(d => d.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(d => d.Timestamp <= endDate.Value);
        }

        query = query.OrderBy(d => d.Timestamp);

        if (maxPoints is > 0)
        {
            var totalCount = await query.CountAsync();
            if (totalCount > maxPoints.Value)
            {
                return await GetDownsampledDataPointsAsync(datasetId, maxPoints.Value);
            }
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    // Büyük dataset'lerde grafik için tüm noktaları göndermek yerine, veriyi zaman sırasına göre
    // @maxPoints kovaya bölüp her kovadan min ve max değerli gerçek noktayı döndürür (şekli AVG'den daha iyi korur,
    // ani sıçramalar kaybolmaz). Bir kovada outlier varsa, kovadan seçilen noktalar da outlier olarak işaretlenir.
    private async Task<IEnumerable<DataPoint>> GetDownsampledDataPointsAsync(int datasetId, int maxPoints, CancellationToken cancellationToken = default)
    {
        return await _context.DataPoints
            .FromSql($"""
                WITH bucketed AS (
                    SELECT "Id", "DatasetId", "Timestamp", "Value", "IsOutlier", "CreatedAt",
                           NTILE({maxPoints}) OVER (ORDER BY "Timestamp") AS bucket
                    FROM "DataPoints"
                    WHERE "DatasetId" = {datasetId}
                ),
                ranked AS (
                    SELECT *,
                           ROW_NUMBER() OVER (PARTITION BY bucket ORDER BY "Value" ASC, "Timestamp" ASC) AS min_rank,
                           ROW_NUMBER() OVER (PARTITION BY bucket ORDER BY "Value" DESC, "Timestamp" ASC) AS max_rank,
                           BOOL_OR("IsOutlier") OVER (PARTITION BY bucket) AS bucket_has_outlier
                    FROM bucketed
                )
                SELECT "Id", "DatasetId", "Timestamp", "Value", bucket_has_outlier AS "IsOutlier", "CreatedAt"
                FROM ranked
                WHERE min_rank = 1 OR max_rank = 1
                ORDER BY "Timestamp"
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
    public async Task<IEnumerable<DataPoint>> GetDataPointsPagedAsync(int datasetId, int page, int pageSize)
    {
        return await _context.DataPoints
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId)
            .OrderBy(d => d.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    public async Task<IEnumerable<DataPoint>> GetOutliersAsync(int datasetId)
    {
        return await _context.DataPoints
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId && d.IsOutlier)
            .OrderBy(d => d.Timestamp)
            .ToListAsync();
    }
    public Task<int> GetDataPointsCountAsync(int datasetId) => _context.DataPoints.CountAsync(d => d.DatasetId == datasetId);

    public async Task RemoveDataPointsForDatasetAsync(int datasetId, CancellationToken cancellationToken = default)
    {
        await _context.DataPoints
            .Where(d => d.DatasetId == datasetId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task BulkCopyDataPointsAsync(IEnumerable<DataPoint> dataPoints, CancellationToken cancellationToken = default)
    {
        var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;

        if (wasClosed)
        {
            await conn.OpenAsync(cancellationToken);
        }

        try
        {
            await using var writer = await conn.BeginBinaryImportAsync("COPY \"DataPoints\" (\"DatasetId\", \"Timestamp\", \"Value\", \"IsOutlier\", \"CreatedAt\") FROM STDIN (FORMAT BINARY)", cancellationToken);
            
            foreach (var dataPoint in dataPoints)
            {
                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(dataPoint.DatasetId, NpgsqlDbType.Integer);
                await writer.WriteAsync(dataPoint.Timestamp, NpgsqlDbType.TimestampTz);
                await writer.WriteAsync(dataPoint.Value, NpgsqlDbType.Numeric);
                await writer.WriteAsync(dataPoint.IsOutlier, NpgsqlDbType.Boolean);
                await writer.WriteAsync(dataPoint.CreatedAt, NpgsqlDbType.TimestampTz);
            }
            await writer.CompleteAsync(cancellationToken);
        }
        finally
        {
            if (wasClosed)
                await conn.CloseAsync();
        }
    }

    public async Task<DatasetStatistics> GetStatisticsAsync(int datasetId, CancellationToken cancellationToken = default)
    {
        var result = await _context.Database
            .SqlQuery<DatasetStatistics>($"""
                SELECT
                    AVG("Value") AS "Mean",
                    (PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY "Value"))::numeric AS "Median",
                    (STDDEV_POP("Value"))::numeric AS "StdDev",
                    MIN("Value") AS "Min",
                    MAX("Value") AS "Max",
                    MIN("Timestamp") AS "MinDate",
                    MAX("Timestamp") AS "MaxDate",
                    NULL::numeric AS "CoefficientOfVariation"
                FROM "DataPoints"
                WHERE "DatasetId" = {datasetId}
                """)
            .FirstOrDefaultAsync(cancellationToken);

        if (result?.MinDate is null || result.MaxDate is null)
        {
            return new DatasetStatistics();
        }

        var mean = result.Mean ?? 0m;
        var stdDev = result.StdDev ?? 0m;
        var coefficientOfVariation = mean != 0 ? stdDev / mean : 0m;

        return new DatasetStatistics
        {
            Mean = (int?)mean,
            Median = (int?)(result.Median ?? 0m),
            StdDev = stdDev,
            Min = result.Min ?? 0m,
            Max = result.Max ?? 0m,
            MinDate = result.MinDate,
            MaxDate = result.MaxDate,
            CoefficientOfVariation = coefficientOfVariation
        };
    }
}
