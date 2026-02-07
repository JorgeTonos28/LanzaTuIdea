namespace LanzaTuIdea.Api.Models;

public class IdeaComment
{
    public int Id { get; set; }
    public int IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;
    public DateTime CommentedAt { get; set; }
    public int CommentedByUserId { get; set; }
    public AppUser CommentedByUser { get; set; } = null!;
    public string CommentedByRole { get; set; } = "";
    public string CommentedByName { get; set; } = "";
    public string Comment { get; set; } = "";
}
