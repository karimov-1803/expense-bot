namespace Expensive.bot;

public class BotCommandHandler
{
    private const int UtcOffsetHours = 5; // Dushanbe vaqti

    private const string StartText =
        "Salom! Xarajatni shunday yozing: taksi 15 yoki non 8 oziq-ovqat\n\n" +
        "Buyruqlar:\n" +
        "/bugun - bugungi xarajatlar\n" +
        "/oy - shu oy xarajatlari\n" +
        "/oxirgilar - oxirgi 10 ta xarajat\n" +
        "/ochirish - oxirgi xarajatni o'chirish";

    private const string HelpHint = "Tushunmadim. Shunday yozing: taksi 15 yoki non 8 oziq-ovqat";

    private readonly ExpenseService _service;

    public BotCommandHandler(ExpenseService service)
    {
        _service = service;
    }

    public Task<string> HandleAsync(string text, long telegramId, CancellationToken ct)
    {
        if (text.StartsWith("/start")) return Task.FromResult(StartText);
        if (text.StartsWith("/oxirgilar")) return LastAsync(telegramId, ct);
        if (text.StartsWith("/bugun")) return ReportAsync(telegramId, today: true, ct);
        if (text.StartsWith("/oy")) return ReportAsync(telegramId, today: false, ct);
        if (text.StartsWith("/ochirish")) return DeleteLastAsync(telegramId, ct);

        return AddAsync(text, telegramId, ct);
    }

    private async Task<string> LastAsync(long telegramId, CancellationToken ct)
    {
        var last = await _service.GetLastAsync(telegramId, 10, ct);

        return last.Count == 0
            ? "Hozircha xarajat yo'q."
            : string.Join("\n", last.Select((e, i) =>
                $"{i + 1}. {e.Note}, {e.Amount} ({e.Category})"));
    }

    private async Task<string> ReportAsync(long telegramId, bool today, CancellationToken ct)
    {
        var localNow = DateTime.UtcNow.AddHours(UtcOffsetHours);
        var startUtc = (today
            ? localNow.Date
            : new DateTime(localNow.Year, localNow.Month, 1)).AddHours(-UtcOffsetHours);
        var title = today ? "Bugungi xarajatlar" : "Shu oy xarajatlari";

        var items = await _service.GetSinceAsync(telegramId, startUtc, ct);
        if (items.Count == 0)
            return $"{title}: hozircha yo'q.";

        var total = items.Sum(e => e.Amount);
        var lines = items
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Sum = g.Sum(e => e.Amount) })
            .OrderByDescending(g => g.Sum)
            .Select(g => $"{g.Category}: {g.Sum}");

        return $"{title}: {total} somoni\n" + string.Join("\n", lines);
    }

    private async Task<string> DeleteLastAsync(long telegramId, CancellationToken ct)
    {
        var deleted = await _service.DeleteLastAsync(telegramId, ct);

        return deleted is null
            ? "O'chirish uchun xarajat yo'q."
            : $"O'chirildi: {deleted.Note}, {deleted.Amount}";
    }

    private async Task<string> AddAsync(string text, long telegramId, CancellationToken ct)
    {
        var expense = ExpenseParser.Parse(text);
        if (expense is null)
            return HelpHint;

        await _service.AddAsync(telegramId, expense, ct);

        return $"Saqlandi: {expense.Note}, {expense.Amount} somoni, kategoriya: {expense.Category}";
    }
}