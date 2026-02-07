namespace LanzaTuIdea.Api.Models.Dto;

public record IdeaCreateRequest(
    string Descripcion,
    string Detalle,
    string? Instancia,
    string? NombreCompleto,
    string? Email,
    string? Departamento
);

public record IdeaSummaryDto(
    int Id,
    DateTime CreatedAt,
    string Descripcion,
    string Status
);

public record IdeaAdminSummaryDto(
    int Id,
    DateTime CreatedAt,
    string Descripcion,
    string Status,
    string CodigoEmpleado,
    string? NombreCompleto,
    string? Email,
    string? Departamento,
    string? Clasificacion
);

public record IdeaDetailDto(
    int Id,
    DateTime CreatedAt,
    string Descripcion,
    string Detalle,
    string Status,
    string? Clasificacion,
    string? Instancia,
    string? Via,
    string? AdminComment,
    int? AssignedToUserId,
    string? AssignedToName,
    string CodigoEmpleado,
    string? NombreCompleto,
    IReadOnlyList<IdeaHistoryDto> History,
    IReadOnlyList<IdeaCommentDto> Comments
);

public record IdeaHistoryDto(
    DateTime ChangedAt,
    string ChangedBy,
    string ChangeType,
    string? Notes
);

public record IdeaCommentDto(
    int Id,
    DateTime CommentedAt,
    string CommentedByRole,
    string CommentedByName,
    string Comment
);

public record IdeaReviewRequest(
    string Status,
    string? Clasificacion,
    string? Via,
    string? AdminComment
);

public record IdeaCommentRequest(string Comment);

public record IdeaManualRequest(
    string CodigoEmpleado,
    string Descripcion,
    string Detalle,
    string? Via,
    string? AdminComment,
    string? Clasificacion,
    string? Instancia,
    string? NombreCompleto,
    string? Email,
    string? Departamento
);
