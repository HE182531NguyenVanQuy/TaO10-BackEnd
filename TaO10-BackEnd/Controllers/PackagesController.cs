using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.DTOs.Exams;
using TaO10_BackEnd.DTOs.Package;
using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PackagesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Packages — Returns all active packages for the pricing page
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PackageResponse>>> GetActivePackages()
        {
            var activeStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.EntityType == "Package" && s.Code == "ACTIVE");

            if (activeStatus == null) return NotFound("Status 'ACTIVE' for Package not configured.");

            var packages = await _context.Packages
                .Include(p => p.PackageExams)
                .Where(p => p.StatusId == activeStatus.StatusId)
                .OrderBy(p => p.Price)
                .Select(p => new PackageResponse
                {
                    PackageId = p.PackageId,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    DurationTime = p.DurationTime,
                    ExamsCount = p.PackageExams.Count,
                    Status = activeStatus.DisplayName ?? "Active",
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(packages);
        }

        // GET: api/Packages/{packageId}/exams — Returns exams in a purchased package
        [Authorize]
        [HttpGet("{packageId}/exams")]
        public async Task<ActionResult<PackageDetailResponse>> GetPackageExams(Guid packageId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var package = await _context.Packages
                .Include(p => p.PackageExams)
                    .ThenInclude(pe => pe.Exam)
                        .ThenInclude(e => e.Status)
                .FirstOrDefaultAsync(p => p.PackageId == packageId);

            if (package == null) return NotFound("Gói học tập không tồn tại.");

            // Check if user has purchased this specific package
            var activeUserPkgStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.EntityType == "UserPackage" && s.Code == "ACTIVE");

            if (activeUserPkgStatus == null)
                return BadRequest("Status 'ACTIVE' for UserPackage not configured.");

            var hasPurchased = await _context.UserPackages
                .AnyAsync(up => up.UserId == userId
                    && up.PackageId == packageId
                    && up.StatusId == activeUserPkgStatus.StatusId
                    && (up.EndDate == null || up.EndDate >= DateTime.UtcNow));

            if (!hasPurchased)
            {
                return StatusCode(403, new { message = "Bạn chưa mua gói này. Vui lòng thanh toán để truy cập đề thi." });
            }

            // Load user progress for exams
            var examIds = package.PackageExams.Select(pe => pe.ExamId).ToList();
            var userProgress = await _context.UserProgresses
                .Where(up => up.UserId == userId && up.ItemType == "EXAM" && examIds.Contains(up.ItemId ?? Guid.Empty))
                .ToDictionaryAsync(up => up.ItemId ?? Guid.Empty, up => up.ProgressPercentage ?? 0);

            var exams = package.PackageExams
                .Where(pe => pe.Exam != null && pe.Exam.Status?.Code == "ACTIVE")
                .Select(pe => new ExamDto
                {
                    ExamId = pe.Exam.ExamId,
                    Title = pe.Exam.Title,
                    Description = pe.Exam.Description,
                    QuestionsCount = pe.Exam.QuestionsCount,
                    DurationTime = pe.Exam.DurationTime,
                    Level = pe.Exam.Level,
                    Year = pe.Exam.Year,
                    ExamType = pe.Exam.ExamType,
                    ViewsCount = pe.Exam.ViewsCount ?? 0,
                    AttemptsCount = pe.Exam.AttemptsCount ?? 0,
                    StatusCode = pe.Exam.Status?.DisplayName ?? "Active",
                    IsPremium = true,
                    ProgressPercentage = userProgress.ContainsKey(pe.Exam.ExamId) ? userProgress[pe.Exam.ExamId] : 0,
                    CreatedAt = pe.Exam.CreatedAt
                })
                .OrderBy(e => e.Title)
                .ToList();

            return Ok(new PackageDetailResponse
            {
                PackageId = package.PackageId,
                Name = package.Name,
                Description = package.Description,
                Price = package.Price,
                DurationTime = package.DurationTime,
                Status = "Active",
                Exams = exams
            });
        }

        // GET: api/Packages/my-packages — Returns all active packages for a user
        [Authorize]
        [HttpGet("my-packages")]
        public async Task<ActionResult<IEnumerable<UserPackageResponse>>> GetMyPackages()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var activeStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.EntityType == "UserPackage" && s.Code == "ACTIVE");

            if (activeStatus == null) return NotFound("Status 'ACTIVE' for UserPackage not configured.");

            var userPackages = await _context.UserPackages
                .Include(up => up.Package)
                    .ThenInclude(p => p.PackageExams)
                .Include(up => up.Status)
                .Where(up => up.UserId == userId
                    && up.StatusId == activeStatus.StatusId
                    && (up.EndDate == null || up.EndDate >= DateTime.UtcNow))
                .OrderByDescending(up => up.CreatedAt)
                .Select(up => new UserPackageResponse
                {
                    UserPackageId = up.UserPackageId,
                    PackageId = up.PackageId ?? Guid.Empty,
                    PackageName = up.Package != null ? up.Package.Name : "Unknown",
                    StartDate = up.StartDate ?? DateTime.UtcNow,
                    EndDate = up.EndDate,
                    Status = up.Status.DisplayName ?? "Active",
                    IsActive = true,
                    ExamsCount = up.Package != null ? up.Package.PackageExams.Count : 0
                    ,DurationTime = up.Package != null ? up.Package.DurationTime ?? 0 : 0
                })
                .ToListAsync();

            return Ok(userPackages);
        }

        // GET: api/Packages/my-package — Returns the best active package for the user
        [Authorize]
        [HttpGet("my-package")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<UserPackageResponse>> GetMyPackage()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var activeStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.EntityType == "UserPackage" && s.Code == "ACTIVE");

            if (activeStatus == null) return NotFound("Status 'ACTIVE' for UserPackage not configured.");

            // Get active package that is not expired, prefer highest-priced (best) package
            var userPackage = await _context.UserPackages
                .Include(up => up.Package)
                    .ThenInclude(p => p.PackageExams)
                .Include(up => up.Status)
                .Where(up => up.UserId == userId && up.StatusId == activeStatus.StatusId && (up.EndDate == null || up.EndDate >= DateTime.UtcNow))
                .OrderByDescending(up => up.Package!.Price)
                .ThenByDescending(up => up.EndDate)
                .FirstOrDefaultAsync();

            if (userPackage != null)
            {
                return Ok(new UserPackageResponse
                {
                    UserPackageId = userPackage.UserPackageId,
                    PackageId = userPackage.PackageId ?? Guid.Empty,
                    PackageName = userPackage.Package?.Name ?? "Premium",
                    StartDate = userPackage.StartDate ?? DateTime.UtcNow,
                    EndDate = userPackage.EndDate,
                    Status = userPackage.Status.DisplayName ?? "Active",
                    IsActive = true,
                    ExamsCount = userPackage.Package?.PackageExams?.Count ?? 0
                    ,DurationTime = userPackage.Package?.DurationTime ?? 0
                });
            }

            // Fallback to no package — user hasn't purchased
            return Ok(new UserPackageResponse
            {
                UserPackageId = Guid.Empty,
                PackageId = Guid.Empty,
                PackageName = "Chưa có gói",
                StartDate = DateTime.UtcNow,
                EndDate = null,
                Status = "Inactive",
                IsActive = false,
                ExamsCount = 0
                ,DurationTime = 0
            });
        }

        // POST: api/Packages/activate-free-trial — Activates the free trial for a user
        [Authorize]
        [HttpPost("activate-free-trial")]
        public async Task<ActionResult<UserPackageResponse>> ActivateFreeTrial()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            // Find free package (price = 0)
            var freePackage = await _context.Packages
                .Include(p => p.PackageExams)
                .FirstOrDefaultAsync(p => p.Price == 0);

            if (freePackage == null)
                return BadRequest(new { message = "Gói dùng thử chưa được cấu hình trên hệ thống." });

            var activeUserPkgStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.EntityType == "UserPackage" && s.Code == "ACTIVE");

            if (activeUserPkgStatus == null)
                return BadRequest("Status 'ACTIVE' for UserPackage not configured.");

            // Check if user already used free trial
            var alreadyUsed = await _context.UserPackages
                .AnyAsync(up => up.UserId == userId && up.PackageId == freePackage.PackageId);

            if (alreadyUsed)
                return BadRequest(new { message = "Bạn đã sử dụng gói dùng thử miễn phí rồi." });

            // Create free trial UserPackage
            var duration = freePackage.DurationTime ?? 3;
            var start = DateTime.UtcNow;
            var end = start.AddDays(duration);

            var userPackage = new UserPackage
            {
                UserPackageId = Guid.NewGuid(),
                UserId = userId,
                PackageId = freePackage.PackageId,
                PaymentId = null,
                StartDate = start,
                EndDate = end,
                StatusId = activeUserPkgStatus.StatusId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserPackages.Add(userPackage);

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = "Kích hoạt dùng thử thành công!",
                Content = $"Gói dùng thử miễn phí đã được kích hoạt. Bạn có {duration} ngày để trải nghiệm. Hạn dùng đến {end:dd/MM/yyyy}.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return Ok(new UserPackageResponse
            {
                UserPackageId = userPackage.UserPackageId,
                PackageId = freePackage.PackageId,
                PackageName = freePackage.Name,
                StartDate = start,
                EndDate = end,
                Status = "Active",
                IsActive = true,
                ExamsCount = freePackage.PackageExams?.Count ?? 0
            });
        }

        // POST: api/Packages (Admin only)
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PackageResponse>> CreatePackage([FromBody] CreatePackageRequest request)
        {
            // Simple authorization check for admin role
            var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");
            if (roleClaim == null || roleClaim.Value != "admin")
                return Forbid("Chỉ quản trị viên mới có quyền này.");

            var activeStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.EntityType == "Package" && s.Code == "ACTIVE");

            if (activeStatus == null) return BadRequest("Status 'ACTIVE' for Package not configured.");

            var package = new Package
            {
                PackageId = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                DurationTime = request.DurationTime,
                StatusId = activeStatus.StatusId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetActivePackages), new { id = package.PackageId }, new PackageResponse
            {
                PackageId = package.PackageId,
                Name = package.Name,
                Description = package.Description,
                Price = package.Price,
                DurationTime = package.DurationTime,
                ExamsCount = 0,
                Status = "Active",
                CreatedAt = package.CreatedAt
            });
        }

        // PUT: api/Packages/{id} (Admin only)
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePackage(Guid id, [FromBody] UpdatePackageRequest request)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");
            if (roleClaim == null || roleClaim.Value != "admin")
                return Forbid();

            var package = await _context.Packages.FindAsync(id);
            if (package == null) return NotFound("Gói học tập không tồn tại.");

            var status = await _context.Statuses
                .FirstOrDefaultAsync(s => s.EntityType == "Package" && s.Code == request.Status);

            if (status == null) return BadRequest("Trạng thái gói học không hợp lệ.");

            package.Name = request.Name;
            package.Description = request.Description;
            package.Price = request.Price;
            package.DurationTime = request.DurationTime;
            package.StatusId = status.StatusId;
            package.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Packages/{id} (Admin only)
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePackage(Guid id)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");
            if (roleClaim == null || roleClaim.Value != "admin")
                return Forbid();

            var package = await _context.Packages.FindAsync(id);
            if (package == null) return NotFound("Gói học tập không tồn tại.");

            var inactiveStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.EntityType == "Package" && s.Code == "INACTIVE");

            if (inactiveStatus != null)
            {
                package.StatusId = inactiveStatus.StatusId;
                package.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Packages.Remove(package);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
