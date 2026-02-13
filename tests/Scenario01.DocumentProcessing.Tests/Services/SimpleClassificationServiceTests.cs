using FluentAssertions;
using Scenario01.DocumentProcessing.Models;
using Scenario01.DocumentProcessing.Services;
using Xunit;

namespace Scenario01.DocumentProcessing.Tests.Services;

public class SimpleClassificationServiceTests
{
    private readonly SimpleClassificationService _sut = new();

    [Theory]
    [InlineData("This is an invoice for services rendered.", DocumentClassification.Invoice)]
    [InlineData("Please find the bill attached.", DocumentClassification.Invoice)]
    [InlineData("Amount due: $500.00", DocumentClassification.Invoice)]
    [InlineData("Invoice number: INV-001", DocumentClassification.Invoice)]
    public async Task ClassifyAsync_TextContainingInvoiceKeywords_ClassifiesAsInvoice(
        string text, DocumentClassification expected)
    {
        // Act
        var result = await _sut.ClassifyAsync(text, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Here is your receipt for the purchase.", DocumentClassification.Receipt)]
    [InlineData("Transaction confirmed. Payment received.", DocumentClassification.Receipt)]
    [InlineData("Thank you for your purchase.", DocumentClassification.Receipt)]
    public async Task ClassifyAsync_TextContainingReceiptKeywords_ClassifiesAsReceipt(
        string text, DocumentClassification expected)
    {
        // Act
        var result = await _sut.ClassifyAsync(text, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("This contract is between the parties.", DocumentClassification.Contract)]
    [InlineData("The agreement shall be binding upon signature.", DocumentClassification.Contract)]
    [InlineData("Terms and conditions apply. The parties hereby agree.", DocumentClassification.Contract)]
    public async Task ClassifyAsync_TextContainingContractKeywords_ClassifiesAsContract(
        string text, DocumentClassification expected)
    {
        // Act
        var result = await _sut.ClassifyAsync(text, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Quarterly report with executive summary and findings.", DocumentClassification.Report)]
    [InlineData("Analysis of results and recommendations.", DocumentClassification.Report)]
    public async Task ClassifyAsync_TextContainingReportKeywords_ClassifiesAsReport(
        string text, DocumentClassification expected)
    {
        // Act
        var result = await _sut.ClassifyAsync(text, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Dear Sir, Sincerely yours.", DocumentClassification.Correspondence)]
    [InlineData("Please find attached the memo with kind regards.", DocumentClassification.Correspondence)]
    public async Task ClassifyAsync_TextContainingCorrespondenceKeywords_ClassifiesAsCorrespondence(
        string text, DocumentClassification expected)
    {
        // Act
        var result = await _sut.ClassifyAsync(text, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Random text with no matching keywords at all.")]
    [InlineData("abcdefghijklmnopqrstuvwxyz")]
    public async Task ClassifyAsync_TextWithNoMatchingKeywords_ClassifiesAsUnknown(string text)
    {
        // Act
        var result = await _sut.ClassifyAsync(text, CancellationToken.None);

        // Assert
        result.Should().Be(DocumentClassification.Unknown);
    }

    [Fact]
    public async Task ClassifyAsync_EmptyText_ClassifiesAsUnknown()
    {
        // Act
        var result = await _sut.ClassifyAsync(string.Empty, CancellationToken.None);

        // Assert
        result.Should().Be(DocumentClassification.Unknown);
    }

    [Fact]
    public async Task ClassifyAsync_WhitespaceText_ClassifiesAsUnknown()
    {
        // Act
        var result = await _sut.ClassifyAsync("   ", CancellationToken.None);

        // Assert
        result.Should().Be(DocumentClassification.Unknown);
    }

    [Fact]
    public async Task ClassifyAsync_NullText_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.ClassifyAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ClassifyAsync_CancellationRequested_ThrowsOperationCancelledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => _sut.ClassifyAsync("Some invoice text", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ClassifyAsync_CaseInsensitiveMatching_ClassifiesCorrectly()
    {
        // Act
        var result = await _sut.ClassifyAsync("INVOICE NUMBER: INV-100", CancellationToken.None);

        // Assert
        result.Should().Be(DocumentClassification.Invoice);
    }

    [Fact]
    public async Task ClassifyAsync_MultipleCategories_ReturnsCategoryWithMostKeywordMatches()
    {
        // Text with multiple invoice keywords but only one receipt keyword.
        var text = "This invoice has an amount due, a subtotal, and a total due. Receipt attached.";

        // Act
        var result = await _sut.ClassifyAsync(text, CancellationToken.None);

        // Assert — invoice should win because it has more keyword matches.
        result.Should().Be(DocumentClassification.Invoice);
    }
}
