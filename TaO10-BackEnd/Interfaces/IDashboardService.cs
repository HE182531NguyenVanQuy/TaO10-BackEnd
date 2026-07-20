using System.Collections.Generic;
using System.Threading.Tasks;
using TaO10_BackEnd.DTOs.Dashboard;

namespace TaO10_BackEnd.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetGeneralStatsAsync();
        Task<TransactionPagedResponse> GetRecentTransactionsAsync(int pageNumber, int pageSize);
        Task<List<RevenueDataDto>> GetRevenueAnalyticsAsync(string period);
        Task<List<PackageStatDto>> GetPackageDistributionAsync();
        Task<AdminReportDto> GetReportsAsync(DateTime? from, DateTime? to);
    }
}
