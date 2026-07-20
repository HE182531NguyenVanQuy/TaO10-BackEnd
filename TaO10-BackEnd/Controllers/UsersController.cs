using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.DTOs.User;
using TaO10_BackEnd.Interfaces;
using ClosedXML.Excel;

namespace TaO10_BackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    private bool IsAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");
        return roleClaim != null && string.Equals(roleClaim.Value, "admin", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> GetPagedUsers([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
    {
        if (!IsAdmin()) return Forbid();

        try
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest(ApiResponse<UserPagedResponse>.ErrorResponse(
                    "Trang và số lượng bản ghi phải lớn hơn 0",
                    "INVALID_PAGINATION",
                    400));
            }

            var result = await _userService.GetPagedUsersAsync(search, pageNumber, pageSize);
            return Ok(ApiResponse<UserPagedResponse>.SuccessResponse(result, "Lấy danh sách người dùng thành công", 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPagedUsers");
            return StatusCode(500, ApiResponse<UserPagedResponse>.ErrorResponse("Lỗi hệ thống khi tải danh sách người dùng", "INTERNAL_ERROR", 500));
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportUsers([FromQuery] string? search = null)
    {
        if (!IsAdmin()) return Forbid();
        var result = await _userService.GetPagedUsersAsync(search, 1, int.MaxValue);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Người dùng");
        sheet.Range("A1:G1").Merge().Value = "DANH SÁCH NGƯỜI DÙNG";
        sheet.Range("A1:G1").Style.Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(XLColor.White);
        sheet.Range("A1:G1").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#006194"));
        sheet.Range("A1:G1").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Range("A2:G2").Merge().Value = $"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm} | Tổng số: {result.TotalCount:N0}";
        sheet.Range("A2:G2").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        var headers = new[] { "Họ và tên", "Email", "Số điện thoại", "Vai trò", "Trạng thái", "Ngày tham gia", "Mã người dùng" };
        for (var column = 1; column <= headers.Length; column++) sheet.Cell(4, column).Value = headers[column - 1];
        var row = 5;
        foreach (var user in result.Items)
        {
            sheet.Cell(row, 1).Value = user.FullName;
            sheet.Cell(row, 2).Value = user.Email;
            sheet.Cell(row, 3).Value = user.Phone ?? "";
            sheet.Cell(row, 4).Value = user.Role;
            sheet.Cell(row, 5).Value = user.StatusDisplayName;
            if (user.CreatedAt.HasValue) sheet.Cell(row, 6).Value = user.CreatedAt.Value;
            sheet.Cell(row, 7).Value = user.UserId.ToString();
            row++;
        }
        if (result.Items.Count > 0) sheet.Range(4, 1, row - 1, headers.Length).CreateTable("UsersTable");
        sheet.Column(6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        sheet.SheetView.FreezeRows(4);
        sheet.Columns().AdjustToContents();
        foreach (var column in sheet.ColumnsUsed()) if (column.Width > 42) column.Width = 42;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"danh-sach-users-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        if (!IsAdmin()) return Forbid();

        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(ApiResponse<UserDetailDto>.SuccessResponse(user, "Lấy chi tiết người dùng thành công", 200));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<UserDetailDto>.ErrorResponse(ex.Message, "USER_NOT_FOUND", 404));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserById");
            return StatusCode(500, ApiResponse<UserDetailDto>.ErrorResponse("Lỗi hệ thống khi tải chi tiết người dùng", "INTERNAL_ERROR", 500));
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        if (!IsAdmin()) return Forbid();

        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StatusCode))
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse("Trạng thái không hợp lệ", "INVALID_STATUS", 400));
            }

            var success = await _userService.UpdateUserStatusAsync(id, request);
            if (success)
            {
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Cập nhật trạng thái người dùng thành công", 200));
            }
            return BadRequest(ApiResponse<bool>.ErrorResponse("Không thể cập nhật trạng thái người dùng", "UPDATE_FAILED", 400));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message, "USER_NOT_FOUND", 404));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message, "INVALID_ARGUMENT", 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateUserStatus");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Lỗi hệ thống khi cập nhật trạng thái", "INTERNAL_ERROR", 500));
        }
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request)
    {
        if (!IsAdmin()) return Forbid();

        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse("Quyền không hợp lệ", "INVALID_ROLE", 400));
            }

            var success = await _userService.UpdateUserRoleAsync(id, request);
            if (success)
            {
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Cập nhật vai trò người dùng thành công", 200));
            }
            return BadRequest(ApiResponse<bool>.ErrorResponse("Không thể cập nhật vai trò người dùng", "UPDATE_FAILED", 400));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message, "USER_NOT_FOUND", 404));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message, "INVALID_ARGUMENT", 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateUserRole");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Lỗi hệ thống khi cập nhật vai trò", "INTERNAL_ERROR", 500));
        }
    }
}
