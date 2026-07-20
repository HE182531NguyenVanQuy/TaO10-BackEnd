using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Services;

public sealed class PackageAccessService
{
    public const int PracticeMinimumDays = 90;
    public const int WordDownloadMinimumDays = 180;
    private readonly AppDbContext _dbContext;

    public PackageAccessService(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> HasActivePackageAsync(Guid userId, int minimumDurationDays)
    {
        var now = DateTime.UtcNow;
        return _dbContext.UserPackages.AsNoTracking().AnyAsync(userPackage =>
            userPackage.UserId == userId &&
            userPackage.Status.EntityType == "UserPackage" &&
            userPackage.Status.Code.ToUpper() == "ACTIVE" &&
            (!userPackage.EndDate.HasValue || userPackage.EndDate >= now) &&
            userPackage.Package != null &&
            (userPackage.Package.DurationTime ?? 0) >= minimumDurationDays);
    }
}
