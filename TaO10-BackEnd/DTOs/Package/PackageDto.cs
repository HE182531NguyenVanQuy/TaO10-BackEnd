using System;
using System.Collections.Generic;
using TaO10_BackEnd.DTOs.Exams;

namespace TaO10_BackEnd.DTOs.Package
{
 public class PackageResponse
 {
 public Guid PackageId { get; set; }
 public string Name { get; set; } = null!;
 public string? Description { get; set; }
 public int Price { get; set; }
 public int? DurationTime { get; set; }
 public int ExamsCount { get; set; }
 public string Status { get; set; } = null!;
 public DateTime? CreatedAt { get; set; }
 }

 public class PackageDetailResponse
 {
 public Guid PackageId { get; set; }
 public string Name { get; set; } = null!;
 public string? Description { get; set; }
 public int Price { get; set; }
 public int? DurationTime { get; set; }
 public string Status { get; set; } = null!;
 public List<ExamDto> Exams { get; set; } = new();
 }

 public class CreatePackageRequest
 {
 public string Name { get; set; } = null!;
 public string? Description { get; set; }
 public int Price { get; set; }
 public int? DurationTime { get; set; }
 }

 public class UpdatePackageRequest
 {
 public string Name { get; set; } = null!;
 public string? Description { get; set; }
 public int Price { get; set; }
 public int? DurationTime { get; set; }
 public string Status { get; set; } = null!;
 }

 public class UserPackageResponse
 {
 public Guid UserPackageId { get; set; }
 public Guid PackageId { get; set; }
 public string PackageName { get; set; } = null!;
 public DateTime StartDate { get; set; }
 public DateTime? EndDate { get; set; }
 public string Status { get; set; } = null!;
 public bool IsActive { get; set; }
 public int ExamsCount { get; set; }
 public int DurationTime { get; set; }
 }
}
