using LanzaTuIdea.Api.Data;
using LanzaTuIdea.Api.Models;
using LanzaTuIdea.Api.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LanzaTuIdea.Api.Controllers;

[ApiController]
[Authorize(Roles = AppConstants.Roles.Admin + "," + AppConstants.Roles.Gestor)]
[Route("api/admin/ideas")]
public class AdminIdeaCommentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminIdeaCommentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<IdeaCommentDto>> AddComment(int id, [FromBody] IdeaCommentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            return BadRequest(new { message = "El comentario es requerido." });
        }

        if (request.Comment.Length > 2000)
        {
            return BadRequest(new { message = "El comentario excede el límite permitido." });
        }

        var currentUser = await GetCurrentUserWithRolesAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var idea = await _context.Ideas.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (idea is null)
        {
            return NotFound();
        }

        var comment = new IdeaComment
        {
            IdeaId = idea.Id,
            CommentedAt = DateTime.UtcNow,
            CommentedByUserId = currentUser.Id,
            CommentedByRole = ResolvePrimaryRole(currentUser),
            CommentedByName = currentUser.NombreCompleto ?? currentUser.UserName,
            Comment = request.Comment.Trim()
        };

        _context.IdeaComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        return new IdeaCommentDto(
            comment.Id,
            comment.CommentedAt,
            comment.CommentedByRole,
            comment.CommentedByName,
            comment.Comment);
    }

    private async Task<AppUser?> GetCurrentUserWithRolesAsync(CancellationToken cancellationToken)
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return await _context.AppUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
    }

    private static string ResolvePrimaryRole(AppUser user)
    {
        var roleNames = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        if (roleNames.Any(r => r.Equals(AppConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase)))
        {
            return AppConstants.Roles.Admin;
        }

        if (roleNames.Any(r => r.Equals(AppConstants.Roles.Gestor, StringComparison.OrdinalIgnoreCase)))
        {
            return AppConstants.Roles.Gestor;
        }

        return AppConstants.Roles.Ideador;
    }
}
