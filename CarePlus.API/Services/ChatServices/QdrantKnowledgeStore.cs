using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CarePlus.API.Services.ChatServices.Interface;
using Microsoft.Extensions.Options;

namespace CarePlus.API.Services.ChatServices;

public sealed class QdrantKnowledgeStore : IVectorKnowledgeStore
{
    private readonly HttpClient _httpClient;
    private readonly QdrantOptions _options;

    public QdrantKnowledgeStore(
        HttpClient httpClient,
        IOptions<QdrantOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task ReplaceDocumentAsync(KnowledgeDocument document, IReadOnlyList<KnowledgeChunk> chunks, CancellationToken cancellationToken)
    {
        await EnsureCollectionAsync(cancellationToken);
        await EnsureDocumentIdIndexAsync(cancellationToken);
        var collection = Uri.EscapeDataString(_options.Collection);

        using (var deleteRequest = CreateRequest(HttpMethod.Post, $"/collections/{collection}/points/delete?wait=true"))
        {
            deleteRequest.Content = JsonContent.Create(new
            {
                filter = new
                {
                    must = new[] { new { key = "documentId", match = new { value = document.Id } } }
                }
            });
            using var deleteResponse = await _httpClient.SendAsync(deleteRequest, cancellationToken);
            deleteResponse.EnsureSuccessStatusCode();
        }

        var points = chunks.Select(chunk => new
        {
            id = DeterministicId($"{document.Id}:{chunk.Index}"),
            vector = new
            {
                text = chunk.Content,
                model = _options.InferenceModel
            },
            payload = new
            {
                documentId = document.Id,
                document.Title,
                document.Source,
                chunkIndex = chunk.Index,
                content = chunk.Content
            }
        }).ToArray();

        using var upsertRequest = CreateRequest(HttpMethod.Put, $"/collections/{collection}/points?wait=true");
        upsertRequest.Content = JsonContent.Create(new { points });
        using var upsertResponse = await _httpClient.SendAsync(upsertRequest, cancellationToken);
        upsertResponse.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        await EnsureCollectionAsync(cancellationToken);
        var collection = Uri.EscapeDataString(_options.Collection);
        using var request = CreateRequest(HttpMethod.Post, $"/collections/{collection}/points/query");
        request.Content = JsonContent.Create(new
        {
            query = new
            {
                text = query,
                model = _options.InferenceModel
            },
            limit = Math.Clamp(_options.SearchLimit, 1, 10),
            score_threshold = Math.Clamp(_options.MinimumScore, 0, 1),
            with_payload = true,
            with_vector = false
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!json.RootElement.TryGetProperty("result", out var result)
            || !result.TryGetProperty("points", out var points)
            || points.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var matches = new List<KnowledgeMatch>();
        foreach (var item in points.EnumerateArray())
        {
            if (!item.TryGetProperty("payload", out var payload))
            {
                continue;
            }
            matches.Add(new KnowledgeMatch(
                ReadString(payload, "documentId"),
                ReadString(payload, "title"),
                ReadString(payload, "source"),
                ReadString(payload, "content"),
                item.TryGetProperty("score", out var score) ? score.GetDouble() : 0));
        }
        return matches;
    }

    private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Url) || string.IsNullOrWhiteSpace(_options.Collection))
        {
            throw new InvalidOperationException("Qdrant is not configured.");
        }

        var collection = Uri.EscapeDataString(_options.Collection);
        using var checkRequest = CreateRequest(HttpMethod.Get, $"/collections/{collection}");
        using var checkResponse = await _httpClient.SendAsync(checkRequest, cancellationToken);
        if (checkResponse.IsSuccessStatusCode)
        {
            return;
        }
        if (checkResponse.StatusCode != HttpStatusCode.NotFound)
        {
            checkResponse.EnsureSuccessStatusCode();
        }

        using var createRequest = CreateRequest(HttpMethod.Put, $"/collections/{collection}");
        createRequest.Content = JsonContent.Create(new
        {
            vectors = new { size = _options.VectorSize, distance = "Cosine" }
        });
        using var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);
        createResponse.EnsureSuccessStatusCode();
    }

    private async Task EnsureDocumentIdIndexAsync(CancellationToken cancellationToken)
    {
        var collection = Uri.EscapeDataString(_options.Collection);
        using var request = CreateRequest(HttpMethod.Put, $"/collections/{collection}/index?wait=true");
        request.Content = JsonContent.Create(new
        {
            field_name = "documentId",
            field_schema = "keyword"
        });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"{_options.Url.TrimEnd('/')}{path}");
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("api-key", _options.ApiKey);
        }
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static string ReadString(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string DeterministicId(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]).ToString();
    }
}

