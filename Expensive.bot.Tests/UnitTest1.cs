using Xunit;
using Expensive.bot;

namespace Expensive.bot.Tests;

public class ExpenseParserTests
{
    [Fact]
    public void Parse_SimpleExpense_UsesDefaultCategory()
    {
        var result = ExpenseParser.Parse("taksi 15");

        Assert.NotNull(result);
        Assert.Equal("taksi", result!.Note);
        Assert.Equal(15m, result.Amount);
        Assert.Equal("boshqa", result.Category);
    }

    [Fact]
    public void Parse_WithCategory_ReturnsCategory()
    {
        var result = ExpenseParser.Parse("non 8 oziq-ovqat");

        Assert.NotNull(result);
        Assert.Equal("non", result!.Note);
        Assert.Equal(8m, result.Amount);
        Assert.Equal("oziq-ovqat", result.Category);
    }

    [Fact]
    public void Parse_CommaAsDecimalSeparator_Works()
    {
        var result = ExpenseParser.Parse("non 8,5");

        Assert.NotNull(result);
        Assert.Equal(8.5m, result!.Amount);
    }

    [Fact]
    public void Parse_MultiWordCategory_JoinsWords()
    {
        var result = ExpenseParser.Parse("non 8 oziq ovqat");

        Assert.NotNull(result);
        Assert.Equal("oziq ovqat", result!.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("salom")]
    [InlineData("taksi abc")]
    [InlineData("taksi -5")]
    [InlineData("taksi 0")]
    public void Parse_InvalidInput_ReturnsNull(string? text)
    {
        var result = ExpenseParser.Parse(text);

        Assert.Null(result);
    }
}