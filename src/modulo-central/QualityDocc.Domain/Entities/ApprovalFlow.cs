using System;

namespace QualityDocc.Domain.Entities
{
    public class ApprovalFlow
    {
        public int Id { get; set; }

        // Vinculado a DocumentVersions
        public int VersionId { get; set; }

        // Vinculado a Users
        public int ApproverId { get; set; }

        public string Comments { get; set; } = string.Empty;

        // Decision (Approved, Rejected, etc.)
        public string Decision { get; set; } = string.Empty;

        public DateTime DateCreate { get; set; } = DateTime.Now;
    }
}