namespace Day04.RAGPipeline.Models;

// ── Ingestion ─────────────────────────────────────────────────────────────────
public record IngestRequest
{
    public string Text { get; init; } = "";
    public string FileName { get; init; } = "manual";
}

public record IngestResponse
{
    public string FileName { get; init; } = "";
    public int TotalChunks { get; init; }
    public string Message { get; init; } = "";
}

// ── Query ─────────────────────────────────────────────────────────────────────
public record QueryRequest
{
    public string Question { get; init; } = "";
    public int MaxChunks { get; init; } = 3;
}

public record QueryResponse
{
    public string Answer { get; init; } = "";
    public List<Source> Sources { get; init; } = new();
    public bool HasContext { get; init; }
}

public record Source
{
    public string FileName { get; init; } = "";
    public int ChunkIndex { get; init; }
    public double Score { get; init; }
    public string Preview { get; init; } = ""; // first 100 chars of chunk
}

// ── Internal chunk model ──────────────────────────────────────────────────────
public record DocumentChunk
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; init; } = "";
    public string FileName { get; init; } = "";
    public int ChunkIndex { get; init; }
    public int TotalChunks { get; init; }
}