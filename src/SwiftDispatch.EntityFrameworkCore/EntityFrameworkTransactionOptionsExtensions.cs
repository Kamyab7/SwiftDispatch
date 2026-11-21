using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SwiftDispatch.EntityFrameworkCore;

/// <summary>Build-time configuration for generated EF transaction behaviors.</summary>
public static class EntityFrameworkTransactionOptionsExtensions
{
    /// <summary>
    /// Selects the default context for requests marked [Transactional]. Place this call directly
    /// inside the AddSwiftDispatch lambda. The generator reads it; the lambda is not executed.
    /// Register TContext separately in DI. Unmarked requests are unaffected.
    /// </summary>
    public static SwiftDispatchOptions UseEntityFrameworkTransactions<TContext>(this SwiftDispatchOptions options)
        where TContext : DbContext => options;
}
