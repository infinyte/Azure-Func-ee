namespace Scenario01.DocumentProcessing.Models;

/// <summary>
/// Categorizes a document based on its content after classification analysis.
/// </summary>
public enum DocumentClassification
{
    /// <summary>
    /// The document could not be classified into a known category.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// An invoice or billing document.
    /// </summary>
    Invoice = 1,

    /// <summary>
    /// A receipt or proof of purchase.
    /// </summary>
    Receipt = 2,

    /// <summary>
    /// A contract or legal agreement.
    /// </summary>
    Contract = 3,

    /// <summary>
    /// A report or analytical document.
    /// </summary>
    Report = 4,

    /// <summary>
    /// General correspondence such as letters or memos.
    /// </summary>
    Correspondence = 5
}
