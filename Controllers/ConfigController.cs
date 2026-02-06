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

    [HttpGet("branding")]
    [AllowAnonymous]
    public async Task<ActionResult<BrandingDto>> GetBranding(CancellationToken cancellationToken)
    {
        var branding = await _context.AppBrandings
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new BrandingDto(BuildPublicUrl(branding?.LogoPath), BuildPublicUrl(branding?.FaviconPath));
    }

    [HttpPost("branding")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public async Task<ActionResult<BrandingDto>> UpdateBranding(
        [FromForm] IFormFile? logo,
        [FromForm] IFormFile? favicon,
        CancellationToken cancellationToken)
    {
        if (logo is null && favicon is null)
        {
            return BadRequest(new { message = "Debes enviar un logo o favicon." });
        }

        var branding = await _context.AppBrandings.FirstOrDefaultAsync(cancellationToken);
        if (branding is null)
        {
            branding = new AppBranding { UpdatedAt = DateTime.UtcNow };
            _context.AppBrandings.Add(branding);
        }

        if (logo is not null)
        {
            if (!HasAllowedExtension(logo.FileName, [".png", ".jpg", ".jpeg"]))
            {
                return BadRequest(new { message = "El logo debe ser PNG o JPG." });
            }

            DeleteFileIfExists(branding.LogoPath);
            branding.LogoPath = await SaveFileAsync(logo, "logo", cancellationToken);
        }

        if (favicon is not null)
        {
            if (!HasAllowedExtension(favicon.FileName, [".png", ".ico"]))
            {
                return BadRequest(new { message = "El favicon debe ser PNG o ICO." });
            }

            DeleteFileIfExists(branding.FaviconPath);
            branding.FaviconPath = await SaveFileAsync(favicon, "favicon", cancellationToken);
        }

        branding.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new BrandingDto(BuildPublicUrl(branding.LogoPath), BuildPublicUrl(branding.FaviconPath));
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

    private string? BuildPublicUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var basePath = HttpContext.Request.PathBase.HasValue
            ? HttpContext.Request.PathBase.Value
            : string.Empty;

        return $"{basePath}{path}";
    }

    private async Task<string> SaveFileAsync(IFormFile file, string prefix, CancellationToken cancellationToken)
    {
        var uploadsRoot = Path.Combine(_environment.WebRootPath ?? "wwwroot", "branding");
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{prefix}-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/branding/{fileName}";
    }

    private void DeleteFileIfExists(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var trimmed = relativePath.TrimStart('/');
        var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", trimmed.Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    private static bool HasAllowedExtension(string fileName, string[] allowedExtensions)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }
}
