using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Application.Contracts.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public UnitOfWork(AppDbContext context) => _context = context;
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => 
        _context.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw; 
        }
    }
}