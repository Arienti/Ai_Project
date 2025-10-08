using Ai_Project.DTO;
using Microsoft.EntityFrameworkCore;

namespace Ai_Project.Database
{
    public class DataBase : DbContext
    {
        public DbSet<TopicDTO> Topics { get; set; } = null!;
        public DbSet<MessagesDTO> Messages { get; set; } = null!;

        // Constructor to accept options (recommended)
        public DataBase(DbContextOptions<DataBase> options) : base(options)
        {
            Database.EnsureCreated();
        }

        // Optional parameterless constructor for quick usage - configure connection here
        public DataBase()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback connection string if options not provided externally
                optionsBuilder.UseSqlite("Data Source=ai_project.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Topic configuration
            modelBuilder.Entity<TopicDTO>(entity =>
            {
                entity.HasKey(t => t.ID);

                entity.Property(t => t.ID)
                      .HasColumnName("id")
                      .ValueGeneratedOnAdd();

                entity.Property(t => t.Topic)
                      .HasColumnName("topic")
                      .IsRequired();

                entity.Property(t => t.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasMany(t => t.Messages)
                      .WithOne(m => m.TopicDTO)
                      .HasForeignKey(m => m.TopicId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Message configuration
            modelBuilder.Entity<MessagesDTO>(entity =>
            {
                entity.HasKey(m => m.ID);

                entity.Property(m => m.ID)
                      .HasColumnName("id")
                      .ValueGeneratedOnAdd();

                entity.Property(m => m.Content)
                      .HasColumnName("content")
                      .IsRequired();

                entity.Property(m => m.Sender)
                      .HasColumnName("sender")
                      .IsRequired();

                entity.Property(m => m.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(m => m.TopicId)
                      .HasColumnName("topic_id")
                      .IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }

        public new void Dispose()
        {
           // base.Dispose();
        }
    }
}
