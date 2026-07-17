using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TmsApi.Infrastructure.Persistence;

public class TmsDbContextFactory : IDesignTimeDbContextFactory<TmsDbContext>
{
    public TmsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TmsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=TmsDb;Username=postgres;Password=4704");

        return new TmsDbContext(optionsBuilder.Options);
    }
}
