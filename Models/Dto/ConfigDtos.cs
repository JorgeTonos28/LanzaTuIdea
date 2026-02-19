namespace LanzaTuIdea.Api.Models.Dto;

public record CatalogItemDto(int Id, string Nombre);
public record CreateCatalogItemRequest(string Nombre);

public record ClassificationCatalogItemDto(
    int Id,
    string Nombre,
    string Proceso,
    string Subproceso,
    string? Icono,
    string? Descripcion
);
