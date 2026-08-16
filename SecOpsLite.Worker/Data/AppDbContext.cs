using Microsoft.EntityFrameworkCore;
using SecOpsLite.Worker.Models;

namespace SecOpsLite.Worker.Data;

public class AppDbContext : DbContext{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){

    }
    public DbSet<AnomalyEvent> AnomalyEvents => Set<AnomalyEvent>();
}