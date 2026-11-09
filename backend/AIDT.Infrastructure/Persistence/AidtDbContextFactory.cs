using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AIDT.Infrastructure.Persistence;

public sealed class AidtDbContextFactory : IDesignTimeDbContextFactory<AidtDbContext>
{
    public AidtDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AidtDbContext>()
            .UseSqlServer(
                @"Server=np:\\.\pipe\sql\query;Database=AIDT;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5",
                sqlServer => sqlServer.MigrationsAssembly(typeof(AidtDbContext).Assembly.FullName))
            .Options;
        return new AidtDbContext(options);
    }
}
