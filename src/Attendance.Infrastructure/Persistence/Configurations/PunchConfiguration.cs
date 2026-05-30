using Attendance.Domain.Entities;
using Attendance.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

internal sealed class PunchConfiguration : IEntityTypeConfiguration<Punch>
{
    public void Configure(EntityTypeBuilder<Punch> b)
    {
        b.ToTable("fact_punch", "core");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id).HasColumnName("punch_id").ValueGeneratedOnAdd();
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.PunchAt).HasColumnName("punch_at");
        b.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.Direction)
            .HasColumnName("direction")
            .HasMaxLength(3)
            .HasConversion(d => d.ToWireString(), s => DirectionExtensions.Parse(s));
        b.Property(x => x.BatchId).HasColumnName("batch_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.IngestedAt).HasColumnName("ingested_at");

        // Uniqueness key the import depends on
        b.HasIndex(x => new { x.UserId, x.PunchAt, x.DeviceId, x.Direction })
            .IsUnique()
            .HasDatabaseName("uq_fact_punch_event");

        b.HasIndex(x => x.BatchId).HasDatabaseName("ix_fact_punch_batch");
    }
}
