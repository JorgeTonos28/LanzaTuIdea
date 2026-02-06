namespace LanzaTuIdea.Api.Models.Dto;

public record CatalogItemDto(int Id, string Nombre);
public record CreateCatalogItemRequest(string Nombre);
public record BrandingDto(string? LogoUrl, string? FaviconUrl);
