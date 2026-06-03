using System; // Necesario para DateTime
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDocc.Domain.Entities
{
    public class Document : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Cambiado a string para que coincida con varchar(50) en SQL
        public string CurrentStatus { get; set; } = "Borrador";

        // Campos faltantes que están en tu tabla (image_015b1e.png)
        public int AuthorId { get; set; }
        public int CompanyId { get; set; }
        public int CategoryId { get; set; }
        public string? RejectionNotes { get; set; }

     

        // Propiedades de navegación
        public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;
    }
}