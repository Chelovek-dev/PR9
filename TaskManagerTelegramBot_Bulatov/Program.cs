using Microsoft.EntityFrameworkCore;
using TaskManagerTelegramBot_Bulatov;
using TaskManagerTelegramBot_Bulatov.Data;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    "server=localhost;port=3306;database=reminder_bot;user=root;password=;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString,
        ServerVersion.AutoDetect(connectionString)));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();