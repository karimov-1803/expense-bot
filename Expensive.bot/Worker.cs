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
                    await TrySendErrorAsync(client, update, ct);
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
        var handler = scope.ServiceProvider.GetRequiredService<BotCommandHandler>();

        var reply = await handler.HandleAsync(text, telegramId, ct);
        await client.SendMessage(chatId, reply, cancellationToken: ct);
    }

    private async Task TrySendErrorAsync(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        var chatId = update.Message?.Chat.Id;
        if (chatId is null)
            return;

        try
        {
            await client.SendMessage(chatId.Value,
                "Xatolik yuz berdi. Keyinroq qayta urinib ko'ring.",
                cancellationToken: ct);
        }
        catch (Exception sendEx)
        {
            _logger.LogError(sendEx, "Xato xabarini yuborib bo'lmadi");
        }
    }
}