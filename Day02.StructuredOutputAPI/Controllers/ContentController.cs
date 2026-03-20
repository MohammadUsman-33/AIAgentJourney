using Day02.StructuredOutputAPI.Models;
using Day02.StructuredOutputAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Day02.StructuredOutputAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly StructuredAIService _ai;
    private readonly ILogger<ContentController> _logger;

    public ContentController(StructuredAIService ai, ILogger<ContentController> logger)
    {
        _ai = ai;
        _logger = logger;
    }

    // POST api/content/blog-titles
    [HttpPost("blog-titles")]
    public async Task<ActionResult<BlogTitlesResponse>> GetBlogTitles(
        [FromBody] BlogTitlesRequest request)
    {
        _logger.LogInformation("Blog titles requested for topic: {Topic}", request.Topic);
        var result = await _ai.GetBlogTitlesAsync(request);
        return Ok(result);
    }

    // POST api/content/summarize
    [HttpPost("summarize")]
    public async Task<ActionResult<SummaryResponse>> Summarize(
        [FromBody] SummarizeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        _logger.LogInformation("Summarize requested, text length: {Len}", request.Text.Length);
        var result = await _ai.SummarizeAsync(request);
        return Ok(result);
    }

    // POST api/content/extract
    [HttpPost("extract")]
    public async Task<ActionResult<ExtractResponse>> Extract(
        [FromBody] ExtractRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        _logger.LogInformation("Extract requested: {What}", request.WhatToExtract);
        var result = await _ai.ExtractAsync(request);
        return Ok(result);
    }
}