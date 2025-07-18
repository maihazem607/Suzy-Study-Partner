using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Suzy.Models;

namespace Suzy.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Note> Notes { get; set; } = null!;
        public DbSet<TodoItem> TodoItems { get; set; } = null!;
        public DbSet<StudySession> StudySessions { get; set; } = null!;
        public DbSet<StudySessionParticipant> StudySessionParticipants { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure TodoItem
            builder.Entity<TodoItem>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StudySessionId);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure StudySession
            builder.Entity<StudySession>(entity =>
            {
                entity.HasIndex(e => e.CreatorUserId);
                entity.HasIndex(e => e.IsPublic);
                entity.HasIndex(e => e.InviteCode).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Configure one-to-many relationship with TodoItems
                entity.HasMany(s => s.TodoItems)
                      .WithOne()
                      .HasForeignKey(t => t.StudySessionId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Configure one-to-many relationship with Participants
                entity.HasMany(s => s.Participants)
                      .WithOne(p => p.StudySession)
                      .HasForeignKey(p => p.StudySessionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure StudySessionParticipant
            builder.Entity<StudySessionParticipant>(entity =>
            {
                entity.HasIndex(e => new { e.StudySessionId, e.UserId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
