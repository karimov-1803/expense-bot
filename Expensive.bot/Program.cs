using Expensive.bot;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=expenses.db"));
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();