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
            "Все события удалены.",
            "Выберите тип повтора:",
            "Задача создана с повтором: ",
            "Задача создана без повтора.",
            "Указанное вами время и дата не могут быть установлены, потому что сейчас уже: "
        };

        public Worker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public static ReplyKeyboardMarkup GetButtons()
        {
            List<KeyboardButton> buttons = new List<KeyboardButton>
            {
                new KeyboardButton("Удалить все задачи"),
                new KeyboardButton("Мои задачи"),
                new KeyboardButton("Создать задачу")
            };
            return new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };
        }

        public static ReplyKeyboardMarkup GetRepeatButtons()
        {
            List<List<KeyboardButton>> rows = new List<List<KeyboardButton>>
            {
                new List<KeyboardButton> { new KeyboardButton("Без повтора"), new KeyboardButton("Ежедневно") },
                new List<KeyboardButton> { new KeyboardButton("По будням"), new KeyboardButton("Еженедельно") },
                new List<KeyboardButton> { new KeyboardButton("Ежемесячно"), new KeyboardButton("Отмена") }
            };
            return new ReplyKeyboardMarkup(rows) { ResizeKeyboard = true, OneTimeKeyboard = true };
        }

        public InlineKeyboardMarkup GetDeleteButton(int taskId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🗑️ Удалить задачу", $"delete_{taskId}")
                }
            });
        }

        public async Task SendMessage(long chatId, int type)
        {
            if (type < 0 || type >= Messages.Count)
            {
                await TelegramBotClient.SendMessage(chatId, "Ошибка: неверный тип сообщения");
                return;
            }

            if (type == 3)
            {
                await TelegramBotClient.SendMessage(
                    chatId,
                    $"{Messages[10]}{DateTime.Now:HH:mm dd.MM.yyyy}",
                    ParseMode.Html,
                    replyMarkup: GetButtons());
                return;
            }

            await TelegramBotClient.SendMessage(
                chatId,
                Messages[type],
                ParseMode.Html,
                replyMarkup: type == 7 ? GetRepeatButtons() : GetButtons());
        }

        class PendingTask
        {
            public DateTime Time { get; set; }
            public string Text { get; set; } = "";
        }

        private Dictionary<long, PendingTask> pendingTasks = new Dictionary<long, PendingTask>();

        private async void ShowMyTasks(long chatId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await db.Users.Include(u => u.Events).FirstOrDefaultAsync(u => u.IdUser == chatId);
            if (user == null || user.Events.Count == 0)
            {
                await SendMessage(chatId, 4);
                return;
            }

            await TelegramBotClient.SendMessage(
                chatId,
                $"📋 Ваши задачи ({user.Events.Count}):",
                ParseMode.Html);

            foreach (var ev in user.Events)
            {
                string repeatInfo = ev.IsRecurring ? " 🔁" : "";
                string taskText = $"<b>⏰ {ev.Time:HH:mm dd.MM.yyyy}{repeatInfo}</b>\n{ev.Message}";

                await TelegramBotClient.SendMessage(
                    chatId,
                    taskText,
                    ParseMode.Html,
                    replyMarkup: GetDeleteButton(ev.Id));
            }
        }

        private async void ProcessMessage(Message message)
        {
            Console.WriteLine($"Сообщение: {message.Text} от {message.Chat.Username}");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (message.Text.StartsWith("/"))
            {
                if (message.Text == "/start")
                {
                    await TelegramBotClient.SendMessage(
                        message.Chat.Id,
                        Messages[0],
                        ParseMode.Html,
                        replyMarkup: GetButtons());
                }
                else if (message.Text == "/create_task")
                {
                    await TelegramBotClient.SendMessage(
                        message.Chat.Id,
                        Messages[1],
                        ParseMode.Html,
                        replyMarkup: GetButtons());
                }
                else if (message.Text == "/list_tasks" || message.Text == "/mytasks")
                {
                    ShowMyTasks(message.Chat.Id);
                }
            }
            else if (message.Text == "Мои задачи")
            {
                ShowMyTasks(message.Chat.Id);
            }
            else if (message.Text == "Создать задачу")
            {
                await TelegramBotClient.SendMessage(
                    message.Chat.Id,
                    Messages[1],
                    ParseMode.Html,
                    replyMarkup: GetButtons());
            }
            else if (message.Text == "Удалить все задачи")
            {
                var user = await db.Users.Include(u => u.Events).FirstOrDefaultAsync(u => u.IdUser == message.Chat.Id);
                if (user != null && user.Events.Any())
                {
                    int count = user.Events.Count;
                    db.Events.RemoveRange(user.Events);
                    await db.SaveChangesAsync();
                    await TelegramBotClient.SendMessage(
                        message.Chat.Id,
                        $"✅ Удалено {count} задач",
                        replyMarkup: GetButtons());
                }
                else
                {
                    await SendMessage(message.Chat.Id, 4);
                }
            }
            else if (pendingTasks.ContainsKey(message.Chat.Id))
            {
                var pending = pendingTasks[message.Chat.Id];

                if (message.Text == "Отмена")
                {
                    pendingTasks.Remove(message.Chat.Id);
                    await TelegramBotClient.SendMessage(
                        message.Chat.Id,
                        "Создание задачи отменено.",
                        replyMarkup: GetButtons());
                    return;
                }

                string repeatType = "none";
                if (message.Text == "Ежедневно") repeatType = "daily";
                else if (message.Text == "По будням") repeatType = "weekdays";
                else if (message.Text == "Еженедельно") repeatType = "weekly";
                else if (message.Text == "Ежемесячно") repeatType = "monthly";

                var user = await db.Users.Include(u => u.Events).FirstOrDefaultAsync(u => u.IdUser == message.Chat.Id);
                if (user == null)
                {
                    user = new Users(message.Chat.Id, message.Chat.Username ?? "");
                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                }

                var newEvent = new Events(pending.Time, pending.Text, user.Id);

                if (repeatType != "none")
                {
                    newEvent.IsRecurring = true;
                    newEvent.RecurrencePattern = repeatType;
                    await TelegramBotClient.SendMessage(
                        message.Chat.Id,
                        $"{Messages[8]}{message.Text}",
                        replyMarkup: GetButtons());
                }
                else
                {
                    await TelegramBotClient.SendMessage(
                        message.Chat.Id,
                        Messages[9],
                        replyMarkup: GetButtons());
                }

                user.Events.Add(newEvent);
                await db.SaveChangesAsync();
                pendingTasks.Remove(message.Chat.Id);
            }
            else
            {
                string[] parts = message.Text.Split('\n');
                if (parts.Length < 2)
                {
                    await SendMessage(message.Chat.Id, 2);
                    return;
                }

                if (!DateTime.TryParse(parts[0], out DateTime taskTime))
                {
                    await SendMessage(message.Chat.Id, 2);
                    return;
                }

                if (taskTime < DateTime.Now)
                {
                    await SendMessage(message.Chat.Id, 3);
                    return;
                }

                pendingTasks[message.Chat.Id] = new PendingTask
                {
                    Time = taskTime,
                    Text = string.Join("\n", parts.Skip(1))
                };

                await SendMessage(message.Chat.Id, 7);
            }
        }

        private DateTime CalculateNextTime(DateTime current, string repeatType)
        {
            return repeatType switch
            {
                "daily" => current.AddDays(1),
                "weekdays" => GetNextWeekday(current),
                "weekly" => current.AddDays(7),
                "monthly" => current.AddMonths(1),
                _ => current
            };
        }

        private DateTime GetNextWeekday(DateTime current)
        {
            DateTime next = current.AddDays(1);
            while (next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday)
            {
                next = next.AddDays(1);
            }
            return next;
        }

        private async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message)
            {
                ProcessMessage(update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var query = update.CallbackQuery;
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (query.Data.StartsWith("delete_"))
                {
                    if (int.TryParse(query.Data.Replace("delete_", ""), out int taskId))
                    {
                        var task = await db.Events
                            .Include(e => e.User)
                            .FirstOrDefaultAsync(e => e.Id == taskId && e.User.IdUser == query.Message.Chat.Id);

                        if (task != null)
                        {
                            db.Events.Remove(task);
                            await db.SaveChangesAsync();

                            await TelegramBotClient.SendMessage(
                                query.Message.Chat.Id,
                                $"✅ Задача удалена: {task.Message}",
                                replyMarkup: GetButtons());

                                await TelegramBotClient.DeleteMessage(
                                    query.Message.Chat.Id,
                                    query.Message.MessageId);
                        }
                    }
                }
            }
        }

        private Task HandleError(ITelegramBotClient client, Exception error, CancellationToken token)
        {
            Console.WriteLine($"Ошибка: {error.Message}");
            return Task.CompletedTask;
        }

        public async void CheckReminders(object state)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.Now;
            var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            var end = start.AddMinutes(1);

            var tasks = await db.Events.Include(e => e.User).Where(e => e.Time >= start && e.Time < end).ToListAsync();

            foreach (var task in tasks)
            {
                try
                {
                    await TelegramBotClient.SendMessage(task.User.IdUser, $"Напоминание: {task.Message}");

                    if (task.IsRecurring && !string.IsNullOrEmpty(task.RecurrencePattern))
                    {
                        var nextTime = CalculateNextTime(task.Time, task.RecurrencePattern);
                        var newTask = new Events(nextTime, task.Message, task.UserId)
                        {
                            IsRecurring = true,
                            RecurrencePattern = task.RecurrencePattern
                        };
                        db.Events.Add(newTask);
                    }

                    db.Events.Remove(task);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }

            if (tasks.Any())
            {
                await db.SaveChangesAsync();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.EnsureCreatedAsync();

            TelegramBotClient = new TelegramBotClient(Token);
            TelegramBotClient.StartReceiving(HandleUpdate, HandleError, null, stoppingToken);

            Timer = new Timer(CheckReminders, null, 0, 60000);
        }
    }
}