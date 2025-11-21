using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS_ST10337861.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }

        [Required]
        public string LecturerId { get; set; } = string.Empty;

        [Required]
        public string Month { get; set; } = string.Empty;

        [Required]
        [Range(2020, 2100)]
        public int Year { get; set; }

        [Required]
        [Range(0.5, 500)]
        public decimal HoursWorked { get; set; }

        [Required]
        [Range(50, 10000)]
        public decimal HourlyRate { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Verified, Approved, Rejected

        public DateTime SubmittedDate { get; set; }

        // Navigation properties
        // Lecturer navigation is optional for model binding (we set LecturerId explicitly),
        // so make this nullable to avoid required-field validation on the navigation property.
        public ApplicationUser? Lecturer { get; set; }
        public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
    }
}