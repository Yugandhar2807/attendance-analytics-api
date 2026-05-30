using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

internal sealed class DailyAttendanceConfiguration : IEntityTypeConfiguration<DailyAttendance>
{
    public void Configure(EntityTypeBuilder<DailyAttendance> b)
    {
        b.ToTable("fact_daily_attendance", "mart");
        b.HasKey(x => new { x.UserId, x.AttendanceDate });

        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.AttendanceDate).HasColumnName("attendance_date");
        b.Property(x => x.FirstIn).HasColumnName("first_in");
        b.Property(x => x.LastOut).HasColumnName("last_out");
        b.Property(x => x.DurationMinutes).HasColumnName("duration_minutes");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
        b.Property(x => x.ClosedAt).HasColumnName("closed_at");
    }
}
