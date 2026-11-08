using Microsoft.EntityFrameworkCore;

namespace Expensive.bot;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<BotUser> Users => Set<BotUser>();
    public DbSet<Expense> Expenses => Set<Expense>();
}