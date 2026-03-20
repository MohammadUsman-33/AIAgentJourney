using System.Text.Json;
using Day02.StructuredOutputAPI.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Day02.StructuredOutputAPI.Services;

public class StructuredAIService
{
    private readonly IChatCompletionService _chat;
    private readonly Kernel _kernel;
    private readonly ILogger<StructuredAIService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StructuredAIService(Kernel kernel, ILogger<StructuredAIService> logger)
    {
        _kernel = kernel;
        _chat = kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;
    }

    // ── Core retry method — the heart of structured output ───────────────────
    private async Task<T> GetStructuredResponseAsync<T>(
        string systemPrompt,
        string userPrompt,
        int maxRetries = 3)
    {
        var lastError = "";

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempt {Attempt}/{Max}", attempt, maxRetries);

                // On retry, append the previous error so model self-corrects
                var finalPrompt = attempt == 1
                    ? userPrompt
                    : $"{userPrompt}\n\nYour previous response failed JSON parsing with error: {lastError}\nReturn ONLY valid JSON this time. No extra text.";

                var history = new ChatHistory(systemPrompt);
                history.AddUserMessage(finalPrompt);

                var settings = new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        ["temperature"] = 0.9  // low temp = predictable JSON output
                    }
                };

                var response = await _chat.GetChatMessageContentAsync(history, settings, _kernel);
                var content = response.Content ?? "";

                // Strip markdown code fences if model wraps JSON in ```json ... ```
                content = StripCodeFences(content);

                _logger.LogInformation("Raw response: {Response}", content);

                return JsonSerializer.Deserialize<T>(content, JsonOpts)
                    ?? throw new JsonException("Deserialized to null");
            }
            catch (JsonException ex)
            {
                lastError = ex.Message;
                _logger.LogWarning("Attempt {Attempt} failed: {Error}", attempt, ex.Message);

                if (attempt == maxRetries)
                    throw new InvalidOperationException(
                        $"Model failed to return valid JSON after {maxRetries} attempts. Last error: {lastError}");
            }
        }

        throw new InvalidOperationException("Unexpected retry loop exit");
    }

    // ── Strip ```json ... ``` code fences from model response ─────────────────
    private static string StripCodeFences(string content)
    {
        content = content.Trim();

        if (content.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            content = content[7..];
        else if (content.StartsWith("```"))
            content = content[3..];

        if (content.EndsWith("```"))
            content = content[..^3];

        return content.Trim();
    }

    // ── Endpoint 1: Blog Titles ───────────────────────────────────────────────
    public async Task<BlogTitlesResponse> GetBlogTitlesAsync(BlogTitlesRequest request)
    {

        var jsonTemplate = """ {"titles": ["title1", "title2", "title3", "title4", "title5"]} """;
        var systemPrompt =
            """
            You are a content strategist. You ALWAYS respond with ONLY valid JSON.
            NEVER add any text before or after the JSON.
            NEVER wrap the JSON in code fences.
            """;

        var userPrompt =
            $"""
            Generate exactly 5 blog titles for the topic: {request.Topic}
            Target audience: {request.Audience}

            Return this EXACT JSON structure:
            { jsonTemplate }
            """;

        var result = await GetStructuredResponseAsync<BlogTitlesLLMResponse>(
            systemPrompt, userPrompt);

        return new BlogTitlesResponse
        {
            Titles = result.Titles,
            Topic = request.Topic,
            TokensUsed = result.Titles.Sum(t => t.Length / 4) // rough estimate
        };
    }

    // ── Endpoint 2: Summarize ─────────────────────────────────────────────────
    public async Task<SummaryResponse> SummarizeAsync(SummarizeRequest request)
    {


        var jsonTemplate = """
                            {
                              "summary": "2-3 sentence summary here",
                              "keyPoints": ["point 1", "point 2", "point 3"],
                              "sentiment": "positive OR neutral OR negative"
                            }
                            """;

        var systemPrompt =
            """
            You are a document analyst. You ALWAYS respond with ONLY valid JSON.
            NEVER add any text before or after the JSON.
            NEVER wrap the JSON in code fences.
            """;
        var userPrompt =
    $"""
    Analyze this text and return a summary:

    TEXT:
    {request.Text}

    Return this EXACT JSON structure:
    {jsonTemplate}
    
    """;
        //var userPrompt =
        //    $"""
        //    Analyze this text and return a summary:

        //    TEXT:
        //    {request.Text}

        //    Return this EXACT JSON structure:
        //    {{
        //      "summary":   "2-3 sentence summary here",
        //      "keyPoints": ["point 1", "point 2", "point 3"],
        //      "sentiment": "positive OR neutral OR negative"
        //    }}
        //    """;

        var result = await GetStructuredResponseAsync<SummaryLLMResponse>(
            systemPrompt, userPrompt);

        return new SummaryResponse
        {
            Summary = result.Summary,
            KeyPoints = result.KeyPoints,
            Sentiment = result.Sentiment,
            TokensUsed = request.Text.Length / 4
        };
    }

    // ── Endpoint 3: Extract ───────────────────────────────────────────────────
    public async Task<ExtractResponse> ExtractAsync(ExtractRequest request)
    {
        var jsonTemplate = """
                            "extractedData": {{
                              "field1": "value1",
                              "field2": "value2"
                            }}
                            """;
        var systemPrompt =
            """
            You are a data extraction specialist. You ALWAYS respond with ONLY valid JSON.
            NEVER add any text before or after the JSON.
            NEVER wrap the JSON in code fences.
            """;

        var userPrompt =
            $"""
            Extract the following information from the text below: {request.WhatToExtract}

            TEXT:
            {request.Text}

            Return this EXACT JSON structure:
            { jsonTemplate }
            Use the field names that match what was requested to extract.
            If a field is not found, use "not found" as the value.
            """;

        var result = await GetStructuredResponseAsync<ExtractLLMResponse>(
            systemPrompt, userPrompt);

        return new ExtractResponse
        {
            ExtractedData = result.ExtractedData,
            TokensUsed = request.Text.Length / 4
        };
    }
}