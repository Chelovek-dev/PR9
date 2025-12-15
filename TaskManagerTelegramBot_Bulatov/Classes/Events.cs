namespace TaskManagerTelegramBot_Bulatov.Classes
{
    public class Events
    {
        public DateTime Time {  get; set; }
        public string Message { get; set; }
        public Events(DateTime time, string message)
        {
            Time = time;
            Message = message;
        }
    }
}