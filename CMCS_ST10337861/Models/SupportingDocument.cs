namespace CMCS_ST10337861.Models
{
    public class SupportingDocument
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        // Navigation property
        public Claim Claim { get; set; } = null!;
    }
}