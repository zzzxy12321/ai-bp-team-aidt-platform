using AIDT.Application.Abstractions;
using AIDT.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Persistence;

public sealed class EfApplicationTransaction : IApplicationTransaction
{
    private readonly AidtDbContext _dbContext;
    private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _transaction;
    private bool _completed;

    public EfApplicationTransaction(
        AidtDbContext dbContext,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        _dbContext = dbContext;
        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
            _completed = true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(cancellationToken);
            throw new ConflictException(ErrorCodes.Conflict, "数据已被其他操作修改，请刷新后重试。");
        }
        catch (DbUpdateException)
        {
            await RollbackAsync(cancellationToken);
            throw new ConflictException(ErrorCodes.Conflict, "数据写入冲突，请刷新后重试。");
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (!_completed)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _completed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
    }
}

public sealed class EfUnitOfWork(AidtDbContext dbContext) : IUnitOfWork
{
    public async Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfApplicationTransaction(dbContext, transaction);
    }
}
