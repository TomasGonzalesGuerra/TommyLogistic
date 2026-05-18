using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TommyLogistic.API.Services;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = nameof(UserEnum.Admin))]
public class ReportsController(IReportService reportService) : ControllerBase
{
    private readonly IReportService _reportService = reportService;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Yop";

    // ── Excel ──────────────────────────────────────────────────────────────────
    [HttpGet("orders/excel")]
    [Authorize(Roles = "Admin,Supervisor,Operator")]
    public async Task<IActionResult> DownloadOrdersExcel([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? status)
    {
        var bytes = await _reportService.GenerateOrdersExcelAsync(desde, hasta, status);
        var fileName = $"pedidos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("cargas/excel")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> DownloadCargasExcel([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var bytes = await _reportService.GenerateCargasExcelAsync(desde, hasta);
        var fileName = $"cargas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",fileName);
    }

    [HttpGet("drivers/excel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DownloadDriversExcel()
    {
        var bytes = await _reportService.GenerateDriversExcelAsync();
        return File(bytes,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"drivers_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("my-orders/excel")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> DownloadMyOrdersExcel()
    {
        var userID = CurrentUserId;
        var bytes = await _reportService.GenerateClientOrdersExcelAsync(userID);
        return File(bytes,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"mis_pedidos_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // ── PDF ────────────────────────────────────────────────────────────────────
    [HttpGet("orders/pdf")]
    [Authorize(Roles = "Admin,Supervisor,Operator")]
    public async Task<IActionResult> DownloadOrdersPdf([FromQuery] DateTime? desde,[FromQuery] DateTime? hasta,[FromQuery] string? status)
    {
        var bytes = await _reportService.GenerateOrdersPdfAsync(desde, hasta, status);
        var fileName = $"reporte_pedidos_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("cargas/pdf")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> DownloadCargasPdf([FromQuery] DateTime? desde,[FromQuery] DateTime? hasta)
    {
        var bytes = await _reportService.GenerateCargasPdfAsync(desde, hasta);
        return File(bytes, "application/pdf",$"reporte_cargas_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpGet("carga/{id}/pdf")]
    [Authorize(Roles = "Admin,Supervisor,Operator")]
    public async Task<IActionResult> DownloadCargaDetailPdf(Guid id)
    {
        var bytes = await _reportService.GenerateCargaDetailPdfAsync(id);
        return File(bytes, "application/pdf", $"carga_{id}.pdf");
    }
}