using Microsoft.EntityFrameworkCore;

namespace Expensive.bot;

public class ExpenseService
{
    private readonly AppDbContext _db;

    public ExpenseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(long telegramId, ParsedExpense parsed, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId, ct);
        if (user is null)
        {
            user = new BotUser { TelegramId = telegramId };
            _db.Users.Add(user);
        }

        _db.Expenses.Add(new Expense
        {
            BotUser = user,
            Amount = parsed.Amount,
            Category = parsed.Category,
            Note = parsed.Note
        });
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Expense>> GetLastAsync(long telegramId, int count, CancellationToken ct) =>
        _db.Expenses
            .Where(e => e.BotUser.TelegramId == telegramId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

    public Task<List<Expense>> GetSinceAsync(long telegramId, DateTime fromUtc, CancellationToken ct) =>
        _db.Expenses
            .Where(e => e.BotUser.TelegramId == telegramId && e.CreatedAt >= fromUtc)
            .ToListAsync(ct);

    public async Task<Expense?> DeleteLastAsync(long telegramId, CancellationToken ct)
    {
        var last = await _db.Expenses
            .Where(e => e.BotUser.TelegramId == telegramId)
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync(ct);

        if (last is null)
            return null;

        _db.Expenses.Remove(last);
        await _db.SaveChangesAsync(ct);
        return last;
    }
}