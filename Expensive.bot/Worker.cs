using Telegram.Bot;
using Telegram.Bot.Types;

namespace Expensive.bot;

public class Worker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(IConfiguration config, ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var token = _config["BotToken"]
            ?? throw new InvalidOperationException("BotToken topilmadi");

        var bot = new TelegramBotClient(token);

        bot.StartReceiving(
            async (client, update, ct) =>
            {
                try
                {
                    await HandleUpdateAsync(client, update, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Xabarni qayta ishlashda xato");

                    var failedChatId = update.Message?.Chat.Id;
                    if (failedChatId is not null)
                    {
                        try
                        {
                            await client.SendMessage(failedChatId.Value,
                                "Xatolik yuz berdi. Keyinroq qayta urinib ko'ring.",
                                cancellationToken: ct);
                        }
                        catch (Exception sendEx)
                        {
                            _logger.LogError(sendEx, "Xato xabarini yuborib bo'lmadi");
                        }
                    }
                }
            },
            (client, ex, source, ct) =>
            {
                _logger.LogError(ex, "Telegram xatosi");
                return Task.CompletedTask;
            },
            cancellationToken: stoppingToken);

        _logger.LogInformation("Bot ishlayapti...");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        if (update.Message?.Text is not { } text)
            return;

        var chatId = update.Message.Chat.Id;
        var telegramId = update.Message.From?.Id ?? chatId;

       

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ExpenseService>();

        if (text.StartsWith("/start"))
        {
            await client.SendMessage(chatId,
                "Salom! Xarajatni shunday yozing: taksi 15 yoki non 8 oziq-ovqat\n\nBuyruqlar:\n/bugun - bugungi xarajatlar\n/oy - shu oy xarajatlari\n/oxirgilar - oxirgi 10 ta xarajat\n/ochirish - oxirgi xarajatni o'chirish",
                cancellationToken: ct);
            return;
        }

        if (text.StartsWith("/oxirgilar"))
        {
            var last = await service.GetLastAsync(telegramId, 10, ct);

            var answer = last.Count == 0
                ? "Hozircha xarajat yo'q."
                : string.Join("\n", last.Select((e, i) =>
                    $"{i + 1}. {e.Note}, {e.Amount} ({e.Category})"));

            await client.SendMessage(chatId, answer, cancellationToken: ct);
            return;
        }

        if (text.StartsWith("/bugun") || text.StartsWith("/oy"))
        {
            var isToday = text.StartsWith("/bugun");
            var localNow = DateTime.UtcNow.AddHours(5); // Dushanbe vaqti
            var startUtc = (isToday
                ? localNow.Date
                : new DateTime(localNow.Year, localNow.Month, 1)).AddHours(-5);
            var title = isToday ? "Bugungi xarajatlar" : "Shu oy xarajatlari";

            var items = await service.GetSinceAsync(telegramId, startUtc, ct);

            if (items.Count == 0)
            {
                await client.SendMessage(chatId, $"{title}: hozircha yo'q.", cancellationToken: ct);
                return;
            }

            var total = items.Sum(e => e.Amount);
            var lines = items
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Sum = g.Sum(e => e.Amount) })
                .OrderByDescending(g => g.Sum)
                .Select(g => $"{g.Category}: {g.Sum}");

            await client.SendMessage(chatId,
                $"{title}: {total} somoni\n" + string.Join("\n", lines),
                cancellationToken: ct);
            return;
        }

        if (text.StartsWith("/ochirish"))
        {
            var deleted = await service.DeleteLastAsync(telegramId, ct);

            var answer = deleted is null
                ? "O'chirish uchun xarajat yo'q."
                : $"O'chirildi: {deleted.Note}, {deleted.Amount}";

            await client.SendMessage(chatId, answer, cancellationToken: ct);
            return;
        }

        var expense = ExpenseParser.Parse(text);
        if (expense is null)
        {
            await client.SendMessage(chatId,
                "Tushunmadim. Shunday yozing: taksi 15 yoki non 8 oziq-ovqat",
                cancellationToken: ct);
            return;
        }

        await service.AddAsync(telegramId, expense, ct);

        await client.SendMessage(chatId,
            $"Saqlandi: {expense.Note}, {expense.Amount} somoni, kategoriya: {expense.Category}",
            cancellationToken: ct);
    }
}