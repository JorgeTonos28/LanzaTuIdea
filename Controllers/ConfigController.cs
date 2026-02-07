using LanzaTuIdea.Api.Data;
using LanzaTuIdea.Api.Models;
using LanzaTuIdea.Api.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanzaTuIdea.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ConfigController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("settings")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<string, string>>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _context.SystemSettings
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    [HttpPost("upload-logo")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest(new { message = "Invalid file type." });

        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsPath))
            Directory.CreateDirectory(uploadsPath);

        var fileName = $"logo_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var fileUrl = $"/uploads/{fileName}";
        await UpdateSettingAsync("LogoUrl", fileUrl, cancellationToken);

        return Ok(new { url = fileUrl });
    }

    [HttpPost("upload-favicon")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public async Task<IActionResult> UploadFavicon(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest(new { message = "Invalid file type." });

        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsPath))
            Directory.CreateDirectory(uploadsPath);

        var fileName = $"favicon_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var fileUrl = $"/uploads/{fileName}";
        await UpdateSettingAsync("FaviconUrl", fileUrl, cancellationToken);

        return Ok(new { url = fileUrl });
    }

    private async Task UpdateSettingAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting == null)
        {
            setting = new SystemSetting { Key = key, Value = value };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    [HttpGet("classifications")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemDto>>> GetClassifications(CancellationToken cancellationToken)
    {
        var items = await _context.Classifications
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogItemDto(c.Id, c.Nombre))
            .ToListAsync(cancellationToken);

        return items;
    }

    [HttpPost("classifications")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public async Task<ActionResult<CatalogItemDto>> CreateClassification([FromBody] CreateCatalogItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new { message = "El nombre es requerido." });
        }

        var nombre = TrimTo(request.Nombre, 200);
        if (nombre is null)
        {
            return BadRequest(new { message = "El nombre es requerido." });
        }

        var classification = new Classification { Nombre = nombre, Activo = true };
        _context.Classifications.Add(classification);
        await _context.SaveChangesAsync(cancellationToken);
        return new CatalogItemDto(classification.Id, classification.Nombre);
    }

    [HttpDelete("classifications/{id:int}")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public async Task<IActionResult> DeleteClassification(int id, CancellationToken cancellationToken)
    {
        var classification = await _context.Classifications.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (classification is null)
        {
            return NotFound();
        }

        classification.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpGet("instances")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemDto>>> GetInstances(CancellationToken cancellationToken)
    {
        var items = await _context.Instances
            .AsNoTracking()
            .Where(i => i.Activo)
            .OrderBy(i => i.Nombre)
            .Select(i => new CatalogItemDto(i.Id, i.Nombre))
            .ToListAsync(cancellationToken);

        return items;
    }

    [HttpPost("instances")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public async Task<ActionResult<CatalogItemDto>> CreateInstance([FromBody] CreateCatalogItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new { message = "El nombre es requerido." });
        }

        var nombre = TrimTo(request.Nombre, 200);
        if (nombre is null)
        {
            return BadRequest(new { message = "El nombre es requerido." });
        }

        var instance = new Instance { Nombre = nombre, Activo = true };
        _context.Instances.Add(instance);
        await _context.SaveChangesAsync(cancellationToken);
        return new CatalogItemDto(instance.Id, instance.Nombre);
    }

    [HttpDelete("instances/{id:int}")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public async Task<IActionResult> DeleteInstance(int id, CancellationToken cancellationToken)
    {
        var instance = await _context.Instances.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (instance is null)
        {
            return NotFound();
        }

        instance.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    private static string? TrimTo(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }
}
