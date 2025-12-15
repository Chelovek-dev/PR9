using System.Net.NetworkInformation;
using System.Threading.Tasks;
using TaskManagerTelegramBot_Bulatov.Classes;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TaskManagerTelegramBot_Bulatov
{
    public class Worker : BackgroundService
    {
        readonly string Token = "";
        TelegramBotClient TelegramBotClient;
        List<Users> Users = new List<Users>();
        Timer Timer;
        List<string> Messages = new List<string>()
        {
            "Здравствуйте! " +
            "\nРады приветствовать вас в Telegram-боте «Напоминатор»! 😊  " +
            "\nНаш бот создан для того, чтобы напоминать вам о важных событиях и мероприятиях. " +
            "С ним вы точно не пропустите ничего важного! 💬  " +
            "\nНе забудьте добавить бота в список своих контактов и настроить уведомления. " +
            "Тогда вы всегда будете в курсе событий! 😊",

            "Укажите дату и время напоминания в следующем формате:" +
            "\n<i><b>12:51 26.01.2025</b>" +
            "\nНапомни о том что я хотел сходить в магазин.</i>",

            "Кажется, что-то не получилось." +
            "Укажите дату и время напоминания в следующем формате:" +
            "\n<i><b>12:51 26.01.2025</b>" +
            "\nНапомни о том что я хотел сходить в магазин.</i>",
            "",
            "Задачи пользователя не найдены.",
            "Событие удалено.",
            "Все события удалены."
            };
        public bool CheckFormatDateTime(string value, out DateTime time)
        {
            return DateTime.TryParse(value, out time);
        }
        public static ReplyKeyboardMarkup GetButtons()
        {
            List<KeyboardButton> keyboardButtons = new List<KeyboardButton>();
            keyboardButtons.Add(new KeyboardButton("Удалить все задачи"));
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>() { keyboardButtons }  
            };
        }
        public static InlineKeyboardMarkup DeleteEvent(string Message)
        {
            List<InlineKeyboardButton> inlineKeyboards = new List<InlineKeyboardButton>();
            inlineKeyboards.Add(new InlineKeyboardButton("Удалить", Message));
            return new InlineKeyboardMarkup(inlineKeyboards);
        }
        public async void SendMessage(long chatId, int typeMessage)
        {
            if (typeMessage != 3)
            {
                await TelegramBotClient.SendMessage(
                    chatId,
                    Messages[typeMessage],
                    ParseMode.Html,
                    replyMarkup: GetButtons()
                    );
            }
            else if (chatId == 3)
            {
                await TelegramBotClient.SendMessage(
                    chatId,
                    $"Указанное вами время и дата не могут быть установлены, " +
                    $"потому что сейчас уже: {DateTime.Now.ToString("HH:mm dd:MM:yyyy)}")}");
            }
        }
        public async void Command(long chatId, string command)
        {
            if (command.ToLower() == "/start") SendMessage(chatId, 0);
            else if (command.ToLower() == "/create_task") SendMessage(chatId, 1);
            else if (command.ToLower() == "/list_tasks")
            {
                Users User = Users.Find(x => x.IdUser == chatId);
                if (User == null) SendMessage(chatId, 4);
                else if (User.Events.Count == 0) SendMessage(chatId, 4);
                else
                {
                    foreach (Events Event in User.Events)
                    {
                        await TelegramBotClient.SendMessage(
                            chatId,
                            $"Уведомить пользователя: {Event.TIme.ToString("HH:mm dd:MM:yyyy")}" +
                            $"\n Сообщение: {Event.Message}",
                            replyMarkup: DeleteEvent(Event.Message)
                            );
                    }
                }
            }
        }
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
