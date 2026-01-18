namespace MaltalistApi.Models
{
    public class Report
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public string? ReporterName { get; set; }
        public string? ReporterEmail { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "pending"; // pending, reviewed, resolved, dismissed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
        public string? ReviewNotes { get; set; }

        // Navigation property
        public Listing? Listing { get; set; }
    }

    public class CreateReportRequest
    {
        public int ListingId { get; set; }
        public string? ReporterName { get; set; }
        public string? ReporterEmail { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateReportStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? ReviewedBy { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
