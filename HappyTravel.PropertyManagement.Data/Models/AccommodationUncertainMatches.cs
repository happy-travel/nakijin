namespace HappyTravel.PropertyManagement.Data.Models
{
    public class AccommodationUncertainMatches
    {
        public int Id { get; set; }
        public int ExistingHtId { get; set; }
        public int NewHtId { get; set; }
        public float Score { get; set; }
        public bool IsActive { get; set; }
    }
}