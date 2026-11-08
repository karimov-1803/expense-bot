namespace Expensive.bot;

public class BotUser
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Expense> Expenses { get; set; } = new();
}

public class Expense
{
    public int Id { get; set; }
    public int BotUserId { get; set; }
    public BotUser BotUser { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Category { get; set; } = "boshqa";
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}