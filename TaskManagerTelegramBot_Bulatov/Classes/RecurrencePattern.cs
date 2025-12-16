namespace TaskManagerTelegramBot_Bulatov.Classes
{
    public class RecurrencePattern
    {
        public string Type { get; set; } = "daily";
        public int Interval { get; set; } = 1;
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();
        public TimeSpan TimeOfDay { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Occurrences { get; set; }
        public int? DayOfMonth { get; set; }
        public string? CustomPattern { get; set; }
    }
}