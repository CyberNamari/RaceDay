using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsUnicode(false).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Venue>(e =>
        {
            e.ToTable("Venues");
            e.HasKey(x => x.VenueID);
            e.Property(x => x.VenueName).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Address).HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Province).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(50).IsUnicode(false).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.StartTime).HasColumnType("time");
            e.Property(x => x.Description).HasColumnType("varchar(max)");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue("Scheduled");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserID).HasConstraintName("FK_Events_Organiser")
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Venue).WithMany(v => v.Events)
                .HasForeignKey(x => x.VenueID).HasConstraintName("FK_Events_Venue")
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(50).IsUnicode(false).IsRequired();
            e.Property(x => x.DistanceKM).HasPrecision(5, 2);
            e.Property(x => x.EntryFee).HasPrecision(8, 2).HasDefaultValue(0m);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID).HasConstraintName("FK_Categories_Event")
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Registration>(e =>
        {
            e.ToTable("Registrations");
            e.HasKey(x => x.RegistrationID);
            e.Property(x => x.BibNumber).HasMaxLength(10).IsUnicode(false);
            e.Property(x => x.RegistrationDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.PaymentStatus).HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue("Pending");
            e.HasOne(x => x.Participant).WithMany(u => u.Registrations)
                .HasForeignKey(x => x.ParticipantID).HasConstraintName("FK_Registrations_Participant")
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Category).WithMany(c => c.Registrations)
                .HasForeignKey(x => x.CategoryID).HasConstraintName("FK_Registrations_Category")
                .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID })
                .IsUnique().HasDatabaseName("UQ_Registrations_Participant_Category");
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue("Finished");
            e.HasOne(x => x.Registration).WithOne(r => r.Result)
                .HasForeignKey<Result>(x => x.RegistrationID).HasConstraintName("FK_Results_Registration")
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}