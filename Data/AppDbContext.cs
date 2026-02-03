using Microsoft.EntityFrameworkCore;

namespace LanzaTuIdea.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Ping> Pings => Set<Ping>();
}
