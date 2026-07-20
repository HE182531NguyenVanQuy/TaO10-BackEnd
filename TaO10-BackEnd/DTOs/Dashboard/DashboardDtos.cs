using System;
using System.Collections.Generic;
using TaO10_BackEnd.Common;

namespace TaO10_BackEnd.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public int TotalCustomers { get; set; }
        public int TotalPackages { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveExams { get; set; }
        public int TotalAttempts { get; set; }
    }

    public class TransactionDto
    {
        public string Id { get; set; } = null!;
        public string User { get; set; } = null!;
        public string? UserEmail { get; set; }
        public string Initials { get; set; } = null!;
        public string AvatarBg { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string Amount { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public class RevenueDataDto
    {
        public string Month { get; set; } = null!;
        public decimal Value { get; set; }
    }

    public class PackageStatDto
    {
        public string Name { get; set; } = string.Empty;
        public int Users { get; set; }
        public decimal Price { get; set; }
        public decimal Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class TransactionPagedResponse
    {
        public List<TransactionDto> Items { get; set; } = new List<TransactionDto>();
        public int TotalCount { get; set; }
    }

    public class AdminReportDto
    {
        public ReportSummaryDto Summary { get; set; } = new();
        public List<ExamReportDto> Exams { get; set; } = new();
        public List<PackageReportDto> Packages { get; set; } = new();
    }

    public class ReportSummaryDto
    {
        public int TotalExams { get; set; }
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal PassRate { get; set; }
        public int TotalPackages { get; set; }
        public int PackagesSold { get; set; }
        public decimal PackageRevenue { get; set; }
        public int ActiveSubscribers { get; set; }
    }

    public class ExamReportDto
    {
        public Guid ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public int CompletedAttempts { get; set; }
        public int UniqueStudents { get; set; }
        public decimal AverageScore { get; set; }
        public decimal PassRate { get; set; }
    }

    public class PackageReportDto
    {
        public Guid PackageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Purchases { get; set; }
        public int ActiveSubscribers { get; set; }
        public decimal Revenue { get; set; }
        public int ExamCount { get; set; }
    }
}
