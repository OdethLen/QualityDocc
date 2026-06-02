using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDocc.Domain.Entities
{
    public class Document : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DocumentStatus CurrentStatus { get; set; } = DocumentStatus.Borrador;

        // Relaciones necesarias (Foreign Keys)
        public int AuthorId { get; set; }
        public int CompanyId { get; set; }
        public int CategoryId { get; set; }

        public string? RejectionNotes { get; set; }

        // Propiedades de navegación para Entity Framework
        public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;
    }
}