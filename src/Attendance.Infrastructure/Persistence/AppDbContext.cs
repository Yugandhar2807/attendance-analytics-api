using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Persistence;

/// <summary>
/// Per-tenant DbContext. The connection string is supplied at construction time
/// by <see cref="ScopedDbContextFactory"/>, which reads the current tenant from
/// <see cref="Attendance.Application.Tenancy.ITenantContext"/>.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Punch> Punches => Set<Punch>();
    public DbSet<DailyAttendance> DailyAttendance => Set<DailyAttendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("core");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
