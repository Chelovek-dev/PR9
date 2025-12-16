namespace TaskManagerTelegramBot_Bulatov.Classes
{
    public class Users
    {
        public int Id { get; set; }  
        public long IdUser { get; set; }  
        public string? Username { get; set; }  
        public virtual List<Events> Events { get; set; } = new List<Events>();

        public Users() { }

        public Users(long idUser)
        {
            IdUser = idUser;
            Events = new List<Events>();
        }

        public Users(long idUser, string username)
        {
            IdUser = idUser;
            Username = username;
            Events = new List<Events>();
        }
    }
}