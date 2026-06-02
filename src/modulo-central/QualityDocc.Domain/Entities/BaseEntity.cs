using System;
using System.Collections.Generic;
using System.Text;

namespace QualityDocc.Domain.Entities
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public int? IdUserCreate { get; set; }
        public DateTime? DateCreate { get; set; } = DateTime.Now;
        public int? IdUserUpdate { get; set; }
        public DateTime? DateUpdate { get; set; }
        public int? IdUserDelete { get; set; }
        public DateTime? DateDelete { get; set; }
        public bool Status { get; set; } = true;
    }
}
