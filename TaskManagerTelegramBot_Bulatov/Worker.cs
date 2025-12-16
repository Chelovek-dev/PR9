using TaskManagerTelegramBot_Bulatov.Classes;
using TaskManagerTelegramBot_Bulatov.Data;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;

namespace TaskManagerTelegramBot_Bulatov
{
    public class Worker : BackgroundService
    {
        readonly string Token = "8595695924:AAHJq3NZAj2Ej8oVNiixNMC0wKi314Gmp88";
        TelegramBotClient TelegramBotClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<Worker> _logger;
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

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

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
            else if (typeMessage == 3)
            {
                await TelegramBotClient.SendMessage(
                    chatId,
                    $"Указанное вами время и дата не могут быть установлены, " +
                    $"потому что сейчас уже: {DateTime.Now.ToString("HH:mm dd.MM.yyyy")}");
            }
        }
        public async void Command(long chatId, string command)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (command.ToLower() == "/start")
                SendMessage(chatId, 0);
            else if (command.ToLower() == "/create_task")
                SendMessage(chatId, 1);
            else if (command.ToLower() == "/list_tasks")
            {
                var user = await dbContext.Users
                    .Include(u => u.Events)
                    .FirstOrDefaultAsync(x => x.IdUser == chatId);

                if (user == null)
                    SendMessage(chatId, 4);
                else if (user.Events.Count == 0)
                    SendMessage(chatId, 4);
                else
                {
                    foreach (Events Event in user.Events)
                    {
                        await TelegramBotClient.SendMessage(
                            chatId,
                            $"Уведомить пользователя: {Event.Time.ToString("HH:mm dd.MM.yyyy")}" +
                            $"\n Сообщение: {Event.Message}",
                            replyMarkup: DeleteEvent(Event.Message)
                            );
                    }
                }
            }
        }

        private async void GetMessages(Message message)
        {
            Console.WriteLine("Получено сообщение: " + message.Text + " от пользователя: " + message.Chat.Username);
            long IdUser = message.Chat.Id;
            string MessageUser = message.Text;

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (message.Text.Contains("/"))
                Command(message.Chat.Id, message.Text);
            else if (message.Text.Equals("Удалить все задачи"))
            {
                var user = await dbContext.Users
                    .Include(u => u.Events)
                    .FirstOrDefaultAsync(x => x.IdUser == message.Chat.Id);

                if (user == null)
                    SendMessage(message.Chat.Id, 4);
                else if (user.Events.Count == 0)
                    SendMessage(user.IdUser, 4);
                else
                {
                    dbContext.Events.RemoveRange(user.Events);
                    user.Events.Clear();
                    await dbContext.SaveChangesAsync();
                    SendMessage(user.IdUser, 6);
                }
            }
            else
            {
                var user = await dbContext.Users
                    .Include(u => u.Events)
                    .FirstOrDefaultAsync(x => x.IdUser == message.Chat.Id);

                if (user == null)
                {
                    user = new Users(message.Chat.Id, message.Chat.Username ?? string.Empty);
                    dbContext.Users.Add(user);
                    await dbContext.SaveChangesAsync();
                }

                string[] Info = message.Text.Split('\n');
                if (Info.Length < 2)
                {
                    SendMessage(message.Chat.Id, 2);
                    return;
                }

                DateTime Time;
                if (CheckFormatDateTime(Info[0], out Time) == false)
                {
                    SendMessage(message.Chat.Id, 2);
                    return;
                }

                if (Time < DateTime.Now)
                {
                    SendMessage(message.Chat.Id, 3);
                    return;
                }

                var newEvent = new Events(
                    Time,
                    message.Text.Replace(Time.ToString("HH:mm dd.MM.yyyy") + "\n", ""),
                    user.Id);

                user.Events.Add(newEvent);
                await dbContext.SaveChangesAsync();

                await TelegramBotClient.SendMessage(
                    message.Chat.Id,
                    $"Напоминание добавлено на {Time.ToString("HH:mm dd.MM.yyyy")}",
                    replyMarkup: GetButtons());
            }
        }

        private async Task HandleUpdateAsync(
            ITelegramBotClient client,
            Update update,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (update.Type == UpdateType.Message)
                GetMessages(update.Message);
            else if (update.Type == UpdateType.CallbackQuery)
            {
                CallbackQuery query = update.CallbackQuery;

                var user = await dbContext.Users
                    .Include(u => u.Events)
                    .FirstOrDefaultAsync(x => x.IdUser == query.Message.Chat.Id);

                if (user != null)
                {
                    var eventToRemove = user.Events.FirstOrDefault(x => x.Message == query.Data);
                    if (eventToRemove != null)
                    {
                        dbContext.Events.Remove(eventToRemove);
                        await dbContext.SaveChangesAsync();
                        SendMessage(query.Message.Chat.Id, 5);
                    }
                }
            }
        }

        private async Task HandleErrorAsync(
            ITelegramBotClient client,
            Exception exception,
            HandleErrorSource source,
            CancellationToken token)
        {
            Console.WriteLine("Ошибка: " + exception.Message);
        }

        public async void Tick(object obj)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.Now;
            var startOfMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            var endOfMinute = startOfMinute.AddMinutes(1).AddSeconds(-1);

            var eventsToNotify = await dbContext.Events
                .Include(e => e.User)
                .Where(e => e.Time >= startOfMinute && e.Time <= endOfMinute)
                .ToListAsync();

            foreach (var eventItem in eventsToNotify)
            {
                try
                {
                    await TelegramBotClient.SendMessage(
                        eventItem.User.IdUser,
                        "Напоминание: " + eventItem.Message);

                    dbContext.Events.Remove(eventItem);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке напоминания: {ex.Message}");
                }
            }

            if (eventsToNotify.Any())
            {
                await dbContext.SaveChangesAsync();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.EnsureCreatedAsync();

            TelegramBotClient = new TelegramBotClient(Token);

            TelegramBotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                null,
                new CancellationTokenSource().Token);

            TimerCallback TimerCallback = new TimerCallback(Tick);
            Timer = new Timer(TimerCallback, 0, 0, 60 * 1000);
        }
    }
}
