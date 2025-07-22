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
        public DbSet<PastTestPaper> PastTestPapers { get; set; }
        public DbSet<PastTestPaperCategory> PastTestPaperCategories { get; set; }
        public DbSet<MockTestResult> MockTestResults { get; set; }
        public DbSet<MockTestQuestion> MockTestQuestions { get; set; }
        public DbSet<MockTestSourceDocument> MockTestSourceDocuments { get; set; }
        public DbSet<Suzy.Models.TaskItem> TaskItems { get; set; }
        // Chat Analytics models
        public DbSet<ChatConversation> ChatConversations { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<StudyAnalytics> StudyAnalytics { get; set; } = null!;
        public DbSet<WeeklySummary> WeeklySummaries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- Configuration for Note & Category Relationship ---
            builder.Entity<NoteCategory>()
                .HasKey(nc => new { nc.NoteId, nc.CategoryId });

            builder.Entity<NoteCategory>()
                .HasOne(nc => nc.Note)
                .WithMany(n => n.NoteCategories)
                .HasForeignKey(nc => nc.NoteId);

            builder.Entity<NoteCategory>()
                .HasOne(nc => nc.Category)
                .WithMany(c => c.NoteCategories)
                .HasForeignKey(nc => nc.CategoryId);
            

            // --- ADDED: Configuration for PastTestPaper & Category Relationship ---
            builder.Entity<PastTestPaperCategory>()
                .HasKey(pc => new { pc.PastTestPaperId, pc.CategoryId });

            builder.Entity<PastTestPaperCategory>()
                .HasOne(pc => pc.PastTestPaper)
                .WithMany(p => p.PastTestPaperCategories)
                .HasForeignKey(pc => pc.PastTestPaperId);

            builder.Entity<PastTestPaperCategory>()
                .HasOne(pc => pc.Category)
                .WithMany(c => c.PastTestPaperCategories)
                .HasForeignKey(pc => pc.CategoryId);
        }
    }
}