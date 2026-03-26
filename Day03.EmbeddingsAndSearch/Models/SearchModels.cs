namespace Day03.EmbeddingsAndSearch.Models;

public record TextEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; init; } = "";
    public string Source { get; init; } = "manual";
    public string Category { get; init; } = "general";
}

public record SearchResult
{
    public string Text { get; init; } = "";
    public string Source { get; init; } = "";
    public string Category { get; init; } = "";
    public double Score { get; init; }
}