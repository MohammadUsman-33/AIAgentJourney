using Day04.RAGPipeline.Models;

namespace Day04.RAGPipeline.Services;

public class DocumentChunkerService
{
    // Rough token estimate: 1 token ≈ 4 characters
    private const int CharsPerToken = 4;
    private const int ChunkTokens = 200;  // target chunk size
    private const int OverlapTokens = 20;   // overlap between chunks

    private readonly int _chunkSize;
    private readonly int _overlapSize;

    public DocumentChunkerService()
    {
        _chunkSize = ChunkTokens * CharsPerToken; // 2000 chars
        _overlapSize = OverlapTokens * CharsPerToken; // 200 chars
    }

    //public List<DocumentChunk> ChunkDocument(string text, string fileName)
    //{
    //    // Clean up whitespace
    //    text = CleanText(text);

    //    var chunks = new List<DocumentChunk>();
    //    int position = 0;
    //    int index = 0;

    //    while (position < text.Length)
    //    {
    //        // Calculate end position for this chunk
    //        int end = Math.Min(position + _chunkSize, text.Length);

    //        // Try to end at a sentence boundary (. or \n)
    //        // instead of cutting mid-sentence
    //        if (end < text.Length)
    //            end = FindNaturalBreak(text, end);

    //        var chunkText = text[position..end].Trim();

    //        if (!string.IsNullOrWhiteSpace(chunkText))
    //        {
    //            chunks.Add(new DocumentChunk
    //            {
    //                Text = chunkText,
    //                FileName = fileName,
    //                ChunkIndex = index++,
    //                TotalChunks = 0  // will be set after all chunks created
    //            });
    //        }

    //        // Move forward but keep overlap
    //        position = end - _overlapSize;

    //        // Safety: always move forward at least 1 char
    //        if (position >= end) position = end;
    //    }

    //    // Now we know total chunks — update each
    //    var total = chunks.Count;
    //    return chunks.Select(c => c with { TotalChunks = total }).ToList();
    ////}
    //public List<DocumentChunk> ChunkDocument(string text, string fileName)
    //{
    //    // Clean up whitespace
    //    text = CleanText(text);

    //    var chunks = new List<DocumentChunk>();
    //    int position = 0;
    //    int index = 0;

    //    while (position < text.Length)
    //    {
    //        // Calculate end position for this chunk
    //        int end = Math.Min(position + _chunkSize, text.Length);

    //        // Try to end at a sentence boundary
    //        if (end < text.Length)
    //            end = FindNaturalBreak(text, end);

    //        // Safety: end must always be greater than position
    //        if (end <= position)
    //            end = Math.Min(position + _chunkSize, text.Length);

    //        var chunkText = text[position..end].Trim();

    //        if (!string.IsNullOrWhiteSpace(chunkText))
    //        {
    //            chunks.Add(new DocumentChunk
    //            {
    //                Text = chunkText,
    //                FileName = fileName,
    //                ChunkIndex = index++,
    //                TotalChunks = 0
    //            });
    //        }

    //        // ✅ FIX: ensure position never goes negative
    //        int nextPosition = end - _overlapSize;
    //        position = Math.Max(nextPosition, end - (end - position) / 2);

    //        // Safety: always move forward at least 1 character
    //        if (position >= end) position = end;
    //    }

    //    // Update total chunks count
    //    var total = chunks.Count;
    //    return chunks.Select(c => c with { TotalChunks = total }).ToList();
    //}

    public List<DocumentChunk> ChunkDocument(string text, string fileName)
    {
        text = CleanText(text);

        var chunks = new List<DocumentChunk>();
        int position = 0;
        int index = 0;

        while (position < text.Length)
        {
            // Calculate end position for this chunk
            int end = Math.Min(position + _chunkSize, text.Length);

            // Try to end at a sentence boundary
            if (end < text.Length)
                end = FindNaturalBreak(text, end);

            // Safety: end must always be greater than position
            if (end <= position)
                end = Math.Min(position + _chunkSize, text.Length);

            var chunkText = text[position..end].Trim();

            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                chunks.Add(new DocumentChunk
                {
                    Text = chunkText,
                    FileName = fileName,
                    ChunkIndex = index++,
                    TotalChunks = 0
                });
            }

            // ✅ KEY FIX: if we reached the end of the document — stop immediately
            if (end >= text.Length) break;

            // Move forward with overlap for mid-document chunks only
            position = end - _overlapSize;

            // Safety: never go backwards past where we started this chunk
            if (position <= 0 || position >= end)
                position = end;
        }

        var total = chunks.Count;
        return chunks.Select(c => c with { TotalChunks = total }).ToList();
    }
    // Find the nearest sentence end before the cut point
    private static int FindNaturalBreak(string text, int position)
    {
        // Look back up to 200 chars for a sentence end
        int lookback = Math.Max(0, position - 200);
        for (int i = position; i > lookback; i--)
        {
            if (text[i] == '.' || text[i] == '\n')
                return i + 1;
        }
        return position; // no natural break found — cut anyway
    }

    private static string CleanText(string text)
    {
        // Collapse multiple newlines and spaces
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, @" {2,}", " ");
        return text.Trim();
    }
}