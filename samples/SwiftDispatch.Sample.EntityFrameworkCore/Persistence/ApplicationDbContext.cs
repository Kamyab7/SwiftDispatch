using Microsoft.EntityFrameworkCore;
using SwiftDispatch.Sample.EntityFrameworkCore.Entities;

namespace SwiftDispatch.Sample.EntityFrameworkCore.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}
