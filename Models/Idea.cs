namespace LanzaTuIdea.Api.Models;

public class Idea
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;
    public string CodigoEmpleado { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Problema { get; set; } = "";
    public string Detalle { get; set; } = "";
    public string Status { get; set; } = "Registrada";
    public string? Clasificacion { get; set; }
    public string? Via { get; set; }
    public string? AdminComment { get; set; }
    public int? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    public List<IdeaHistory> History { get; set; } = new();
    public List<IdeaComment> Comments { get; set; } = new();
}
