using TimeSeriesForecaster.Application.Contracts.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public UnitOfWork(AppDbContext context) => _context = context;
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => 
        _context.SaveChangesAsync(cancellationToken);
}
