using Scenario01.DocumentProcessing.Models;

namespace Scenario01.DocumentProcessing.Services;

/// <summary>
/// A keyword-based document classification service that scans extracted text
/// for known terms to determine the document type. Intended as a baseline
/// implementation; production systems should substitute an ML-backed classifier.
/// </summary>
public sealed class SimpleClassificationService : IClassificationService
{
    /// <summary>
    /// Mapping of classification categories to their associated keywords.
    /// Keywords are checked case-insensitively against the document text.
    /// </summary>
    private static readonly IReadOnlyDictionary<DocumentClassification, string[]> ClassificationKeywords =
        new Dictionary<DocumentClassification, string[]>
        {
            [DocumentClassification.Invoice] = new[]
            {
                "invoice", "bill", "billing", "amount due", "payment due",
                "invoice number", "subtotal", "total due", "net amount"
            },
            [DocumentClassification.Receipt] = new[]
            {
                "receipt", "transaction", "purchase", "paid", "payment received",
                "change due", "thank you for your purchase", "order confirmation"
            },
            [DocumentClassification.Contract] = new[]
            {
                "contract", "agreement", "terms and conditions", "hereby agree",
                "binding", "parties", "effective date", "termination",
                "obligations", "whereas", "witnesseth"
            },
            [DocumentClassification.Report] = new[]
            {
                "report", "analysis", "findings", "summary", "executive summary",
                "conclusion", "recommendations", "methodology", "results",
                "quarterly report", "annual report"
            },
            [DocumentClassification.Correspondence] = new[]
            {
                "dear", "sincerely", "regards", "to whom it may concern",
                "letter", "memo", "memorandum", "correspondence",
                "please find attached", "kind regards", "best regards"
            }
        };

    /// <inheritdoc />
    public Task<DocumentClassification> ClassifyAsync(string text, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(text);
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(DocumentClassification.Unknown);
        }

        // Score each classification by counting keyword matches.
        var bestClassification = DocumentClassification.Unknown;
        var bestScore = 0;

        foreach (var (classification, keywords) in ClassificationKeywords)
        {
            var score = keywords.Count(keyword =>
                text.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            if (score > bestScore)
            {
                bestScore = score;
                bestClassification = classification;
            }
        }

        return Task.FromResult(bestClassification);
    }
}
