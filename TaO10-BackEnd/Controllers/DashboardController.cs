using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.DTOs.Dashboard;
using TaO10_BackEnd.Interfaces;
using ClosedXML.Excel;
using TaO10_BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace TaO10_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;
        private readonly AppDbContext _dbContext;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger, AppDbContext dbContext)
        {
            _dashboardService = dashboardService;
            _logger = logger;
            _dbContext = dbContext;
        }

        private bool IsAdmin()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");
            return roleClaim != null && string.Equals(roleClaim.Value, "admin", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetGeneralStats()
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var stats = await _dashboardService.GetGeneralStatsAsync();
                return Ok(ApiResponse<DashboardStatsDto>.SuccessResponse(stats, "Lấy thống kê thành công", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetGeneralStats");
                return StatusCode(500, ApiResponse<DashboardStatsDto>.ErrorResponse("Lỗi hệ thống khi tải thống kê", "INTERNAL_ERROR", 500));
            }
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetRecentTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                if (page < 1 || pageSize < 1)
                {
                    return BadRequest(ApiResponse<TransactionPagedResponse>.ErrorResponse(
                        "Trang và số lượng bản ghi phải lớn hơn 0",
                        "INVALID_PAGINATION",
                        400));
                }

                var result = await _dashboardService.GetRecentTransactionsAsync(page, pageSize);
                return Ok(ApiResponse<TransactionPagedResponse>.SuccessResponse(result, "Lấy danh sách giao dịch thành công", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentTransactions");
                return StatusCode(500, ApiResponse<TransactionPagedResponse>.ErrorResponse("Lỗi hệ thống khi tải danh sách giao dịch", "INTERNAL_ERROR", 500));
            }
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueAnalytics([FromQuery] string period = "monthly")
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var result = await _dashboardService.GetRevenueAnalyticsAsync(period);
                // Can't use ApiResponse<List<T>> easily if it expects class constraint, but ApiResponse generic definition doesn't restrict it. Assuming it works.
                return Ok(ApiResponse<List<RevenueDataDto>>.SuccessResponse(result, "Lấy dữ liệu doanh thu thành công", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRevenueAnalytics");
                return StatusCode(500, ApiResponse<List<RevenueDataDto>>.ErrorResponse("Lỗi hệ thống khi tải dữ liệu doanh thu", "INTERNAL_ERROR", 500));
            }
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackageDistribution()
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var result = await _dashboardService.GetPackageDistributionAsync();
                return Ok(ApiResponse<List<PackageStatDto>>.SuccessResponse(result, "Lấy phân bố gói dịch vụ thành công", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPackageDistribution");
                return StatusCode(500, ApiResponse<List<PackageStatDto>>.ErrorResponse("Lỗi hệ thống khi tải phân bố gói", "INTERNAL_ERROR", 500));
            }
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            if (!IsAdmin()) return Forbid();
            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            {
                return BadRequest(ApiResponse<AdminReportDto>.ErrorResponse(
                    "Ngày bắt đầu không được sau ngày kết thúc", "INVALID_DATE_RANGE", 400));
            }

            try
            {
                var result = await _dashboardService.GetReportsAsync(from, to);
                return Ok(ApiResponse<AdminReportDto>.SuccessResponse(result, "Lấy báo cáo thành công", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReports");
                return StatusCode(500, ApiResponse<AdminReportDto>.ErrorResponse(
                    "Lỗi hệ thống khi tải báo cáo", "INTERNAL_ERROR", 500));
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportDashboard()
        {
            if (!IsAdmin()) return Forbid();
            var payments = await _dbContext.Payments.AsNoTracking()
                .Include(payment => payment.User)
                .Include(payment => payment.Package)
                .Include(payment => payment.Status)
                .Include(payment => payment.UserPackages)
                    .ThenInclude(userPackage => userPackage.Package)
                .OrderByDescending(payment => payment.PaidAt ?? payment.CreatedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Giao dịch");
            ConfigureTitle(sheet, "BÁO CÁO ĐĂNG KÝ VÀ THANH TOÁN", null, DateTime.UtcNow, 15);
            var headers = new[]
            {
                "STT", "Transaction ID", "Họ và tên", "Username", "Email", "SĐT", "Tỉnh/TP",
                "Gói học", "Giá gói (VNĐ)", "Số tiền thanh toán", "Ngày đăng ký", "Ngày kích hoạt",
                "Ngày hết hạn", "Số ngày còn lại", "Ghi chú"
            };
            WriteHeaders(sheet, headers);
            var row = 5;
            var sequence = 1;
            var today = DateTime.UtcNow.Date;
            foreach (var payment in payments)
            {
                var userPackage = payment.UserPackages
                    .OrderByDescending(item => item.StartDate ?? item.CreatedAt)
                    .FirstOrDefault();
                var email = payment.User?.Email ?? "";
                var username = email.Contains('@') ? email[..email.IndexOf('@')] : email;
                var remainingDays = userPackage?.EndDate.HasValue == true
                    ? Math.Max(0, (userPackage.EndDate.Value.Date - today).Days)
                    : 0;

                sheet.Cell(row, 1).Value = sequence++;
                sheet.Cell(row, 2).Value = payment.TransactionCode ?? payment.PaymentId.ToString();
                sheet.Cell(row, 3).Value = payment.User?.FullName ?? "";
                sheet.Cell(row, 4).Value = username;
                sheet.Cell(row, 5).Value = email;
                sheet.Cell(row, 6).Value = payment.User?.Phone ?? "";
                sheet.Cell(row, 7).Value = payment.User?.Location ?? "";
                sheet.Cell(row, 8).Value = payment.Package?.Name ?? userPackage?.Package?.Name ?? "";
                sheet.Cell(row, 9).Value = payment.Package?.Price ?? userPackage?.Package?.Price ?? payment.ExpectedAmount;
                sheet.Cell(row, 10).Value = payment.ReceivedAmount ?? 0;
                if (payment.CreatedAt.HasValue) sheet.Cell(row, 11).Value = payment.CreatedAt.Value;
                if (userPackage?.StartDate.HasValue == true) sheet.Cell(row, 12).Value = userPackage.StartDate.Value;
                if (userPackage?.EndDate.HasValue == true) sheet.Cell(row, 13).Value = userPackage.EndDate.Value;
                sheet.Cell(row, 14).Value = remainingDays;
                sheet.Cell(row, 15).Value = $"{payment.Status?.DisplayName ?? payment.Status?.Code ?? ""} - {payment.PaymentMethod}".Trim(' ', '-');
                row++;
            }
            if (payments.Count == 0) sheet.Cell(row++, 1).Value = "Chưa có giao dịch";
            else sheet.Range(4, 1, row - 1, headers.Length).CreateTable("PaymentReportTable");
            sheet.Cell(2, 1).Value = $"Tổng giao dịch: {payments.Count:N0} | Tổng doanh thu: {payments.Sum(p => p.ReceivedAmount ?? 0):N0} VNĐ";
            sheet.Columns(9, 10).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
            sheet.Columns(11, 13).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            sheet.Columns(1, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Columns(14, 14).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            FinalizeWorksheet(sheet, headers.Length);

            var successfulCount = payments.Count(payment =>
            {
                var code = payment.Status?.Code?.ToLowerInvariant() ?? "";
                return code.Contains("success") || code.Contains("completed");
            });
            var failedCount = payments.Count(payment =>
            {
                var code = payment.Status?.Code?.ToLowerInvariant() ?? "";
                return code.Contains("fail") || code.Contains("cancel");
            });
            var pendingCount = Math.Max(0, payments.Count - successfulCount - failedCount);
            var expectedTotal = payments.Sum(payment => (decimal)payment.ExpectedAmount);
            var receivedTotal = payments.Sum(payment => (decimal)(payment.ReceivedAmount ?? 0));
            var totalTransactions = Math.Max(1, payments.Count);

            sheet.Range("Q1:S1").Merge().Value = "TỔNG KẾT";
            sheet.Range("Q1:S1").Style
                .Font.SetBold().Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#006194"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Cell("Q3").Value = "Chỉ số";
            sheet.Cell("R3").Value = "Giá trị";
            sheet.Cell("S3").Value = "Tỷ lệ";
            sheet.Range("Q3:S3").Style
                .Font.SetBold().Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1680B5"));

            var summaryRows = new (string Label, decimal Value, decimal? Rate)[]
            {
                ("Tổng giao dịch", payments.Count, 1m),
                ("Thành công", successfulCount, successfulCount / (decimal)totalTransactions),
                ("Chờ xử lý", pendingCount, pendingCount / (decimal)totalTransactions),
                ("Thất bại / hủy", failedCount, failedCount / (decimal)totalTransactions),
                ("Tổng tiền dự kiến", expectedTotal, null),
                ("Tiền thực nhận", receivedTotal, expectedTotal > 0 ? receivedTotal / expectedTotal : 0m)
            };
            for (var index = 0; index < summaryRows.Length; index++)
            {
                var summaryRow = 4 + index;
                sheet.Cell(summaryRow, 17).Value = summaryRows[index].Label;
                sheet.Cell(summaryRow, 18).Value = summaryRows[index].Value;
                if (summaryRows[index].Rate.HasValue) sheet.Cell(summaryRow, 19).Value = summaryRows[index].Rate.GetValueOrDefault();
            }
            sheet.Range("Q3:S9").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            sheet.Range("Q3:S9").Style.Border.SetInsideBorder(XLBorderStyleValues.Hair);
            sheet.Range("R4:R7").Style.NumberFormat.Format = "#,##0";
            sheet.Range("R8:R9").Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
            sheet.Range("S4:S9").Style.NumberFormat.Format = "0.0%";
            sheet.Column(17).Width = 22;
            sheet.Column(18).Width = 18;
            sheet.Column(19).Width = 12;

            var packageDistribution = payments
                .GroupBy(payment => payment.Package?.Name ?? "Chưa xác định")
                .Select(group => new
                {
                    Name = group.Key,
                    Count = group.Count(),
                    Percentage = group.Count() / (decimal)totalTransactions
                })
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Name)
                .ToList();
            sheet.Range("Q12:S12").Merge().Value = "TỶ TRỌNG CÁC GÓI";
            sheet.Range("Q12:S12").Style
                .Font.SetBold().Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#07855D"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Cell("Q14").Value = "Gói học";
            sheet.Cell("R14").Value = "Lượt đăng ký";
            sheet.Cell("S14").Value = "Tỷ lệ";
            sheet.Range("Q14:S14").Style
                .Font.SetBold().Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#10A879"));
            for (var index = 0; index < packageDistribution.Count; index++)
            {
                var packageRow = 15 + index;
                sheet.Cell(packageRow, 17).Value = packageDistribution[index].Name;
                sheet.Cell(packageRow, 18).Value = packageDistribution[index].Count;
                sheet.Cell(packageRow, 19).Value = packageDistribution[index].Percentage;
            }
            if (packageDistribution.Count > 0)
            {
                var packageLastRow = 14 + packageDistribution.Count;
                sheet.Range(14, 17, packageLastRow, 19).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                sheet.Range(14, 17, packageLastRow, 19).Style.Border.SetInsideBorder(XLBorderStyleValues.Hair);
                sheet.Range(15, 18, packageLastRow, 18).Style.NumberFormat.Format = "#,##0";
                sheet.Range(15, 19, packageLastRow, 19).Style.NumberFormat.Format = "0.0%";
            }
            return ExcelFile(workbook, $"bao-cao-dashboard-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        [HttpGet("reports/exams/export")]
        public async Task<IActionResult> ExportExamReport([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            if (!IsAdmin()) return Forbid();
            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date) return BadRequest("Khoảng ngày không hợp lệ.");

            var report = await _dashboardService.GetReportsAsync(from, to);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Báo cáo Exam");
            ConfigureTitle(sheet, "BÁO CÁO HIỆU SUẤT ĐỀ THI", from, to, 6);

            var headers = new[] { "Đề thi", "Lượt làm", "Hoàn thành", "Học viên", "Điểm trung bình", "Tỷ lệ đạt" };
            WriteHeaders(sheet, headers);
            var row = 5;
            foreach (var exam in report.Exams)
            {
                sheet.Cell(row, 1).Value = exam.Title;
                sheet.Cell(row, 2).Value = exam.Attempts;
                sheet.Cell(row, 3).Value = exam.CompletedAttempts;
                sheet.Cell(row, 4).Value = exam.UniqueStudents;
                sheet.Cell(row, 5).Value = exam.AverageScore;
                sheet.Cell(row, 6).Value = exam.PassRate / 100m;
                row++;
            }
            if (report.Exams.Count == 0) sheet.Cell(row++, 1).Value = "Không có dữ liệu trong khoảng thời gian đã chọn";
            else sheet.Range(4, 1, row - 1, headers.Length).CreateTable("ExamReportTable");

            sheet.Cell(2, 1).Value = $"Tổng lượt làm: {report.Summary.TotalAttempts:N0} | Điểm TB: {report.Summary.AverageScore:N2}/10 | Tỷ lệ đạt: {report.Summary.PassRate:N1}%";
            sheet.Column(5).Style.NumberFormat.Format = "0.00";
            sheet.Column(6).Style.NumberFormat.Format = "0.0%";
            FinalizeWorksheet(sheet, headers.Length);
            return ExcelFile(workbook, $"bao-cao-exam-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        [HttpGet("reports/packages/export")]
        public async Task<IActionResult> ExportPackageReport([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            if (!IsAdmin()) return Forbid();
            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date) return BadRequest("Khoảng ngày không hợp lệ.");

            var report = await _dashboardService.GetReportsAsync(from, to);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Báo cáo Package");
            ConfigureTitle(sheet, "BÁO CÁO DOANH THU PACKAGE", from, to, 6);

            var headers = new[] { "Package", "Giá bán", "Lượt mua", "Đang hoạt động", "Số đề", "Doanh thu" };
            WriteHeaders(sheet, headers);
            var row = 5;
            foreach (var package in report.Packages)
            {
                sheet.Cell(row, 1).Value = package.Name;
                sheet.Cell(row, 2).Value = package.Price;
                sheet.Cell(row, 3).Value = package.Purchases;
                sheet.Cell(row, 4).Value = package.ActiveSubscribers;
                sheet.Cell(row, 5).Value = package.ExamCount;
                sheet.Cell(row, 6).Value = package.Revenue;
                row++;
            }
            if (report.Packages.Count == 0) sheet.Cell(row++, 1).Value = "Không có dữ liệu trong khoảng thời gian đã chọn";
            else sheet.Range(4, 1, row - 1, headers.Length).CreateTable("PackageReportTable");

            sheet.Cell(2, 1).Value = $"Tổng lượt mua: {report.Summary.PackagesSold:N0} | Đang hoạt động: {report.Summary.ActiveSubscribers:N0} | Tổng doanh thu: {report.Summary.PackageRevenue:N0} VNĐ";
            sheet.Column(2).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
            sheet.Column(6).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
            FinalizeWorksheet(sheet, headers.Length);
            return ExcelFile(workbook, $"bao-cao-package-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        private static void ConfigureTitle(IXLWorksheet sheet, string title, DateTime? from, DateTime? to, int columns)
        {
            sheet.Range(1, 1, 1, columns).Merge().Value = title;
            sheet.Range(1, 1, 1, columns).Style
                .Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#006194"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Row(1).Height = 32;
            sheet.Range(2, 1, 2, columns).Merge();
            sheet.Range(3, 1, 3, columns).Merge().Value = $"Kỳ báo cáo: {from?.ToString("dd/MM/yyyy") ?? "Tất cả"} - {to?.ToString("dd/MM/yyyy") ?? "Hiện tại"}";
            sheet.Range(2, 1, 3, columns).Style.Font.SetFontColor(XLColor.FromHtml("#526575"));
            sheet.Range(2, 1, 3, columns).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        private static void WriteHeaders(IXLWorksheet sheet, IReadOnlyList<string> headers)
        {
            for (var column = 1; column <= headers.Count; column++) sheet.Cell(4, column).Value = headers[column - 1];
            sheet.Range(4, 1, 4, headers.Count).Style
                .Font.SetBold().Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1680B5"));
        }

        private static void FinalizeWorksheet(IXLWorksheet sheet, int columns)
        {
            sheet.SheetView.FreezeRows(4);
            sheet.RangeUsed()?.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            sheet.Columns(1, columns).AdjustToContents();
            for (var column = 1; column <= columns; column++)
            {
                if (sheet.Column(column).Width > 42) sheet.Column(column).Width = 42;
                if (sheet.Column(column).Width < 12) sheet.Column(column).Width = 12;
            }
            sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 32);
            sheet.RangeUsed()?.Style.Border.SetBottomBorder(XLBorderStyleValues.Hair);
            sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            sheet.PageSetup.FitToPages(1, 0);
        }

        private FileContentResult ExcelFile(XLWorkbook workbook, string fileName)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
