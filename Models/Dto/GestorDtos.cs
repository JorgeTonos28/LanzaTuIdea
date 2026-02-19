namespace LanzaTuIdea.Api.Models.Dto;

public record GestorSummaryDto(
    int Id,
    string Nombre,
    string? Instancia,
    int IdeasAsignadas
);

public record IdeaAssignmentRequest(int GestorUserId);

public record GestorDashboardDto(
    int TotalAsignadas,
    int TotalRegistradas,
    int IdeasEnProceso
);

public record GestorIdeaSummaryDto(
    int Id,
    DateTime CreatedAt,
    string Descripcion,
    string Status,
    string CodigoEmpleado,
    string? NombreCompleto,
    string? Email,
    string? Departamento
);

public record GestorIdeaManualRequest(
    string? CodigoEmpleado,
    string? Email,
    string? NombreCompleto,
    string? Departamento,
    string Descripcion,
    string Problema,
    string Detalle,
    string? Clasificacion,
    string? Via
);
