# Expense Bot

A Telegram bot for tracking daily expenses. Send a plain message like `taksi 15` or `non 8 oziq-ovqat` and the bot saves it; ask for reports by day or month.

## Features

- Add an expense with a plain message: `<note> <amount> [category]`
- `/bugun` - today's total with a breakdown by category
- `/oy` - this month's total with a breakdown by category
- `/oxirgilar` - the last 10 expenses
- `/ochirish` - delete the last expense
- Each user's data is stored separately in SQLite
- Errors are logged, and the user gets a friendly message instead of silence

## Tech stack

- C# and .NET 10 (Worker Service)
- Telegram.Bot
- Entity Framework Core with SQLite
- xUnit for unit tests

## Project structure

- `Worker.cs` - receives Telegram updates and sends replies
- `ExpenseService.cs` - all database operations
- `ExpenseParser.cs` - turns a message into amount, note and category
- `Models.cs`, `AppDbContext.cs` - database models and context
- `Expensive.bot.Tests` - unit tests for the parser

## Run locally

1. Create a bot with [@BotFather](https://t.me/BotFather) and copy its token.
2. Open a terminal in the `Expensive.bot` project folder and store the token:

   ```
   dotnet user-secrets set "BotToken" "YOUR_TOKEN"
   ```

3. Create the database (requires `dotnet tool install --global dotnet-ef`):

   ```
   dotnet ef database update
   ```

4. Start the bot:

   ```
   dotnet run
   ```

The token is kept in .NET user secrets and is never committed to the repository.

## Tests

```
dotnet test
```

## Roadmap

- Deploy to a Linux server
- Optional PostgreSQL support
- Docker support