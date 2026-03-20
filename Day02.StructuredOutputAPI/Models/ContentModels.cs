namespace Day02.StructuredOutputAPI.Models;

// ── Request Models ────────────────────────────────────────────────────────────
public record BlogTitlesRequest(string Topic, string Audience = "general");
public record SummarizeRequest(string Text);
public record ExtractRequest(string Text, string WhatToExtract);

// ── Response Models ───────────────────────────────────────────────────────
public record BlogTitlesResponse
{
    public List<string> Titles { get; init; } = new();
    public string Topic { get; init; } = "";
    public int TokensUsed { get; init; }
}

public record SummaryResponse
{
    public string Summary { get; init; } = "";
    public List<string> KeyPoints { get; init; } = new();
    public string Sentiment { get; init; } = ""; // positive / neutral / negative
    public int TokensUsed { get; init; }
}

public record ExtractResponse
{
    public Dictionary<string, string> ExtractedData { get; init; } = new();
    public int TokensUsed { get; init; }
}

// ── Internal LLM response shapes (what we ask the model to return) ────────────
public record BlogTitlesLLMResponse(List<string> Titles);
public record SummaryLLMResponse(string Summary, List<string> KeyPoints, string Sentiment);
public record ExtractLLMResponse(Dictionary<string, string> ExtractedData);