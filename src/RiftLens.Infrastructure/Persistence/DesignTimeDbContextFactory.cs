using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RiftLens.Infrastructure.Persistence;

/// <summary>Utilisé uniquement par <c>dotnet ef</c> pour générer les migrations.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RiftLensDbContext>
{
    public RiftLensDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RiftLensDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=RiftLens;Trusted_Connection=False;Encrypt=False")
            .Options;
        return new RiftLensDbContext(options);
    }
}
