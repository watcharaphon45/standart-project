using System.Data;
using clean.Application.Common.Interfaces;
using clean.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace clean.Infrastructure.Services;

public sealed class AtomicWriter(OrganizationDbContext organization, SystemDbContext system) : IAtomicWriter
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await using var transaction = await organization.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await system.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);
        try
        {
            var result = await action();
            await system.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await system.Database.UseTransactionAsync(null, CancellationToken.None);
        }
    }
}
