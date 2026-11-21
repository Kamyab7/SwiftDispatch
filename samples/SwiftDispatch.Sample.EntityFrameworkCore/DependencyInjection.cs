using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.EntityFrameworkCore;
using SwiftDispatch.Sample.EntityFrameworkCore.Persistence;

namespace SwiftDispatch.Sample.EntityFrameworkCore;

public static class DependencyInjection
{
    public static IServiceCollection AddNotesServices(this IServiceCollection services, SqliteConnection connection)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        return services.AddSwiftDispatch(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.UseEntityFrameworkTransactions<ApplicationDbContext>();
        });
    }
}
