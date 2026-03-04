using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Appointment;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.AppointmentId);

        builder.Property(a => a.AppointmentNumber).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(50).IsRequired();
        builder.Property(a => a.BookingSource).HasMaxLength(50);
        builder.Property(a => a.CustomerNotes).HasMaxLength(1000);
        builder.Property(a => a.TherapistNotes).HasMaxLength(1000);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);

        builder.HasIndex(a => a.AppointmentNumber).IsUnique();
        builder.HasIndex(a => a.CustomerId);
        builder.HasIndex(a => a.TherapistId);
        builder.HasIndex(a => a.AppointmentDate);
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Customer)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Therapist)
            .WithMany()
            .HasForeignKey(a => a.TherapistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Room)
            .WithMany(r => r.Appointments)
            .HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(a => a.IsCompleted);
        builder.Ignore(a => a.IsCancelled);
        builder.Ignore(a => a.IsNoShow);
    }
}

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.RoomId);

        builder.Property(r => r.RoomName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.RoomCode).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.RoomType).HasMaxLength(50).IsRequired();

        builder.HasIndex(r => r.RoomCode).IsUnique();
    }
}

public class AppointmentServiceItemConfiguration : IEntityTypeConfiguration<AppointmentServiceItem>
{
    public void Configure(EntityTypeBuilder<AppointmentServiceItem> builder)
    {
        builder.ToTable("AppointmentServiceItems");
        builder.HasKey(x => x.AppointmentServiceItemId);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.Quantity).IsRequired().HasDefaultValue(1);

        builder.HasIndex(x => x.AppointmentId);

        builder.HasOne(x => x.Appointment)
            .WithMany(a => a.ServiceItems)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Service)
            .WithMany()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
