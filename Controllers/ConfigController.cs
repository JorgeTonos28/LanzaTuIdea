using LanzaTuIdea.Api.Data;
using LanzaTuIdea.Api.Models;
using LanzaTuIdea.Api.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace LanzaTuIdea.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private const string DefaultLogoPath = "/assets/branding/logo-placeholder.svg";
    private const string DefaultFaviconPath = "/assets/branding/favicon-placeholder.svg";

    public ConfigController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
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

    [HttpGet("branding")]
    [AllowAnonymous]
    public async Task<ActionResult<BrandingSettingsDto>> GetBranding(CancellationToken cancellationToken)
    {
        var branding = await _context.BrandingSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var logoUrl = branding?.LogoPath ?? DefaultLogoPath;
        var faviconUrl = branding?.FaviconPath ?? DefaultFaviconPath;
        return new BrandingSettingsDto(logoUrl, faviconUrl);
    }

    [HttpPost("branding")]
    [Authorize(Roles = AppConstants.Roles.Admin)]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<BrandingSettingsDto>> UpdateBranding([FromForm] IFormFile? logo, [FromForm] IFormFile? favicon, CancellationToken cancellationToken)
    {
        if (logo is null && favicon is null)
        {
            return BadRequest(new { message = "Debe seleccionar al menos un archivo." });
        }

        var branding = await _context.BrandingSettings.FirstOrDefaultAsync(cancellationToken)
            ?? new AppBranding { UpdatedAt = DateTime.UtcNow };

        if (logo is not null)
        {
            var allowedLogoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg"
            };

            if (!TryGetExtension(logo, allowedLogoExtensions, out var logoExtension))
            {
                return BadRequest(new { message = "Formato de logo no permitido. Usa PNG o JPG." });
            }

            branding.LogoPath = await SaveAssetAsync(logo, "logo", logoExtension, cancellationToken);
        }

        if (favicon is not null)
        {
            var allowedFaviconExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".ico"
            };

            if (!TryGetExtension(favicon, allowedFaviconExtensions, out var faviconExtension))
            {
                return BadRequest(new { message = "Formato de favicon no permitido. Usa PNG o ICO." });
            }

            branding.FaviconPath = await SaveAssetAsync(favicon, "favicon", faviconExtension, cancellationToken);
        }

        branding.UpdatedAt = DateTime.UtcNow;

        if (branding.Id == 0)
        {
            _context.BrandingSettings.Add(branding);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new BrandingSettingsDto(
            branding.LogoPath ?? DefaultLogoPath,
            branding.FaviconPath ?? DefaultFaviconPath);
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

    private async Task<string> SaveAssetAsync(IFormFile file, string baseName, string extension, CancellationToken cancellationToken)
    {
        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
            : _environment.WebRootPath;

        var targetDirectory = Path.Combine(webRoot, "assets", "branding");
        Directory.CreateDirectory(targetDirectory);

        var fileName = $"{baseName}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(targetDirectory, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/assets/branding/{fileName}";
    }

    private static bool TryGetExtension(IFormFile file, HashSet<string> allowedExtensions, out string extension)
    {
        extension = Path.GetExtension(file.FileName);
        return !string.IsNullOrWhiteSpace(extension) && allowedExtensions.Contains(extension);
    }
}
