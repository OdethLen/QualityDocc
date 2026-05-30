using System;

namespace QualityDocc.Domain.Entities
{
    public class DocumentVersion
    {
        // En tu script se llama 'id' en minúscula, C# lo mapea automáticamente
        public int Id { get; set; }

        // Llave foránea de tu tabla Documents
        public int DocumentId { get; set; }

        // Cambió a int en tu script
        public int VersionNumber { get; set; }

        public string FileUrl { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;

        // En tu script se llama 'lifecycleStatus'
        public string LifecycleStatus { get; set; } = "Draft";

        public string ChangeLog { get; set; } = string.Empty;

        // Campos de auditoría que agregaste a todas tus tablas
        public int? IdUserCreate { get; set; }
        public DateTime DateCreate { get; set; } = DateTime.Now;
        public int? IdUserUpdate { get; set; }
        public DateTime? DateUpdate { get; set; }
        public int? IdUserDelete { get; set; }
        public DateTime? DateDelete { get; set; }
        public bool IsDeleted { get; set; } = false; // Mapea tu BIT DEFAULT 0
        public virtual Document Document { get; set; } = null!; // Asegúrate que sea de tipo 'Document'
    }
}