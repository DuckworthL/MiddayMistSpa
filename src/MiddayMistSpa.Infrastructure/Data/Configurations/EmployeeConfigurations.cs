using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Employee;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.EmployeeId);

        builder.Property(e => e.EmployeeCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.MiddleName).HasMaxLength(100);
        builder.Property(e => e.Gender).HasMaxLength(10).IsRequired();
        builder.Property(e => e.CivilStatus).HasMaxLength(20);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.Province).HasMaxLength(100);
        builder.Property(e => e.PostalCode).HasMaxLength(20);
        builder.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.EmergencyContactName).HasMaxLength(200);
        builder.Property(e => e.EmergencyContactPhone).HasMaxLength(50);

        // Philippine Government IDs
        builder.Property(e => e.SSSNumber).HasMaxLength(20);
        builder.Property(e => e.PhilHealthNumber).HasMaxLength(20);
        builder.Property(e => e.PagIBIGNumber).HasMaxLength(20);
        builder.Property(e => e.TINNumber).HasMaxLength(20);

        // Employment
        builder.Property(e => e.Position).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.EmploymentType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EmploymentStatus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PayrollType).HasMaxLength(50).IsRequired();

        // Therapist
        builder.Property(e => e.Specialization).HasMaxLength(200);
        builder.Property(e => e.LicenseNumber).HasMaxLength(100);

        builder.HasIndex(e => e.EmployeeCode).IsUnique();
        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(e => e.FullName);
    }
}

public class EmployeeScheduleConfiguration : IEntityTypeConfiguration<EmployeeSchedule>
{
    public void Configure(EntityTypeBuilder<EmployeeSchedule> builder)
    {
        builder.ToTable("EmployeeSchedules");
        builder.HasKey(s => s.ScheduleId);

        builder.HasOne(s => s.Employee)
            .WithMany(e => e.Schedules)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmployeeShiftConfiguration : IEntityTypeConfiguration<EmployeeShift>
{
    public void Configure(EntityTypeBuilder<EmployeeShift> builder)
    {
        builder.ToTable("EmployeeShifts");
        builder.HasKey(s => s.ShiftId);

        builder.HasIndex(s => s.EmployeeId);
        builder.HasIndex(s => s.DayOfWeek);

        builder.HasOne(s => s.Employee)
            .WithMany(e => e.EmployeeShifts)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.Duration);
    }
}

public class ShiftExceptionConfiguration : IEntityTypeConfiguration<ShiftException>
{
    public void Configure(EntityTypeBuilder<ShiftException> builder)
    {
        builder.ToTable("ShiftExceptions");
        builder.HasKey(e => e.ExceptionId);

        builder.Property(e => e.ExceptionType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(500);

        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.ExceptionDate);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.ShiftExceptions)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.IsFullDayOff);
    }
}

public class TimeOffRequestConfiguration : IEntityTypeConfiguration<TimeOffRequest>
{
    public void Configure(EntityTypeBuilder<TimeOffRequest> builder)
    {
        builder.ToTable("TimeOffRequests");
        builder.HasKey(t => t.TimeOffRequestId);

        builder.Property(t => t.LeaveType).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Reason).HasMaxLength(500);
        builder.Property(t => t.Status).HasMaxLength(50).IsRequired();
        builder.Property(t => t.RejectionReason).HasMaxLength(500);

        builder.HasOne(t => t.Employee)
            .WithMany(e => e.TimeOffRequests)
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.ApprovedByUser)
            .WithMany()
            .HasForeignKey(t => t.ApprovedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class EmployeeLeaveBalanceConfiguration : IEntityTypeConfiguration<EmployeeLeaveBalance>
{
    public void Configure(EntityTypeBuilder<EmployeeLeaveBalance> builder)
    {
        builder.ToTable("EmployeeLeaveBalances");
        builder.HasKey(l => l.LeaveBalanceId);

        builder.HasIndex(l => new { l.EmployeeId, l.Year }).IsUnique();

        builder.HasOne(l => l.Employee)
            .WithMany(e => e.LeaveBalances)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(l => l.SILRemaining);
        builder.Ignore(l => l.SickLeaveRemaining);
    }
}

public class EmployeeAdvanceConfiguration : IEntityTypeConfiguration<EmployeeAdvance>
{
    public void Configure(EntityTypeBuilder<EmployeeAdvance> builder)
    {
        builder.ToTable("EmployeeAdvances");
        builder.HasKey(a => a.AdvanceId);

        builder.Property(a => a.AdvanceType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(50).IsRequired();

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.Advances)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ApprovedByUser)
            .WithMany()
            .HasForeignKey(a => a.ApprovedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(a => a.InstallmentsRemaining);
        builder.Ignore(a => a.IsFullyPaid);
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");
        builder.HasKey(a => a.AttendanceId);

        builder.Property(a => a.Status).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.TotalHours).HasColumnType("decimal(8,2)");
        builder.Property(a => a.BreakMinutes).HasColumnType("decimal(8,2)");

        builder.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.AttendanceRecords)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
