using Microsoft.EntityFrameworkCore;
using TrueMediator.Sample.EntityFrameworkCore.Entities;

namespace TrueMediator.Sample.EntityFrameworkCore.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}
