using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Attendance.Application.AiAssist;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Attendance.Infrastructure.AiAssist;

/// <summary>
/// Calls Anthropic Claude (claude-haiku-4-5) to infer how the customer's
/// arbitrary CSV header maps to our canonical columns. Single round-trip,
/// structured JSON response.
///
/// Set ANTHROPIC_API_KEY in env vars / user-secrets to enable.
/// If unset, this service throws -- callers should feature-flag the endpoint.
/// </summary>
public sealed class ClaudeSchemaInferenceService : ISchemaInferenceService
{
    private readonly HttpClient _http;
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeSchemaInferenceService> _log;

    public ClaudeSchemaInferenceService(
        HttpClient http,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeSchemaInferenceService> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<SchemaInferenceResult> InferAsync(string csvSample, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "ANTHROPIC_API_KEY not configured. Either set the env var or " +
                "disable the AI inference endpoint via feature flag.");
        }

        var systemPrompt =
            "You map arbitrary CSV column names to a canonical attendance schema. " +
            "Canonical columns: external_ref (the card or biometric id), " +
            "punch_at (the timestamp), device_id (the reader device), direction (IN/OUT). " +
            "Respond ONLY with valid JSON in this exact shape: " +
            "{\"external_ref_column\":\"...\",\"punch_at_column\":\"...\"," +
            "\"device_id_column\":\"...\",\"direction_column\":\"...\"," +
            "\"confidence\":0.0..1.0,\"reasoning\":\"one short sentence\"}";

        var userMessage = $"Map these column names. CSV sample:\n\n{csvSample}";

        var request = new ClaudeRequest(
            Model: _options.Model,
            MaxTokens: 256,
            System: systemPrompt,
            Messages: new[]
            {
                new ClaudeMessage("user", userMessage)
            });

        using var msg = new HttpRequestMessage(HttpMethod.Post, "messages")
        {
            Content = JsonContent.Create(request)
        };
        msg.Headers.Add("x-api-key", _options.ApiKey);
        msg.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _http.SendAsync(msg, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty response from Claude");

        var text = body.Content.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Claude returned no content");

        _log.LogDebug("Claude schema-inference raw response: {Body}", text);

        var parsed = JsonSerializer.Deserialize<RawInference>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Could not parse Claude response as JSON");

        return new SchemaInferenceResult(
            new SchemaMapping(
                parsed.ExternalRefColumn,
                parsed.PunchAtColumn,
                parsed.DeviceIdColumn,
                parsed.DirectionColumn),
            (decimal)parsed.Confidence,
            parsed.Reasoning);
    }

    // -- DTOs for Anthropic Messages API --

    private sealed record ClaudeRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] IReadOnlyList<ClaudeMessage> Messages);

    private sealed record ClaudeMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ClaudeResponse(
        [property: JsonPropertyName("content")] IReadOnlyList<ClaudeContentBlock> Content);

    private sealed record ClaudeContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string Text);

    private sealed record RawInference(
        [property: JsonPropertyName("external_ref_column")] string ExternalRefColumn,
        [property: JsonPropertyName("punch_at_column")] string PunchAtColumn,
        [property: JsonPropertyName("device_id_column")] string DeviceIdColumn,
        [property: JsonPropertyName("direction_column")] string DirectionColumn,
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("reasoning")] string? Reasoning);
}

public sealed class ClaudeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1/";
    public string Model { get; set; } = "claude-haiku-4-5";
}
