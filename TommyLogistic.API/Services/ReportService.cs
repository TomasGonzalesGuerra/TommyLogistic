using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TommyLogistic.API.Data;
using TommyLogistic.Shared.Enums;

namespace TommyLogistic.API.Services;

public interface IReportService
{
    Task<byte[]> GenerateOrdersExcelAsync(DateTime? desde, DateTime? hasta, string? status);
    Task<byte[]> GenerateCargasExcelAsync(DateTime? desde, DateTime? hasta);
    Task<byte[]> GenerateDriversExcelAsync();
    Task<byte[]> GenerateClientOrdersExcelAsync(string userId);
    Task<byte[]> GenerateOrdersPdfAsync(DateTime? desde, DateTime? hasta, string? status);
    Task<byte[]> GenerateCargasPdfAsync(DateTime? desde, DateTime? hasta);
    Task<byte[]> GenerateCargaDetailPdfAsync(int cargaId);
}

public class ReportService : IReportService
{
    private readonly LogisticDataContext _context;

    public ReportService(LogisticDataContext context)
    {
        _context = context;
        // QuestPDF community license (gratis hasta cierto volumen)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EXCEL — PEDIDOS
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateOrdersExcelAsync(DateTime? desde, DateTime? hasta, string? status)
    {
        var query = _context.Orders
            .Include(o => o.Driver).ThenInclude(d => d!.User)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(o => o.RegistrationDate >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.RegistrationDate <= hasta.Value.AddDays(1));
        if (!string.IsNullOrEmpty(status)) query = query.Where(o => nameof(o.OrderStatus) == status);

        var orders = await query.OrderByDescending(o => o.DeliveryDate).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Pedidos");

        // Header styling
        string[] headers = ["ID", "Estado", "Origen", "Destino", "Driver", "Items", "Fecha Creación"];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a56db");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        for (int i = 0; i < orders.Count; i++)
        {
            var o = orders[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = o.Id.ToString().Substring(0, 8).ToUpper();
            ws.Cell(row, 2).Value = TranslateStatus(nameof(o.OrderStatus));
            ws.Cell(row, 3).Value = o.RecipientDistrict;
            ws.Cell(row, 4).Value = o.Driver?.User?.FullName ?? "—";
            ws.Cell(row, 5).Value = o.Carga!.Orders!.Count;
            ws.Cell(row, 6).Value = nameof(o.DeliveryDate);

            // Zebra striping
            if (i % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");

            // Color por estado
            ws.Cell(row, 2).Style.Font.FontColor = GetStatusColor(nameof(o.OrderStatus));
            ws.Cell(row, 2).Style.Font.Bold = true;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        // Summary sheet
        var wsSummary = wb.Worksheets.Add("Resumen");
        wsSummary.Cell(1, 1).Value = "Resumen de Pedidos";
        wsSummary.Cell(1, 1).Style.Font.Bold = true;
        wsSummary.Cell(1, 1).Style.Font.FontSize = 14;

        var statusGroups = orders.GroupBy(o => o.OrderStatus).OrderByDescending(g => g.Count());
        wsSummary.Cell(3, 1).Value = "Estado";
        wsSummary.Cell(3, 2).Value = "Cantidad";
        wsSummary.Cell(3, 1).Style.Font.Bold = true;
        wsSummary.Cell(3, 2).Style.Font.Bold = true;

        int summaryRow = 4;
        foreach (var group in statusGroups)
        {
            wsSummary.Cell(summaryRow, 1).Value = TranslateStatus(nameof(group.Key));
            wsSummary.Cell(summaryRow, 2).Value = group.Count();
            summaryRow++;
        }
        wsSummary.Cell(summaryRow, 1).Value = "TOTAL";
        wsSummary.Cell(summaryRow, 2).Value = orders.Count;
        wsSummary.Cell(summaryRow, 1).Style.Font.Bold = true;
        wsSummary.Cell(summaryRow, 2).Style.Font.Bold = true;
        wsSummary.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EXCEL — CARGAS
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateCargasExcelAsync(DateTime? desde, DateTime? hasta)
    {
        var query = _context.Cargas
            .Include(c => c.Driver).ThenInclude(d => d.User)
            .Include(c => c.Supervisor)
            .Include(c => c.Orders)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(c => c.FechaCreacion >= desde.Value);
        if (hasta.HasValue) query = query.Where(c => c.FechaCreacion <= hasta.Value.AddDays(1));

        var cargas = await query.OrderByDescending(c => c.FechaCreacion).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Cargas");

        string[] headers = ["ID", "Estado", "Driver", "Supervisor", "Total Pedidos",
                            "Pedidos Entregados", "Efectividad", "Fecha Inicio",
                            "Fecha Conclusión", "Fecha Facturación"];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a56db");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int i = 0; i < cargas.Count; i++)
        {
            var c = cargas[i];
            var row = i + 2;
            int entregados = c.Orders!.Count(o => o.OrderStatus == OrderStatus.Delivered);
            double efectividad = c.Orders!.Count > 0
                ? Math.Round((double)entregados / c.Orders!.Count * 100, 1)
                : 0;

            ws.Cell(row, 1).Value = c.Id.ToString().Substring(0, 8).ToUpper();
            ws.Cell(row, 2).Value = nameof(c.Status);
            ws.Cell(row, 3).Value = c.Driver?.User?.FullName ?? "—";
            ws.Cell(row, 4).Value = c.Supervisor?.Email ?? "—";
            ws.Cell(row, 5).Value = c.Orders.Count;
            ws.Cell(row, 6).Value = entregados;
            ws.Cell(row, 7).Value = $"{efectividad}%";
            ws.Cell(row, 8).Value = c.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 9).Value = c.FechaConcluida?.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 10).Value = c.FechaFacturada?.ToString("dd/MM/yyyy HH:mm");

            if (i % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EXCEL — DRIVERS
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateDriversExcelAsync()
    {
        var drivers = await _context.Drivers
            .Include(d => d.User)
            .Include(d => d.Orders)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Drivers");

        string[] headers = ["Nombre", "Email", "Teléfono", "DNI", "Vehículo",
                            "Placa", "Total Pedidos", "Entregados", "Efectividad", "Estado"];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a56db");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (int i = 0; i < drivers.Count; i++)
        {
            var d = drivers[i];
            var row = i + 2;
            int entregados = d.Orders!.Count(o => o.OrderStatus == OrderStatus.Delivered);
            double efectividad = d.Orders!.Count > 0
                ? Math.Round((double)entregados / d.Orders!.Count * 100, 1)
                : 0;

            ws.Cell(row, 1).Value = d.User?.FullName ?? "—";
            ws.Cell(row, 2).Value = d.User?.Email ?? "—";
            ws.Cell(row, 3).Value = d.User?.PhoneNumber ?? "—";
            ws.Cell(row, 4).Value = d.User?.Document ?? "—";
            ws.Cell(row, 5).Value = "Driver";
            ws.Cell(row, 6).Value = d.Placa ?? "—";
            ws.Cell(row, 7).Value = d.Orders.Count;
            ws.Cell(row, 8).Value = entregados;
            ws.Cell(row, 9).Value = $"{efectividad}%";
            ws.Cell(row, 10).Value = d.Available ? "Activo" : "Inactivo";

            if (i % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EXCEL — MIS PEDIDOS (CLIENT)
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateClientOrdersExcelAsync(int userId)
    {
        var orders = await _context.Orders
            .Include(o => o.Driver).ThenInclude(d => d!.User)
            .Where(o => o.CompanyID == userId)
            .OrderByDescending(o => o.RegistrationDate)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mis Pedidos");

        string[] headers = ["ID", "Estado", "Destino", "Driver", "Items", "Fecha"];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a56db");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (int i = 0; i < orders.Count; i++)
        {
            var o = orders[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = o.Id.ToString().Substring(0, 8).ToUpper();
            ws.Cell(row, 2).Value = TranslateStatus(nameof(o.OrderStatus));
            ws.Cell(row, 3).Value = o.RecipientDistrict;
            ws.Cell(row, 4).Value = o.Driver?.User?.FullName ?? "—";
            ws.Cell(row, 5).Value = o.Carga!.Orders!.Count();
            ws.Cell(row, 6).Value = o.RegistrationDate.ToString("dd/MM/yyyy");
            if (i % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PDF — PEDIDOS
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateOrdersPdfAsync(DateTime? desde, DateTime? hasta, string? status)
    {
        var query = _context.Orders
            .Include(o => o.Driver).ThenInclude(d => d!.User)
            .Include(o => o.Company).ThenInclude(c => c!.User)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(o => o.RegistrationDate >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.RegistrationDate <= hasta.Value.AddDays(1));
        if (!string.IsNullOrEmpty(status)) query = query.Where(o => nameof(o.OrderStatus) == status);

        var orders = await query.OrderByDescending(o => o.RegistrationDate).ToListAsync();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(ComposeHeader("Reporte de Pedidos",
                    $"{orders.Count} pedidos · {DateTime.Now:dd/MM/yyyy HH:mm}"));

                page.Content().Element(e =>
                {
                    e.Column(col =>
                    {
                        // Summary boxes
                        col.Item().Row(row =>
                        {
                            var delivered = orders.Count(o => o.OrderStatus == OrderStatus.Delivered);
                            var inTransit = orders.Count(o => o.OrderStatus is OrderStatus.OnTheWay or OrderStatus.PickedUp);
                            var pending = orders.Count(o => o.OrderStatus is OrderStatus.Registered or OrderStatus.Assigned);

                            AddStatBox(row, "Total", orders.Count.ToString(), "#1a56db");
                            AddStatBox(row, "Entregados", delivered.ToString(), "#057a55");
                            AddStatBox(row, "En Tránsito", inTransit.ToString(), "#c27803");
                            AddStatBox(row, "Pendientes", pending.ToString(), "#9ca3af");
                        });

                        col.Item().Height(12);

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(70);   // ID
                                cols.RelativeColumn(2);    // Estado
                                cols.RelativeColumn(3);    // Destino
                                cols.RelativeColumn(3);    // Cliente
                                cols.RelativeColumn(3);    // Driver
                                cols.ConstantColumn(40);   // Items
                                cols.RelativeColumn(2);    // Fecha
                            });

                            // Header
                            table.Header(header =>
                            {
                                foreach (var h in new[] { "ID", "Estado", "Destino",
                                                           "Cliente", "Driver", "Items", "Fecha" })
                                {
                                    header.Cell().Background("#1a56db").Padding(6)
                                          .Text(h).FontColor("#ffffff").FontSize(8).Bold();
                                }
                            });

                            // Rows
                            foreach (var (o, idx) in orders.Select((o, i) => (o, i)))
                            {
                                var bg = idx % 2 == 0 ? "#ffffff" : "#f8fafc";
                                string[] cells =
                                [
                                    o.Id.ToString().Substring(0, 8).ToUpper(),
                                    TranslateStatus(nameof(o.OrderStatus)),
                                    o.RecipientDistrict,
                                    o.RecipientName ?? "—",
                                    o.Driver?.User?.FullName ?? "—",
                                    o.Quantity.ToString(),
                                    o.RegistrationDate.ToString("dd/MM/yy")
                                ];

                                foreach (var cellVal in cells)
                                    table.Cell().Background(bg).Padding(5)
                                         .Text(cellVal).FontSize(8);
                            }
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("TommyLogistic · Generado el ").FontSize(8).FontColor("#9ca3af");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor("#9ca3af");
                    text.Span(" · Página ").FontSize(8).FontColor("#9ca3af");
                    text.CurrentPageNumber().FontSize(8).FontColor("#9ca3af");
                    text.Span(" de ").FontSize(8).FontColor("#9ca3af");
                    text.TotalPages().FontSize(8).FontColor("#9ca3af");
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PDF — CARGAS
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateCargasPdfAsync(DateTime? desde, DateTime? hasta)
    {
        var query = _context.Cargas
            .Include(c => c.Driver).ThenInclude(d => d.User)
            .Include(c => c.Supervisor)
            .Include(c => c.Orders)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(c => c.Orders!.Any(o => o.RegistrationDate >= desde.Value));
        if (hasta.HasValue) query = query.Where(c => c.Orders!.Any(o => o.RegistrationDate <= hasta.Value.AddDays(1)));

        var cargas = await query.OrderByDescending(c => c.FechaCreacion).ToListAsync();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(ComposeHeader("Reporte de Cargas",
                    $"{cargas.Count} cargas · {DateTime.Now:dd/MM/yyyy HH:mm}"));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(70);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(3);
                        cols.ConstantColumn(45);
                        cols.ConstantColumn(45);
                        cols.ConstantColumn(55);
                        cols.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        foreach (var h in new[] { "ID", "Estado", "Driver",
                                                   "Pedidos", "Entregados", "Efectividad", "Fecha" })
                        {
                            header.Cell().Background("#1a56db").Padding(6)
                                  .Text(h).FontColor("#ffffff").FontSize(8).Bold();
                        }
                    });

                    foreach (var (c, idx) in cargas.Select((c, i) => (c, i)))
                    {
                        var bg = idx % 2 == 0 ? "#ffffff" : "#f8fafc";
                        int entregados = c.Orders!.Count(o => o.OrderStatus == OrderStatus.Delivered);
                        double ef = c.Orders!.Count > 0
                            ? Math.Round((double)entregados / c.Orders!.Count * 100, 1) : 0;

                        string[] cells =
                        [
                            c.Id.ToString().Substring(0, 8).ToUpper(),
                            nameof(c.Status),
                            c.Driver?.User?.FullName ?? "—",
                            c.Orders!.Count.ToString(),
                            entregados.ToString(),
                            $"{ef}%",
                            c.FechaCreacion.ToString("dd/MM/yy")
                        ];

                        foreach (var v in cells)
                            table.Cell().Background(bg).Padding(5).Text(v).FontSize(8);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("TommyLogistic · Página ").FontSize(8).FontColor("#9ca3af");
                    text.CurrentPageNumber().FontSize(8).FontColor("#9ca3af");
                    text.Span(" de ").FontSize(8).FontColor("#9ca3af");
                    text.TotalPages().FontSize(8).FontColor("#9ca3af");
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PDF — DETALLE CARGA
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateCargaDetailPdfAsync(int cargaId)
    {
        var carga = await _context.Cargas
            .Include(c => c.Driver).ThenInclude(d => d!.User)
            .Include(c => c.Supervisor)
            .Include(c => c.FacturadaPor)
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == cargaId) ?? throw new KeyNotFoundException("Carga no encontrada");
        
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader(
                    $"Detalle de Carga #{carga.Id.ToString().Substring(0, 8).ToUpper()}",
                    nameof(carga.Status)));

                page.Content().Column(col =>
                {
                    // Info general
                    col.Item().Background("#f8fafc").Padding(12).Column(info =>
                    {
                        info.Item().Text("Información General").FontSize(11).Bold();
                        info.Item().Height(8);
                        info.Item().Row(r =>
                        {
                            AddInfoPair(r, "Driver", carga.Driver?.User?.FullName ?? "—");
                            AddInfoPair(r, "Supervisor", carga.Supervisor?.Email ?? "—");
                        });
                        info.Item().Row(r =>
                        {
                            AddInfoPair(r, "Estado", nameof(carga.Status));
                            AddInfoPair(r, "Creada", carga.FechaCreacion.ToString("dd/MM/yyyy HH:mm"));
                        });
                        if (carga.FechaCreacion != DateTime.MinValue)
                        {
                            info.Item().Row(r =>
                            {
                                AddInfoPair(r, "Concluida", carga.FechaCreacion.ToString("dd/MM/yyyy HH:mm"));
                                AddInfoPair(r, "Facturada por", carga.FacturadaPor?.Email ?? "—");
                            });
                        }
                    });

                    col.Item().Height(16);

                    // Stats
                    col.Item().Row(row =>
                    {
                        int entregados = carga.Orders!.Count(o => o.OrderStatus == OrderStatus.Delivered);
                        int fallidos = carga.Orders!.Count(o => o.OrderStatus == OrderStatus.Failed);
                        double ef = carga.Orders!.Count > 0
                            ? Math.Round((double)entregados / carga.Orders.Count * 100, 1) : 0;

                        AddStatBox(row, "Total Pedidos", carga.Orders.Count.ToString(), "#1a56db");
                        AddStatBox(row, "Entregados", entregados.ToString(), "#057a55");
                        AddStatBox(row, "Fallidos", fallidos.ToString(), "#c81e1e");
                        AddStatBox(row, "Efectividad", $"{ef}%", "#c27803");
                    });

                    col.Item().Height(16);

                    // Orders table
                    col.Item().Text("Pedidos de la Carga").FontSize(11).Bold();
                    col.Item().Height(8);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(70);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(40);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "ID", "Estado", "Origen", "Destino", "Items", "Fecha" })
                                header.Cell().Background("#1a56db").Padding(6)
                                      .Text(h).FontColor("#ffffff").FontSize(8).Bold();
                        });

                        foreach (var (o, idx) in carga.Orders!.Select((o, i) => (o, i)))
                        {
                            var bg = idx % 2 == 0 ? "#ffffff" : "#f8fafc";
                            string[] cells =
                            [
                                o.Id.ToString().Substring(0, 8).ToUpper(),
                                TranslateStatus(nameof(o.OrderStatus)),
                                o.RecipientName,
                                o.RecipientDistrict,
                                o.Quantity.ToString(),
                                o.RegistrationDate.ToString("dd/MM/yy")
                            ];
                            foreach (var v in cells)
                                table.Cell().Background(bg).Padding(5).Text(v).FontSize(8);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("TommyLogistic · Generado el ").FontSize(8).FontColor("#9ca3af");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor("#9ca3af");
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    private static Action<IContainer> ComposeHeader(string title, string subtitle) =>
        container => container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("TommyLogistic").FontSize(18).Bold().FontColor("#1a56db");
                    c.Item().Text(title).FontSize(13).Bold();
                    c.Item().Text(subtitle).FontSize(9).FontColor("#6b7280");
                });
                row.ConstantItem(80).AlignRight()
                   .Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(9).FontColor("#9ca3af");
            });
            col.Item().Height(2).Background("#1a56db");
            col.Item().Height(10);
        });

    private static void AddStatBox(RowDescriptor row, string label, string value, string color)
    {
        row.RelativeItem().Padding(4).Border(1).BorderColor(color).Padding(8).Column(c =>
        {
            c.Item().Text(value).FontSize(20).Bold().FontColor(color).AlignCenter();
            c.Item().Text(label).FontSize(8).FontColor("#6b7280").AlignCenter();
        });
    }

    private static void AddInfoPair(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor("#6b7280");
            c.Item().Text(value).FontSize(10).Bold();
        });
    }

    private static string TranslateStatus(string status) => status switch
    {
        "Registered" => "Registrado",
        "Assigned" => "Asignado",
        "PickedUp" => "Recogido",
        "OnTheWay" => "En Camino",
        "Delivered" => "Entregado",
        "RecipientAbsent" => "Ausente",
        "Rescheduled" => "Reprogramado",
        "Returning" => "Retornando",
        "OnStorage" => "En Almacén",
        "Failed" => "Fallido",
        _ => status
    };

    private static XLColor GetStatusColor(string status) => status switch
    {
        "Delivered" => XLColor.FromHtml("#057a55"),
        "OnTheWay" or "PickedUp" => XLColor.FromHtml("#c27803"),
        "Failed" => XLColor.FromHtml("#c81e1e"),
        "Registered" => XLColor.FromHtml("#1a56db"),
        _ => XLColor.FromHtml("#374151")
    };

    public Task<byte[]> GenerateClientOrdersExcelAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<byte[]> GenerateCargaDetailPdfAsync(Guid cargaId)
    {
        throw new NotImplementedException();
    }
}