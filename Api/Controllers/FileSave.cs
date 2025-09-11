using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Services;
using Microsoft.Extensions.Configuration;
using Shared.Helpers;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesaveController : ControllerBase
{
    private readonly string _uploadPath;
    private readonly string _publicUrl;
    private readonly string[] _allowedExtensions;
    private readonly long _maxFileSize;
    private ILogger<FilesaveController> _logger;

    public FilesaveController(IConfiguration config, ILogger<FilesaveController> logger)
    {
        _logger = logger;
        _uploadPath = config["FileStorage:UploadPath"]!;
        _publicUrl = config["FileStorage:PublicUrl"]!;
        _allowedExtensions = config.GetSection("AllowedExtensions").Get<string[]>()!;
        _maxFileSize = config.GetValue<long>("MaxFileSizeBytes");

        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadPath);
    }

    [HttpGet]
    public ActionResult Get()
    {
        return Ok("I'm working oo");
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken token)
    {
        try
        {
            var result = await ProcessFile(file);

            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex);
        }        
    }

    private async Task<UploadResultDto> ProcessFile(IFormFile file)
    {
        // Validate size
        if (file.Length > _maxFileSize)
            return Failed($"File too large (max {_maxFileSize / (1024 * 1024)} MB)");

        // Validate extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
            return Failed($"Invalid extension: {ext}. Allowed: {string.Join(", ", _allowedExtensions)}");

        // Generate safe name
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(_uploadPath, fileName);
        _logger.LogInformation("File path: {0}, file name: {1}", filePath, fileName);

        try
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var request = HttpContext.Request;
            var url = $"{request.Scheme}://{request.Host}{_publicUrl}/{fileName}";

            _logger.LogInformation("File url: {0}", url);

            return new UploadResultDto
            {
                Success = true,
                FileName = file.FileName,
                PreviewUrl = url,
                Size = file.Length,
                ServerFileName = fileName   
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return Failed($"Server error: {ex.Message}");
        }
    }

    private UploadResultDto Failed(string error) => new() { Success = false, Error = error };
}


