using System.Runtime.CompilerServices;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TrueMediator.Contracts;

namespace TrueMediator.EntityFrameworkCore;

/// <summary>A transaction boundary selected and registered by the TrueMediator source generator.</summary>
/// <remarks>
/// Handlers own SaveChangesAsync. The outermost behavior owns its transaction; nested requests
/// using the same context join it. A failed nested participant prevents the owner from committing.
/// Automatic handler retries are not performed. Do not add this behavior as an open behavior.
/// </remarks>
public sealed class EntityFrameworkTransactionBehavior<TRequest, TResponse, TContext>(TContext context)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TContext : DbContext
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.Database.IsRelational())
        {
            throw new NotSupportedException("TrueMediator transactions require an EF Core relational provider.");
        }
        if (Transaction.Current != null)
        {
            throw new NotSupportedException("Ambient transactions are not supported by the TrueMediator EF integration. Use a DbContext transaction instead.");
        }

        if (context.Database.CreateExecutionStrategy().RetriesOnFailure && ExecutionStrategy.Current == null)
        {
            throw new NotSupportedException("Automatic EF retries require an application-owned execution strategy around the complete operation. TrueMediator does not replay handlers or their external side effects.");
        }

        if (context.Database.CurrentTransaction is { } existing)
        {
            TransactionParticipation participation = TransactionParticipation.For(existing);
            try
            {
                participation.ThrowIfFailed();
                TResponse response = await next().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return response;
            }
            catch
            {
                participation.Failed = true;
                throw;
            }
        }

        IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        bool failed = false;
        TransactionParticipation owner = TransactionParticipation.For(transaction);
        try
        {
            TResponse response = await next().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            owner.ThrowIfFailed();
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch
        {
            failed = true;
            // Cancellation must not prevent cleanup, and rollback errors must not hide the cause.
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Disposal makes a final cleanup attempt; preserve the original exception.
            }
            throw;
        }
        finally
        {
            if (failed)
            {
                try
                {
                    await transaction.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Preserve the original failure when final cleanup also fails.
                }
            }
            else
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}

internal sealed class TransactionParticipation
{
    private static readonly ConditionalWeakTable<IDbContextTransaction, TransactionParticipation> Participants = new();
    internal bool Failed { get; set; }
    internal static TransactionParticipation For(IDbContextTransaction transaction) => Participants.GetValue(transaction, static _ => new());

    internal void ThrowIfFailed()
    {
        if (Failed)
        {
            throw new InvalidOperationException("A nested transactional request failed. The owning transaction cannot commit and must be rolled back.");
        }
    }
}
