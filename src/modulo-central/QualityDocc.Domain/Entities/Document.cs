using System;
using System.Collections.Generic;

namespace QualityDocc.Domain.Entities
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DocumentStatus CurrentStatus { get; set; } = DocumentStatus.Borrador;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool Status { get; set; } = true; // Bit NOT NULL default 1 (para borrado lógico)
        public int AuthorId { get; set; }
        // Relación: Un documento tiene muchas versiones históricas
        public string? RejectionNotes { get; set; }
        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    }
}