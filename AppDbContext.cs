using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Store enums as readable text instead of numbers
        b.Entity<User>().Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        b.Entity<Event>().Property(e => e.EventType).HasConversion<string>().HasMaxLength(20);

        // No two users with the same email
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // Users 1 - * Events
        b.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany(u => u.Events)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Events 1 - * Categories
        b.Entity<Category>()
            .HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Events 1 - * Routes
        b.Entity<RaceRoute>()
            .HasOne(r => r.Event).WithMany(e => e.Routes)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Users 1 - * Enrolments (Restrict avoids SQL Server "multiple cascade paths" error)
        b.Entity<Enrolment>()
            .HasOne(e => e.User).WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categories 1 - * Enrolments
        b.Entity<Enrolment>()
            .HasOne(e => e.Category).WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // A participant can only enrol once per category
        b.Entity<Enrolment>().HasIndex(e => new { e.UserId, e.CategoryId }).IsUnique();

        // Enrolments 1 - 1 Results
        b.Entity<Result>()
            .HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}