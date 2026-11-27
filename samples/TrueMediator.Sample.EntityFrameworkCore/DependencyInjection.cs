using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.EntityFrameworkCore;
using TrueMediator.Sample.EntityFrameworkCore.Persistence;

namespace TrueMediator.Sample.EntityFrameworkCore;

public static class DependencyInjection
{
    public static IServiceCollection AddNotesServices(this IServiceCollection services, SqliteConnection connection)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        return services.AddTrueMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.UseEntityFrameworkTransactions<ApplicationDbContext>();
        });
    }
}
