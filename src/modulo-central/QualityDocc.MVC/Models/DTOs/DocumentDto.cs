namespace QualityDocc.MVC.Models.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VersionNumber { get; set; } = "0.1";
        public string CurrentStatus { get; set; } = "Borrador";
        public DateTime ChangeDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "Sistema";
    }
}