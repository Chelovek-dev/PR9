namespace TaskManagerTelegramBot_Bulatov.Classes
{
    public class Events
    {
        public DateTime TIme {  get; set; }
        public string Message { get; set; }
        public Events(DateTime time, string message)
        {
            TIme = time;
            Message = message;
        }
    }
}