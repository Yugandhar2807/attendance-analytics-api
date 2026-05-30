using Attendance.Domain.Entities;
using Attendance.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("dim_user", "core");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("user_id")
            .ValueGeneratedOnAdd();

        b.Property(x => x.ExternalRef)
            .HasColumnName("external_ref")
            .HasMaxLength(64)
            .IsRequired()
            .HasConversion(
                v => v.Value,
                v => ExternalRef.Parse(v));

        b.HasIndex(x => x.ExternalRef).IsUnique();

        b.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        b.Property(x => x.Role).HasColumnName("role").HasConversion<int>();
        b.Property(x => x.JoinedOn).HasColumnName("joined_on");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
