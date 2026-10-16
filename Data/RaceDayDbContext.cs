using Microsoft.EntityFrameworkCore;
using RaceDay.API.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RaceDay.API.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Organiser> Organisers { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<WeatherInfo> WeatherInfos { get; set; }
        public DbSet<RouteInfo> RouteInfos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Unique Constraints ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => new { e.ParticipantId, e.EventId, e.CategoryId })
                .IsUnique();

            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => new { e.EventId, e.BibNumber })
                .IsUnique()
                .HasFilter("[BibNumber] IS NOT NULL");

            // --- Check Constraints ---
            modelBuilder.Entity<User>()
                .ToTable(t => t.HasCheckConstraint("CHK_User_Role", "[Role] IN ('Organiser', 'Participant')"));

            modelBuilder.Entity<Participant>()
                .ToTable(t => t.HasCheckConstraint("CHK_Participant_Gender", "[Gender] IN ('Male', 'Female', 'Other')"));

            modelBuilder.Entity<Event>()
                .ToTable(t => t.HasCheckConstraint("CHK_RegistrationDeadline", "[RegistrationDeadline] <= [EventDate]"));

            modelBuilder.Entity<Category>()
                .ToTable(t => t.HasCheckConstraint("CHK_CategoryType", "[CategoryType] IN ('Running', 'Walking', 'Cycling')"));

            modelBuilder.Entity<Enrolment>()
                .ToTable(t => t.HasCheckConstraint("CHK_Enrolment_Status", "[Status] IN ('Pending', 'Confirmed', 'Paid', 'Cancelled', 'Withdrawn')"));

            // --- Relationships & Cascade Deletes ---
            modelBuilder.Entity<Organiser>()
                .HasOne(o => o.User)
                .WithOne(u => u.Organiser)
                .HasForeignKey<Organiser>(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Participant>()
                .HasOne(p => p.User)
                .WithOne(u => u.Participant)
                .HasForeignKey<Participant>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Participant)
                .WithMany(p => p.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Participant)
                .WithMany(p => p.Results)
                .HasForeignKey(r => r.ParticipantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Results)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Results)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Verifier)
                .WithMany(o => o.VerifiedResults)
                .HasForeignKey(r => r.VerifiedBy)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WeatherInfo>()
                .HasOne(w => w.Event)
                .WithMany(e => e.WeatherInfos)
                .HasForeignKey(w => w.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteInfo>()
                .HasOne(r => r.Event)
                .WithMany(e => e.RouteInfos)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}