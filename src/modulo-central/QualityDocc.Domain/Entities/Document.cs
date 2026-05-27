using System;
using System.Collections.Generic;

namespace QualityDocc.Domain.Entities
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = "Borrador"; // Borrador, Revision, Aprobado, Obsoleto
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool Status { get; set; } = true; // Bit NOT NULL default 1 (para borrado lógico)

        // Relación: Un documento tiene muchas versiones históricas
        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    }
}