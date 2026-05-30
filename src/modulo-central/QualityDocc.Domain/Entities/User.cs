using System;
using System.Collections.Generic;
using System.Text;

namespace QualityDocc.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty; // <-- Nuevo campo
        public string PasswordHash { get; set; } = string.Empty;

        // Relación con Role
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        // Relación con Company (Nulleable para el SuperAdmin)
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}