namespace LanzaTuIdea.Api.Models;

public class Classification
{
    public int Id { get; set; }
    public string Proceso { get; set; } = "";
    public string Subproceso { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Icono { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}
