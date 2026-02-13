using Scenario01.DocumentProcessing.Models;

namespace Scenario01.DocumentProcessing.Services;

/// <summary>
/// Analyzes document text and assigns a classification category.
/// </summary>
public interface IClassificationService
{
    /// <summary>
    /// Classifies a document based on its extracted text content.
    /// </summary>
    /// <param name="text">The extracted text from the document.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>The <see cref="DocumentClassification"/> determined from the text analysis.</returns>
    Task<DocumentClassification> ClassifyAsync(string text, CancellationToken ct);
}
