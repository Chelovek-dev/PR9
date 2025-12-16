namespace TaskManagerTelegramBot_Bulatov.Classes
{
    public class Events
    {
        public int Id { get; set; } 
        public DateTime Time { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }  
        public virtual Users? User { get; set; } 
        public Events() { }
        public Events(DateTime time, string message)
        {
            Time = time;
            Message = message;
        }
        public Events(DateTime time, string message, int userId)
        {
            Time = time;
            Message = message;
            UserId = userId;
        }
    }
}