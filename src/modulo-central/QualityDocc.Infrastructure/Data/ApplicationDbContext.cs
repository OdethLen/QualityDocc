using Microsoft.EntityFrameworkCore;
using QualityDocc.Domain.Entities;

namespace QualityDocc.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Mapeo de tus entidades a conjuntos de datos (Tablas)
        public DbSet<Document> Documents { get; set; } // ¡Faltaba registrar la tabla maestra!
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<ApprovalFlow> ApprovalFlows { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo exacto a los nombres de tus tablas en SQL Server
            modelBuilder.Entity<Document>().ToTable("Documents");
            modelBuilder.Entity<DocumentVersion>().ToTable("DocumentVersions");
            modelBuilder.Entity<ApprovalFlow>().ToTable("ApprovalFlows");

            // Configurar relación: Un Documento tiene muchas Versiones
            modelBuilder.Entity<QualityDocc.Domain.Entities.DocumentVersion>(entity =>
            {
                entity.HasOne<QualityDocc.Domain.Entities.Document>(v => v.Document)
                      .WithMany(d => d.Versions)
                      .HasForeignKey(v => v.DocumentId)
                      .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);
            });

            // Indicamos que los campos booleanos se comporten como BIT de SQL
            modelBuilder.Entity<DocumentVersion>()
                .Property(d => d.IsDeleted)
                .HasColumnType("bit");

            modelBuilder.Entity<Document>()
                .Property(d => d.Status)
                .HasColumnType("bit")
                .HasDefaultValue(true); // Tu regla de bit NOT NULL default 1
        }
    }
}