using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureFunctions.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Scenario01.DocumentProcessing.Services;

namespace Scenario01.DocumentProcessing.Functions;

/// <summary>
/// HTTP-triggered function that exposes a REST endpoint for querying document processing status.
/// </summary>
public sealed class GetDocumentStatusFunction
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<GetDocumentStatusFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Initializes a new instance of <see cref="GetDocumentStatusFunction"/>.
    /// </summary>
    public GetDocumentStatusFunction(
        IDocumentRepository repository,
        ILogger<GetDocumentStatusFunction> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves the processing status and metadata for a document by its unique identifier.
    /// Returns 200 with document metadata on success, or 404 with an error response if not found.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="id">The document identifier extracted from the route.</param>
    /// <param name="context">The function execution context.</param>
    /// <returns>An HTTP response containing the document metadata or an error.</returns>
    [Function("GetDocumentStatus")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "documents/{id}")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        _logger.LogInformation("Retrieving status for document {DocumentId}", id);

        if (string.IsNullOrWhiteSpace(id))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            badRequest.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var errorResponse = new ErrorResponse
            {
                Message = "Document ID is required.",
                ErrorCode = "INVALID_ARGUMENT"
            };
            await badRequest.WriteStringAsync(
                JsonSerializer.Serialize(errorResponse, JsonOptions)).ConfigureAwait(false);

            return badRequest;
        }

        var document = await _repository.GetAsync(id, context.CancellationToken).ConfigureAwait(false);

        if (document is null)
        {
            _logger.LogInformation("Document {DocumentId} not found", id);

            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            notFound.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var errorResponse = new ErrorResponse
            {
                Message = $"Document '{id}' was not found.",
                ErrorCode = "DOCUMENT_NOT_FOUND"
            };
            await notFound.WriteStringAsync(
                JsonSerializer.Serialize(errorResponse, JsonOptions)).ConfigureAwait(false);

            return notFound;
        }

        _logger.LogInformation(
            "Found document {DocumentId} with status {Status}",
            id, document.Status);

        var okResponse = req.CreateResponse(HttpStatusCode.OK);
        okResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await okResponse.WriteStringAsync(
            JsonSerializer.Serialize(document, JsonOptions)).ConfigureAwait(false);

        return okResponse;
    }
}
