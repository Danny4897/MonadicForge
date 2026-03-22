using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MonadicSharp;

namespace MonadicLeaf.Modules.Analyze.Application.Llm;

/// <summary>
/// Thin wrapper over the Anthropic Messages API.
/// Returns Result&lt;string&gt; (the raw text content) — never throws.
/// </summary>
public sealed class AnthropicClient
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;

    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-sonnet-4-6";
    private const string AnthropicVersion = "2023-06-01";

    public AnthropicClient(HttpClient http, string? apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>Sends a message and returns the first text content block.</summary>
    public Task<Result<string>> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            return Task.FromResult(Result<string>.Failure(
                Error.Create("Anthropic API key not configured", "LEAF_LLM_NOT_CONFIGURED")));

        return Try.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", AnthropicVersion);

            var body = new
            {
                model = Model,
                max_tokens = 2048,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userMessage } }
            };

            request.Content = JsonContent.Create(body);

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var parsed = await response.Content
                .ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct);

            return parsed?.Content.FirstOrDefault()?.Text
                ?? throw new InvalidOperationException("Empty response from Anthropic");
        });
    }

    // ─── Response DTOs ────────────────────────────────────────────────────────

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock> Content { get; set; } = [];
    }

    private sealed class ContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;
    }
}
