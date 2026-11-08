using System.Globalization;

namespace Expensive.bot;

public record ParsedExpense(string Note, decimal Amount, string Category);

public static class ExpenseParser
{
    public static ParsedExpense? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        var amountText = parts[1].Replace(',', '.');
        if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return null;

        if (amount <= 0)
            return null;

        var note = parts[0];
        var category = parts.Length > 2 ? string.Join(' ', parts.Skip(2)) : "boshqa";

        return new ParsedExpense(note, amount, category);
    }
}