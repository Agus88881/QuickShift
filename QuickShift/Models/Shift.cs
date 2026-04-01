namespace QuickShift.Models
{
    public class Shift
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
