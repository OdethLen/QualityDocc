using System;
using System.Collections.Generic;
using System.Text;

namespace QualityDocc.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Relación con Empresa
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}