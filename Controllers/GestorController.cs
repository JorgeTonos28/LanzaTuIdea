using LanzaTuIdea.Api.Data;
using LanzaTuIdea.Api.Models;
using LanzaTuIdea.Api.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace LanzaTuIdea.Api.Controllers;

[ApiController]
[Authorize(Roles = AppConstants.Roles.Gestor)]
[Route("api/gestor")]
public class GestorController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<GestorController> _logger;

    public GestorController(AppDbContext context, ILogger<GestorController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<GestorDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        var gestor = await GetCurrentUserAsync(cancellationToken);
        if (gestor is null)
        {
            return Unauthorized();
        }

        var totalAsignadas = await _context.Ideas.CountAsync(i => i.AssignedToUserId == gestor.Id, cancellationToken);
        var totalRegistradas = await _context.Ideas.CountAsync(i => i.CreatedByUserId == gestor.Id, cancellationToken);
        var ideasEnProceso = await _context.Ideas.CountAsync(
            i => i.AssignedToUserId == gestor.Id && i.Status == AppConstants.Status.EnEjecucion,
            cancellationToken);

        return new GestorDashboardDto(totalAsignadas, totalRegistradas, ideasEnProceso);
    }

    [HttpGet("ideas/assigned")]
    public async Task<ActionResult<IReadOnlyList<GestorIdeaSummaryDto>>> AssignedIdeas(CancellationToken cancellationToken)
    {
        var gestor = await GetCurrentUserAsync(cancellationToken);
        if (gestor is null)
        {
            return Unauthorized();
        }

        var ideas = await (from idea in _context.Ideas.AsNoTracking()
                           where idea.AssignedToUserId == gestor.Id
                           join user in _context.AppUsers.AsNoTracking() on idea.CreatedByUserId equals user.Id into userGroup
                           from user in userGroup.DefaultIfEmpty()
                           join employee in _context.Employees.AsNoTracking() on idea.CodigoEmpleado equals employee.Codigo_Empleado into empGroup
                           from employee in empGroup.DefaultIfEmpty()
                           orderby idea.CreatedAt descending
                           select new GestorIdeaSummaryDto(
                               idea.Id,
                               idea.CreatedAt,
                               idea.Descripcion,
                               idea.Status,
                               idea.CodigoEmpleado,
                               employee != null
                                   ? (employee.Nombre + " " + employee.Apellido1 + " " + employee.Apellido2)
                                   : user != null ? user.NombreCompleto : null,
                               employee != null ? employee.E_Mail : null,
                               employee != null ? employee.Departamento : null))
            .ToListAsync(cancellationToken);

        return ideas;
    }

    [HttpGet("ideas/{id:int}")]
    public async Task<ActionResult<IdeaDetailDto>> GetIdea(int id, CancellationToken cancellationToken)
    {
        var gestor = await GetCurrentUserAsync(cancellationToken);
        if (gestor is null)
        {
            return Unauthorized();
        }

        var idea = await _context.Ideas
            .Include(i => i.CreatedByUser)
            .Include(i => i.AssignedToUser)
            .Include(i => i.History)
            .ThenInclude(h => h.ChangedByUser)
            .Include(i => i.Comments)
            .ThenInclude(c => c.CommentedByUser)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (idea is null)
        {
            return NotFound();
        }

        if (idea.AssignedToUserId != gestor.Id)
        {
            return Forbid();
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Codigo_Empleado == idea.CodigoEmpleado, cancellationToken);
        var nombreCompleto = employee?.NombreCompleto ?? idea.CreatedByUser?.NombreCompleto;

        var history = idea.History
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new IdeaHistoryDto(
                h.ChangedAt,
                h.ChangedByUser?.NombreCompleto
                    ?? h.ChangedByUser?.UserName
                    ?? "Sistema",
                h.ChangeType,
                h.Notes))
            .ToList();

        var comments = idea.Comments
            .OrderByDescending(c => c.CommentedAt)
            .Select(c => new IdeaCommentDto(
                c.Id,
                c.CommentedAt,
                c.CommentedByRole,
                c.CommentedByName,
                c.Comment))
            .ToList();

        return new IdeaDetailDto(
            idea.Id,
            idea.CreatedAt,
            idea.Descripcion,
            idea.Problema,
            idea.Detalle,
            idea.Status,
            idea.Clasificacion,
            idea.CreatedByUser?.Instancia,
            idea.Via,
            idea.AdminComment,
            idea.AssignedToUserId,
            idea.AssignedToUser?.NombreCompleto ?? idea.AssignedToUser?.UserName,
            idea.CodigoEmpleado,
            nombreCompleto,
            history,
            comments);
    }

    [HttpGet("employees/search")]
    public async Task<ActionResult<EmployeeLookupDto>> SearchEmployee([FromQuery] string? codigo, [FromQuery] string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigo) && string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "El código o correo es requerido." });
        }

        Employee? employee = null;
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Codigo_Empleado == codigo.Trim(), cancellationToken);
        }

        if (employee is null && !string.IsNullOrWhiteSpace(email))
        {
            var normalized = email.Trim();
            employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.E_Mail == normalized, cancellationToken);
        }

        if (employee is null)
        {
            return NotFound();
        }

        return new EmployeeLookupDto(
            employee.Codigo_Empleado,
            employee.NombreCompleto,
            employee.E_Mail,
            employee.Departamento);
    }

    [HttpPost("ideas/manual")]
    public async Task<IActionResult> ManualIdea([FromBody] GestorIdeaManualRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion)
            || string.IsNullOrWhiteSpace(request.Problema)
            || string.IsNullOrWhiteSpace(request.Detalle)
            || string.IsNullOrWhiteSpace(request.Clasificacion))
        {
            return BadRequest(new { message = "Descripción, problema, detalle y clasificación son requeridos." });
        }

        if (request.Descripcion.Length > 500 || request.Problema.Length > 1000 || request.Detalle.Length > 4000)
        {
            return BadRequest(new { message = "Descripción, problema o detalle exceden el límite permitido." });
        }

        var gestor = await GetCurrentUserAsync(cancellationToken);
        if (gestor is null)
        {
            return Unauthorized();
        }

        var instanciaGestor = gestor.Instancia?.Trim();
        if (string.IsNullOrWhiteSpace(instanciaGestor))
        {
            return BadRequest(new { message = "El gestor no tiene una instancia asignada." });
        }

        if (string.IsNullOrWhiteSpace(request.CodigoEmpleado) && string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Debe indicar código de empleado o correo." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var employee = await FindEmployeeAsync(request, cancellationToken);
            if (employee is null)
            {
                return NotFound(new { message = "No se encontró el colaborador indicado." });
            }

            var codigoEmpleado = TrimTo(employee.Codigo_Empleado, 20) ?? string.Empty;
            var nombreCompleto = employee.NombreCompleto;
            var email = TrimTo(employee.E_Mail, 200);
            var departamento = TrimTo(employee.Departamento, 200);

            if (string.IsNullOrWhiteSpace(codigoEmpleado) || string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return BadRequest(new { message = "El empleado seleccionado no tiene datos completos." });
            }

            var userName = NormalizeUserName(email ?? request.Email ?? codigoEmpleado);
            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest(new { message = "No fue posible generar el usuario del colaborador." });
            }

            var targetUser = await _context.AppUsers
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

            if (targetUser is null)
            {
                targetUser = new AppUser
                {
                    UserName = userName,
                    Codigo_Empleado = codigoEmpleado,
                    NombreCompleto = TrimTo(nombreCompleto, 200),
                    Instancia = TrimTo(instanciaGestor, 200),
                    IsActive = true,
                    LastLoginAt = null
                };
                _context.AppUsers.Add(targetUser);
                await EnsureRoleAsync(targetUser, AppConstants.Roles.Ideador, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                targetUser.Codigo_Empleado = codigoEmpleado;
                targetUser.NombreCompleto = TrimTo(nombreCompleto, 200);
                targetUser.Instancia = TrimTo(instanciaGestor, 200);
                await EnsureRoleAsync(targetUser, AppConstants.Roles.Ideador, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var clasificacion = await _context.Classifications
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Activo && c.Nombre == request.Clasificacion.Trim(), cancellationToken);
            if (clasificacion is null)
            {
                return BadRequest(new { message = "La clasificación seleccionada no es válida." });
            }

            var idea = new Idea
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = targetUser.Id,
                CodigoEmpleado = codigoEmpleado,
                Descripcion = TrimTo(request.Descripcion, 500) ?? string.Empty,
                Problema = TrimTo(request.Problema, 1000) ?? string.Empty,
                Detalle = TrimTo(request.Detalle, 4000) ?? string.Empty,
                Status = AppConstants.Status.Registrada,
                Clasificacion = clasificacion.Nombre,
                Via = TrimTo(request.Via, 100) ?? "Gestor",
                AssignedToUserId = gestor.Id
            };

            idea.History.Add(new IdeaHistory
            {
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = gestor.Id,
                ChangeType = "Registro Gestor",
                Notes = "Registro manual con asignación automática."
            });

            _context.Ideas.Add(idea);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error registrando idea manual del gestor.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No fue posible registrar la idea del gestor." });
        }
    }

    private async Task<Employee?> FindEmployeeAsync(GestorIdeaManualRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.CodigoEmpleado))
        {
            var codigo = request.CodigoEmpleado.Trim();
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Codigo_Empleado == codigo, cancellationToken);
            if (employee is not null)
            {
                return employee;
            }

            if (string.IsNullOrWhiteSpace(request.NombreCompleto)
                || string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Departamento))
            {
                return null;
            }

            var parsed = ParseNombreCompleto(request.NombreCompleto);
            employee = new Employee
            {
                Codigo_Empleado = TrimTo(codigo, 20) ?? string.Empty,
                Nombre = TrimTo(parsed.Nombre, 100) ?? string.Empty,
                Apellido1 = TrimTo(parsed.Apellido1, 100) ?? string.Empty,
                Apellido2 = TrimTo(parsed.Apellido2, 100) ?? string.Empty,
                E_Mail = TrimTo(request.Email, 200) ?? string.Empty,
                Departamento = TrimTo(request.Departamento, 200) ?? string.Empty,
                Estatus = "A"
            };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync(cancellationToken);
            return employee;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            return await _context.Employees.FirstOrDefaultAsync(e => e.E_Mail == email, cancellationToken);
        }

        return null;
    }

    private async Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return await _context.AppUsers.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
    }

    private async Task EnsureRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role is null)
        {
            role = new Role { Name = roleName };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!user.UserRoles.Any(ur => ur.RoleId == role.Id))
        {
            user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
        }
    }

    private static (string Nombre, string Apellido1, string Apellido2) ParseNombreCompleto(string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            return ("", "", "");
        }

        var parts = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return (parts[0], "", "");
        }

        if (parts.Length == 2)
        {
            return (parts[0], parts[1], "");
        }

        return (parts[0], parts[1], string.Join(" ", parts.Skip(2)));
    }

    private static string NormalizeUserName(string userName)
    {
        var trimmed = userName.Trim();
        var atIndex = trimmed.IndexOf('@');
        return atIndex > 0 ? trimmed[..atIndex] : trimmed;
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
