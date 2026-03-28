using Day04.RAGPipeline.Models;
using Day04.RAGPipeline.Services;
using Microsoft.AspNetCore.Mvc;

namespace Day04.RAGPipeline.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RAGController : ControllerBase
{
    private readonly RAGService _rag;
    private readonly ILogger<RAGController> _logger;
    private readonly IWebHostEnvironment _env;

    public RAGController(RAGService rag, ILogger<RAGController> logger, IWebHostEnvironment env)
    {
        _rag = rag;
        _logger = logger;
        _env = env;
    }

    // POST api/rag/ingest-text
    // Ingest raw text directly
    [HttpPost("ingest-text")]
    public async Task<ActionResult<IngestResponse>> IngestText([FromBody] IngestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        var result = await _rag.IngestAsync(request);
        return Ok(result);
    }

    // POST api/rag/ingest-sample
    // Ingest the sample document we created
    [HttpPost("ingest-sample")]
    public async Task<ActionResult<IngestResponse>> IngestSample()
    {
        var filePath = Path.Combine(_env.ContentRootPath, "SampleDocs", "company-policy.txt");

        if (!System.IO.File.Exists(filePath))
            return NotFound("Sample document not found. Make sure company-policy.txt is in SampleDocs folder.");

        var text = await System.IO.File.ReadAllTextAsync(filePath);
        var request = new IngestRequest { Text = text, FileName = "company-policy.txt" };
        var result = await _rag.IngestAsync(request);
        return Ok(result);
    }

    // POST api/rag/query
    // Ask a question about ingested documents
    [HttpPost("query")]
    public async Task<ActionResult<QueryResponse>> Query([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question cannot be empty.");

        var result = await _rag.QueryAsync(request);
        return Ok(result);
    }
}