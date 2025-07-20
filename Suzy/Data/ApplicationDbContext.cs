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
        public DbSet<StudyTimerSession> StudyTimerSessions { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<NoteCategory> NoteCategories { get; set; } = null!;

        // Chat Analytics models
        public DbSet<ChatConversation> ChatConversations { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<StudyAnalytics> StudyAnalytics { get; set; } = null!;
        public DbSet<WeeklySummary> WeeklySummaries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<NoteCategory>()
                .HasKey(nc => new { nc.NoteId, nc.CategoryId });

            builder.Entity<NoteCategory>()
                .HasOne(nc => nc.Note)
                .WithMany()
                .HasForeignKey(nc => nc.NoteId);

            builder.Entity<NoteCategory>()
                .HasOne(nc => nc.Category)
                .WithMany(c => c.NoteCategories)
                .HasForeignKey(nc => nc.CategoryId);
        }
    }
}
