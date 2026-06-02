using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDocc.Domain.Entities
{
    [Table("DocumentVersion")]
    public class DocumentVersion : BaseEntity
    {
        // 1. Relación con el Documento
        public int DocumentId { get; set; }

        // 2. Propiedades de la versión
        public double VersionNumber { get; set; }

        // 3. Campos que el compilador te está pidiendo (¡AQUÍ ESTABAN LOS ERRORES!)
        public string LifecycleStatus { get; set; } = "Draft"; // Necesario para HomeController.cs
        public bool IsDeleted { get; set; } = false;           // Necesario para ApplicationDbContext.cs

        // 4. Campos adicionales
        public string? FileUrl { get; set; }
        public string? Extension { get; set; }
        public string? ChangeLog { get; set; }

        // Propiedad de navegación
        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; } = null!;
    }
}