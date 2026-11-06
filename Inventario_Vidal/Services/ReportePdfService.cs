// Services/ReportePdfService.cs
using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Inventario_Vidal.Services
{
    public class ReportePdfService : IReportePdfService
    {
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReportePdfService> _logger;

        private static class PdfColors
        {
            public const string Header = "#0a1628";
            public const string HeaderText = "#FFFFFF";
            public const string SubHeader = "#8a94a6";
            public const string TableHeader = "#f4f6fb";
            public const string TableHeaderText = "#0a1628";
            public const string Success = "#16a34a";
            public const string Warning = "#d97706";
            public const string Danger = "#b71c1c";
            public const string Info = "#0d47a1";
            public const string Border = "#e8edf5";
            public const string AlternateRow = "#f9fafc";
            public const string Text = "#0a1628";
            public const string TextMuted = "#5a6478";
            public const string PrimaryLight = "#42a5f5";
        }

        public ReportePdfService(
            FabricacionesVidalEirlBdContext context,
            IWebHostEnvironment env,
            ILogger<ReportePdfService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private async Task<string?> ObtenerLogoBase64Async()
        {
            try
            {
                var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
                if (config == null || string.IsNullOrEmpty(config.LogoUrl)) return null;

                var fullPath = Path.Combine(_env.WebRootPath, config.LogoUrl.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    var bytes = await File.ReadAllBytesAsync(fullPath);
                    return Convert.ToBase64String(bytes);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al cargar el logo");
                return null;
            }
        }

        private async Task<(string RazonSocial, string Ruc, string Direccion, string Telefono, string Email)> ObtenerConfiguracionEmpresaAsync()
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();
            return (
                RazonSocial: config?.NombreEmpresa ?? "Fabricaciones de Estructuras Metalicas Vidal E.I.R.L.",
                Ruc: config?.Ruc ?? "20612345678",
                Direccion: config?.Direccion ?? "Av. Principal 123, Lima",
                Telefono: config?.Telefono ?? "999-999-999",
                Email: config?.CorreoElectronico ?? "info@vidal.com"
            );
        }

        private void ConfigurarPagina(PageDescriptor page, bool horizontal = true)
        {
            page.Size(horizontal ? PageSizes.A4.Landscape() : PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Calibri));
        }

        private void AgregarEncabezado(
            IContainer container,
            string titulo,
            string subtitulo,
            string razonSocial,
            string? ruc,
            string? direccion,
            string? telefono,
            string? email,
            string? logoBase64)
        {
            container
                .Background(PdfColors.Header)
                .Padding(15)
                .Row(row =>
                {
                    if (!string.IsNullOrEmpty(logoBase64))
                    {
                        row.ConstantItem(65)
                            .PaddingRight(15)
                            .AlignCenter()
                            .Column(logoCol =>
                            {
                                logoCol.Item()
                                    .Width(60)
                                    .Height(60)
                                    .Image(Convert.FromBase64String(logoBase64))
                                    .FitArea();
                            });
                    }

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(razonSocial).FontSize(13).FontColor(PdfColors.HeaderText).Bold();

                        if (!string.IsNullOrEmpty(ruc))
                            col.Item().PaddingTop(2).Text($"RUC: {ruc}").FontSize(8).FontColor(PdfColors.SubHeader);

                        if (!string.IsNullOrEmpty(direccion))
                            col.Item().PaddingTop(1).Text(direccion).FontSize(8).FontColor(PdfColors.SubHeader);

                        if (!string.IsNullOrEmpty(telefono) || !string.IsNullOrEmpty(email))
                        {
                            var contacto = "";
                            if (!string.IsNullOrEmpty(telefono)) contacto += $"Tel: {telefono}";
                            if (!string.IsNullOrEmpty(email)) contacto += (string.IsNullOrEmpty(contacto) ? "" : " | ") + $"Email: {email}";
                            col.Item().PaddingTop(1).Text(contacto).FontSize(7).FontColor(PdfColors.SubHeader);
                        }

                        col.Item().PaddingTop(5).Text(titulo).FontSize(16).FontColor(PdfColors.HeaderText).Bold();
                        col.Item().PaddingTop(2).Text(subtitulo).FontSize(8).FontColor(PdfColors.SubHeader);
                    });

                    row.ConstantItem(120)
                        .AlignRight()
                        .PaddingTop(5)
                        .Column(col =>
                        {
                            col.Item().Text("GENERADO").FontSize(7).Bold().FontColor(PdfColors.SubHeader).AlignRight();
                            col.Item().PaddingTop(2).Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor(PdfColors.HeaderText).AlignRight();
                        });
                });
        }

        private void AgregarPiePagina(IContainer container, string razonSocial)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span($"{razonSocial}  •  ").FontSize(7).FontColor(PdfColors.SubHeader);
                text.Span("Sistema de Gestión de Inventario").FontSize(7).FontColor(PdfColors.SubHeader);
                text.Span("  •  Página ").FontSize(7).FontColor(PdfColors.SubHeader);
                text.CurrentPageNumber().FontSize(7).FontColor(PdfColors.PrimaryLight).Bold();
                text.Span(" de ").FontSize(7).FontColor(PdfColors.SubHeader);
                text.TotalPages().FontSize(7).FontColor(PdfColors.PrimaryLight).Bold();
            });
        }

        private void TarjetaResumen(IContainer container, string etiqueta, string valor, string colorValor)
        {
            container
                .Border(1).BorderColor(PdfColors.Border)
                .Background(Colors.White).Padding(10)
                .Column(col =>
                {
                    col.Item().Text(valor).FontSize(14).Bold().FontColor(colorValor);
                    col.Item().PaddingTop(2).Text(etiqueta.ToUpper()).FontSize(7).FontColor(PdfColors.SubHeader).Bold();
                });
        }

        private IContainer CeldaHeader(IContainer container)
        {
            return container
                .Background(PdfColors.TableHeader)
                .BorderBottom(1).BorderColor(PdfColors.Border)
                .PaddingVertical(6).PaddingHorizontal(8)
                .DefaultTextStyle(x => x.FontSize(8).Bold().FontColor(PdfColors.TableHeaderText));
        }

        private IContainer CeldaBody(IContainer container)
        {
            return container
                .BorderBottom(0.5f).BorderColor("#F1F5F9")
                .PaddingVertical(5).PaddingHorizontal(6)
                .DefaultTextStyle(x => x.FontSize(8).FontColor(PdfColors.Text));
        }

        // ================================================================
        // GENERAR PDF STOCK
        // ================================================================

        public async Task<byte[]> GenerarPdfStockAsync(IEnumerable<ReporteStockDto> reporte, bool soloAlertas = false)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = reporte.ToList();
            var totalRegistros = datos.Count;
            var alertas = datos.Count(r => r.AlertaStockBajo);
            var valorizadoTotal = datos.Sum(r => r.Valorizado ?? 0);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Stock",
                            $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm} | Total: {totalRegistros} materiales | Alertas: {alertas}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Materiales", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Stock Normal", (totalRegistros - alertas).ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Stock Bajo", alertas.ToString(), PdfColors.Danger));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Valorizado", $"S/ {valorizadoTotal:N2}", PdfColors.Info));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.5f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("#");
                                header.Cell().Element(CeldaHeader).Text("Código");
                                header.Cell().Element(CeldaHeader).Text("Material");
                                header.Cell().Element(CeldaHeader).Text("Categoría");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Unidad");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Stock Actual");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Stock Mínimo");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Estado");
                            });

                            if (datos.Any())
                            {
                                int counter = 1;
                                var itemsOrdenados = datos.OrderBy(m => m.AlertaStockBajo ? 0 : 1).ThenBy(m => m.Nombre);

                                foreach (var item in itemsOrdenados)
                                {
                                    var estadoColor = item.AlertaStockBajo ? PdfColors.Danger : PdfColors.Success;
                                    var estadoTexto = item.AlertaStockBajo ? "Bajo" : "Normal";

                                    table.Cell().Element(CeldaBody).Text(counter.ToString());
                                    table.Cell().Element(CeldaBody).Text(item.CodigoBarras);
                                    table.Cell().Element(CeldaBody).Text(item.Nombre);
                                    table.Cell().Element(CeldaBody).Text(item.Categoria);
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.UnidadConsumo);
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text(item.StockActual.ToString("N2"))
                                        .FontColor(item.AlertaStockBajo ? PdfColors.Danger : PdfColors.Success).Bold();
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.StockMinimo.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text(estadoTexto)
                                        .FontColor(estadoColor).Bold();
                                    counter++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(8)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay materiales registrados")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF PROYECTOS - CORREGIDO
        // ================================================================

        public async Task<byte[]> GenerarPdfProyectosAsync(IEnumerable<ReporteProyectoDto> reporte, DateTime? desde = null, DateTime? hasta = null)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = reporte.ToList();
            var totalRegistros = datos.Count;
            var totalUtilidad = datos.Sum(r => r.UtilidadReal ?? 0);  // ✅ Usar UtilidadReal

            var periodo = "";
            if (desde.HasValue && hasta.HasValue)
                periodo = $"Del {desde.Value:dd/MM/yyyy} al {hasta.Value:dd/MM/yyyy}";
            else if (desde.HasValue)
                periodo = $"Desde {desde.Value:dd/MM/yyyy}";
            else if (hasta.HasValue)
                periodo = $"Hasta {hasta.Value:dd/MM/yyyy}";
            else
                periodo = "Todos los proyectos";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Proyectos",
                            $"{periodo} | Total: {totalRegistros} proyectos | Utilidad: S/ {totalUtilidad:N2}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Proyectos", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Completados", datos.Count(p => p.Estado == "Entregado" || p.Estado == "Terminado").ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "En Proceso", datos.Count(p => p.Estado != "Entregado" && p.Estado != "Terminado" && p.Estado != "Cancelado").ToString(), PdfColors.Warning));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Utilidad Total", $"S/ {totalUtilidad:N2}", PdfColors.Info));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("Código");
                                header.Cell().Element(CeldaHeader).Text("Cliente");
                                header.Cell().Element(CeldaHeader).Text("Estado");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Fecha Inicio");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Fecha Entrega");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Precio Venta");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Costo Estimado");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Utilidad");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Moneda");
                            });

                            if (datos.Any())
                            {
                                foreach (var item in datos)
                                {
                                    var utilidadReal = item.UtilidadReal ?? 0;  // ✅ Usar UtilidadReal
                                    var utilidadColor = utilidadReal >= 0 ? PdfColors.Success : PdfColors.Danger;

                                    table.Cell().Element(CeldaBody).Text(item.Codigo);
                                    table.Cell().Element(CeldaBody).Text(item.Cliente);
                                    table.Cell().Element(CeldaBody).Text(item.Estado);
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.FechaInicio.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.FechaEntregaReal?.ToString("dd/MM/yyyy") ?? "-");
                                    table.Cell().Element(CeldaBody).AlignRight().Text(item.PrecioVenta?.ToString("N2") ?? "-");
                                    table.Cell().Element(CeldaBody).AlignRight().Text(item.CostoEstimado.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignRight()
                                        .Text(utilidadReal.ToString("N2"))  // ✅ Usar UtilidadReal
                                        .FontColor(utilidadColor).Bold();
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.Moneda);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(9)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay proyectos registrados")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF COMPRAS
        // ================================================================

        public async Task<byte[]> GenerarPdfComprasAsync(IEnumerable<ReporteCompraDto> reporte, DateTime? desde = null, DateTime? hasta = null, int? idProveedor = null)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = reporte.ToList();
            var totalRegistros = datos.Count;
            var totalGastado = datos.Sum(r => r.Subtotal ?? 0);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Compras",
                            $"Total: {totalRegistros} compras | Total Gastado: S/ {totalGastado:N2}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Compras", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Registradas", datos.Count(r => r.Estado == "Registrada").ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Anuladas", datos.Count(r => r.Estado == "Anulada").ToString(), PdfColors.Danger));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Gastado", $"S/ {totalGastado:N2}", PdfColors.Info));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("Comprobante");
                                header.Cell().Element(CeldaHeader).Text("Proveedor");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Fecha");
                                header.Cell().Element(CeldaHeader).Text("Material");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Cantidad");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Costo Unit.");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Subtotal");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Moneda");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Estado");
                            });

                            if (datos.Any())
                            {
                                foreach (var item in datos)
                                {
                                    var estadoColor = item.Estado == "Registrada" ? PdfColors.Success : PdfColors.Danger;
                                    table.Cell().Element(CeldaBody).Text(item.NumeroComprobante);
                                    table.Cell().Element(CeldaBody).Text(item.Proveedor);
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.FechaCompra.ToString("dd/MM/yyyy HH:mm"));
                                    table.Cell().Element(CeldaBody).Text(item.Material);
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.Cantidad.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignRight().Text(item.CostoUnitario.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignRight().Text(item.Subtotal?.ToString("N2") ?? "0.00").Bold();
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.Moneda);
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.Estado).FontColor(estadoColor).Bold();
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(9)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay compras registradas")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF KARDEX
        // ================================================================

        public async Task<byte[]> GenerarPdfKardexAsync(IEnumerable<ReporteKardexDto> reporte, int? idMaterial = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = reporte.ToList();
            var totalRegistros = datos.Count;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Kardex",
                            $"Total: {totalRegistros} movimientos",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Movimientos", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Entradas", datos.Count(r => r.Cantidad > 0).ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Salidas", datos.Count(r => r.Cantidad < 0).ToString(), PdfColors.Danger));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Materiales", datos.Select(r => r.Material).Distinct().Count().ToString(), PdfColors.Info));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.8f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Fecha");
                                header.Cell().Element(CeldaHeader).Text("Material");
                                header.Cell().Element(CeldaHeader).Text("Tipo Movimiento");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Cantidad");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Stock Ant.");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Stock Post.");
                                header.Cell().Element(CeldaHeader).Text("Registrado Por");
                                header.Cell().Element(CeldaHeader).Text("Observación");
                            });

                            if (datos.Any())
                            {
                                foreach (var item in datos)
                                {
                                    var cantidadColor = item.Cantidad > 0 ? PdfColors.Success : PdfColors.Danger;
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.FechaMovimiento.ToString("dd/MM/yyyy HH:mm"));
                                    table.Cell().Element(CeldaBody).Text(item.Material);
                                    table.Cell().Element(CeldaBody).Text(item.TipoMovimiento);
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text(item.Cantidad.ToString("N2"))
                                        .FontColor(cantidadColor).Bold();
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.StockAnterior?.ToString("N2") ?? "0.00");
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.StockPosterior?.ToString("N2") ?? "0.00");
                                    table.Cell().Element(CeldaBody).Text(item.RegistradoPor);
                                    table.Cell().Element(CeldaBody).Text(item.Observacion ?? "-");
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(8)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay movimientos de inventario")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF MAQUINARIA
        // ================================================================

        public async Task<byte[]> GenerarPdfMaquinariaAsync(IEnumerable<ReporteMaquinariaDto> reporte)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = reporte.ToList();
            var totalRegistros = datos.Count;
            var totalPrestamos = datos.Sum(r => r.TotalPrestamos);
            var totalMantenimientos = datos.Sum(r => r.TotalMantenimientos);
            var totalCostoMantenimiento = datos.Sum(r => r.CostoMantenimiento);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Maquinaria",
                            $"Total: {totalRegistros} maquinarias | Préstamos: {totalPrestamos} | Mantenimientos: {totalMantenimientos}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Maquinarias", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Disponibles", datos.Count(m => m.Estado == "Disponible").ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Prestadas", datos.Count(m => m.Estado == "Prestada").ToString(), PdfColors.Warning));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "En Mantenimiento", datos.Count(m => m.Estado == "Mantenimiento" || m.Estado == "Fuera de Servicio").ToString(), PdfColors.Danger));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.5f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.3f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("#");
                                header.Cell().Element(CeldaHeader).Text("Código");
                                header.Cell().Element(CeldaHeader).Text("Nombre");
                                header.Cell().Element(CeldaHeader).Text("Marca");
                                header.Cell().Element(CeldaHeader).Text("Modelo");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Estado");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Préstamos");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Mant.");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Costo Mant.");
                            });

                            if (datos.Any())
                            {
                                int counter = 1;
                                foreach (var item in datos)
                                {
                                    var estadoColor = item.Estado switch
                                    {
                                        "Disponible" => PdfColors.Success,
                                        "Prestada" => PdfColors.Warning,
                                        "Mantenimiento" => PdfColors.Info,
                                        "Fuera de Servicio" => PdfColors.Danger,
                                        _ => PdfColors.TextMuted
                                    };

                                    table.Cell().Element(CeldaBody).Text(counter.ToString());
                                    table.Cell().Element(CeldaBody).Text(item.Codigo);
                                    table.Cell().Element(CeldaBody).Text(item.Nombre);
                                    table.Cell().Element(CeldaBody).Text(item.Marca);
                                    table.Cell().Element(CeldaBody).Text(item.Modelo);
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text(item.Estado)
                                        .FontColor(estadoColor).Bold();
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.TotalPrestamos.ToString());
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.TotalMantenimientos.ToString());
                                    table.Cell().Element(CeldaBody).AlignRight()
                                        .Text(item.CostoMantenimiento.ToString("N2"));
                                    counter++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(9)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay maquinarias registradas")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF CLIENTES
        // ================================================================

        public async Task<byte[]> GenerarPdfClientesAsync(IEnumerable<ReporteClienteDto> reporte)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = reporte.ToList();
            var totalRegistros = datos.Count;
            var totalProyectos = datos.Sum(r => r.TotalProyectos);
            var totalInvertido = datos.Sum(r => r.TotalInvertido);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Clientes",
                            $"Total: {totalRegistros} clientes | Proyectos: {totalProyectos} | Invertido: S/ {totalInvertido:N2}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Clientes", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Empresas", datos.Count(r => r.TipoCliente == "Empresa").ToString(), PdfColors.Info));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Personas", datos.Count(r => r.TipoCliente == "Persona").ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Invertido", $"S/ {totalInvertido:N2}", PdfColors.Warning));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.5f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(2.2f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.3f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("#");
                                header.Cell().Element(CeldaHeader).Text("RUC/DNI");
                                header.Cell().Element(CeldaHeader).Text("Nombre / Razón Social");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Tipo");
                                header.Cell().Element(CeldaHeader).Text("Teléfono");
                                header.Cell().Element(CeldaHeader).Text("Email");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Proyectos");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Total Invertido");
                            });

                            if (datos.Any())
                            {
                                int counter = 1;
                                foreach (var item in datos.OrderByDescending(r => r.TotalInvertido))
                                {
                                    table.Cell().Element(CeldaBody).Text(counter.ToString());
                                    table.Cell().Element(CeldaBody).Text(item.NumeroDocumento);
                                    table.Cell().Element(CeldaBody).Text(item.NombreOrazonSocial);
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.TipoCliente);
                                    table.Cell().Element(CeldaBody).Text(item.Telefono ?? "-");
                                    table.Cell().Element(CeldaBody).Text(item.CorreoElectronico ?? "-");
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.TotalProyectos.ToString());
                                    table.Cell().Element(CeldaBody).AlignRight()
                                        .Text(item.TotalInvertido.ToString("N2")).Bold();
                                    counter++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(8)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay clientes registrados")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF CONTRATISTAS
        // ================================================================

        public async Task<byte[]> GenerarPdfContratistasAsync(IEnumerable<ReporteContratistaDto> reporte)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = reporte.ToList();
            var totalRegistros = datos.Count;
            var totalHoras = datos.Sum(r => r.TotalHoras);
            var totalPagado = datos.Sum(r => r.TotalPagado);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Contratistas",
                            $"Total: {totalRegistros} contratistas | Horas: {totalHoras} | Pagado: S/ {totalPagado:N2}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Contratistas", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Horas", totalHoras.ToString(), PdfColors.Info));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Especialidades", datos.Select(r => r.Especialidad).Distinct().Count().ToString(), PdfColors.Warning));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Pagado", $"S/ {totalPagado:N2}", PdfColors.Success));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.5f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.3f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("#");
                                header.Cell().Element(CeldaHeader).Text("DNI");
                                header.Cell().Element(CeldaHeader).Text("Nombre");
                                header.Cell().Element(CeldaHeader).Text("Especialidad");
                                header.Cell().Element(CeldaHeader).Text("Teléfono");
                                header.Cell().Element(CeldaHeader).Text("Email");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Horas");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Tarifa/Hora");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Total Pagado");
                            });

                            if (datos.Any())
                            {
                                int counter = 1;
                                foreach (var item in datos.OrderByDescending(r => r.TotalPagado))
                                {
                                    table.Cell().Element(CeldaBody).Text(counter.ToString());
                                    table.Cell().Element(CeldaBody).Text(item.NumeroDocumento);
                                    table.Cell().Element(CeldaBody).Text(item.NombreCompleto);
                                    table.Cell().Element(CeldaBody).Text(item.Especialidad ?? "-");
                                    table.Cell().Element(CeldaBody).Text(item.Telefono ?? "-");
                                    table.Cell().Element(CeldaBody).Text(item.CorreoElectronico ?? "-");
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.TotalHoras.ToString());
                                    table.Cell().Element(CeldaBody).AlignRight().Text($"S/ {item.TarifaHora:N2}");
                                    table.Cell().Element(CeldaBody).AlignRight()
                                        .Text($"S/ {item.TotalPagado:N2}").Bold();
                                    counter++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(9)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay contratistas registrados")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF UTILIDAD
        // ================================================================

        public async Task<byte[]> GenerarPdfUtilidadAsync(IEnumerable<ReporteUtilidadDto> reporte)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = reporte.ToList();
            var totalRegistros = datos.Count;
            var totalIngresos = datos.Sum(r => r.Ingresos);
            var totalCostos = datos.Sum(r => r.Costos);
            var totalUtilidad = datos.Sum(r => r.Utilidad);
            var margenPromedio = datos.Any() ? datos.Average(r => r.Margen) : 0;
            var proyectosGanancia = datos.Count(r => r.Utilidad >= 0);
            var proyectosPerdida = datos.Count(r => r.Utilidad < 0);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Utilidad por Proyecto",
                            $"Total: {totalRegistros} proyectos | Utilidad: S/ {totalUtilidad:N2} | Margen: {margenPromedio:N1}%",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Proyectos", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "En Ganancia", proyectosGanancia.ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "En Pérdida", proyectosPerdida.ToString(), PdfColors.Danger));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Margen Promedio", $"{margenPromedio:N1}%", PdfColors.Info));
                        });

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Ingresos", $"S/ {totalIngresos:N2}", PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Costos", $"S/ {totalCostos:N2}", PdfColors.Danger));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Utilidad Neta", $"S/ {totalUtilidad:N2}", totalUtilidad >= 0 ? PdfColors.Success : PdfColors.Danger));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Proyectos", $"{proyectosGanancia}/{proyectosPerdida}", PdfColors.Warning));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("Código");
                                header.Cell().Element(CeldaHeader).Text("Proyecto");
                                header.Cell().Element(CeldaHeader).Text("Cliente");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Fecha");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Ingresos");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Costos");
                                header.Cell().Element(CeldaHeader).AlignRight().Text("Utilidad");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Margen");
                            });

                            if (datos.Any())
                            {
                                foreach (var item in datos.OrderByDescending(r => r.Utilidad))
                                {
                                    var utilidadColor = item.Utilidad >= 0 ? PdfColors.Success : PdfColors.Danger;

                                    table.Cell().Element(CeldaBody).Text(item.Codigo);
                                    table.Cell().Element(CeldaBody).Text(item.Nombre);
                                    table.Cell().Element(CeldaBody).Text(item.Cliente);
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.FechaInicio.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(CeldaBody).AlignRight().Text($"S/ {item.Ingresos:N2}").FontColor(PdfColors.Success);
                                    table.Cell().Element(CeldaBody).AlignRight().Text($"S/ {item.Costos:N2}").FontColor(PdfColors.Danger);
                                    table.Cell().Element(CeldaBody).AlignRight()
                                        .Text($"S/ {item.Utilidad:N2}")
                                        .FontColor(utilidadColor).Bold();
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text($"{item.Margen:N1}%")
                                        .FontColor(item.Margen >= 10 ? PdfColors.Success : item.Margen >= 0 ? PdfColors.Warning : PdfColors.Danger).Bold();
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(8)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay proyectos registrados")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF EQUIPO VIDAL - PERSONAS
        // ================================================================

        public async Task<byte[]> GenerarPdfEquipoAsync(IEnumerable<AsignacionesPersonalListDto> personas)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = personas.ToList();
            var totalRegistros = datos.Count;
            var activos = datos.Count(p => p.Activo);
            var inactivos = totalRegistros - activos;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Asignaciones de personal - Personal",
                            $"Total: {totalRegistros} personas | Activos: {activos} | Inactivos: {inactivos}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Personas", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Activos", activos.ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Inactivos", inactivos.ToString(), PdfColors.Danger));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Con Proyecto", datos.Count(p => !string.IsNullOrEmpty(p.CodigoProyectoActivo)).ToString(), PdfColors.Info));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.5f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("#");
                                header.Cell().Element(CeldaHeader).Text("Tipo");
                                header.Cell().Element(CeldaHeader).Text("Nombre");
                                header.Cell().Element(CeldaHeader).Text("Documento");
                                header.Cell().Element(CeldaHeader).Text("Especialidad");
                                header.Cell().Element(CeldaHeader).Text("Teléfono");
                                header.Cell().Element(CeldaHeader).Text("Correo");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Estado");
                            });

                            if (datos.Any())
                            {
                                int counter = 1;
                                foreach (var item in datos.OrderBy(p => p.NombreCompleto))
                                {
                                    var estadoColor = item.Activo ? PdfColors.Success : PdfColors.Danger;
                                    var estadoTexto = item.Activo ? "Activo" : "Inactivo";

                                    table.Cell().Element(CeldaBody).Text(counter.ToString());
                                    table.Cell().Element(CeldaBody).Text(item.Tipo);
                                    table.Cell().Element(CeldaBody).Text(item.NombreCompleto);
                                    table.Cell().Element(CeldaBody).Text(item.NumeroDocumento);
                                    table.Cell().Element(CeldaBody).Text(item.Especialidad ?? "-");
                                    table.Cell().Element(CeldaBody).Text(item.Telefono ?? "-");
                                    table.Cell().Element(CeldaBody).Text(item.CorreoElectronico ?? "-");
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text(estadoTexto)
                                        .FontColor(estadoColor).Bold();
                                    counter++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(8)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay personas registradas")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF ASIGNACIONES ACTIVAS (MATERIALES)
        // ================================================================

        public async Task<byte[]> GenerarPdfAsignacionesActivasAsync(IEnumerable<AsignacionMaterialesMaterialDto> asignaciones)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = asignaciones.ToList();
            var totalRegistros = datos.Count;
            var totalEntregado = datos.Sum(a => a.CantidadEntregada);
            var totalUtilizado = datos.Sum(a => a.CantidadUtilizada);
            var totalPendiente = datos.Sum(a => a.Pendiente);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Asignaciones Activas - Materiales",
                            $"Total: {totalRegistros} asignaciones | Entregado: {totalEntregado} | Pendiente: {totalPendiente}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Asignaciones", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Cantidad Entregada", totalEntregado.ToString("N2"), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Cantidad Utilizada", totalUtilizado.ToString("N2"), PdfColors.Info));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Cantidad Pendiente", totalPendiente.ToString("N2"), PdfColors.Warning));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("#");
                                header.Cell().Element(CeldaHeader).Text("Tipo");
                                header.Cell().Element(CeldaHeader).Text("Persona");
                                header.Cell().Element(CeldaHeader).Text("Documento");
                                header.Cell().Element(CeldaHeader).Text("Material");
                                header.Cell().Element(CeldaHeader).Text("Proyecto");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Entregado");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Utilizado");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Devuelto");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Estado");
                            });

                            if (datos.Any())
                            {
                                int counter = 1;
                                foreach (var item in datos)
                                {
                                    var estadoColor = item.Estado switch
                                    {
                                        "Completado" => PdfColors.Success,
                                        "Pendiente" => PdfColors.Warning,
                                        "Parcial" => PdfColors.Info,
                                        _ => PdfColors.TextMuted
                                    };

                                    table.Cell().Element(CeldaBody).Text(counter.ToString());
                                    table.Cell().Element(CeldaBody).Text(item.Tipo);
                                    table.Cell().Element(CeldaBody).Text(item.NombreCompleto);
                                    table.Cell().Element(CeldaBody).Text(item.NumeroDocumento);
                                    table.Cell().Element(CeldaBody).Text(item.Material);
                                    table.Cell().Element(CeldaBody).Text(item.CodigoProyecto ?? "-");
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.CantidadEntregada.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.CantidadUtilizada.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.CantidadDevuelta.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text(item.Estado)
                                        .FontColor(estadoColor).Bold();
                                    counter++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(10)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay asignaciones activas")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        // ================================================================
        // GENERAR PDF ASIGNACIONES DE PERSONAL
        // ================================================================

        public async Task<byte[]> GenerarPdfAsignacionesPersonalAsync(IEnumerable<AsignacionPersonalProyectoListDto> asignaciones)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = asignaciones.ToList();
            var totalRegistros = datos.Count;
            var activos = datos.Count(a => a.Activo);
            var finalizados = totalRegistros - activos;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Reporte de Asignaciones de Personal",
                            $"Total: {totalRegistros} asignaciones | Activos: {activos} | Finalizados: {finalizados}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Asignaciones", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Activos", activos.ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Finalizados", finalizados.ToString(), PdfColors.Danger));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Proyectos", datos.Select(a => a.CodigoProyecto).Distinct().Count().ToString(), PdfColors.Info));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.5f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("#");
                                header.Cell().Element(CeldaHeader).Text("Tipo");
                                header.Cell().Element(CeldaHeader).Text("Persona");
                                header.Cell().Element(CeldaHeader).Text("Documento");
                                header.Cell().Element(CeldaHeader).Text("Proyecto");
                                header.Cell().Element(CeldaHeader).Text("Rol");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Fecha Inicio");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Fecha Fin");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Estado");
                            });

                            if (datos.Any())
                            {
                                int counter = 1;
                                foreach (var item in datos.OrderByDescending(a => a.Activo).ThenBy(a => a.NombreCompleto))
                                {
                                    var estadoColor = item.Activo ? PdfColors.Success : PdfColors.Danger;
                                    var estadoTexto = item.Activo ? "Activo" : "Finalizado";

                                    table.Cell().Element(CeldaBody).Text(counter.ToString());
                                    table.Cell().Element(CeldaBody).Text(item.Tipo);
                                    table.Cell().Element(CeldaBody).Text(item.NombreCompleto);
                                    table.Cell().Element(CeldaBody).Text(item.NumeroDocumento);
                                    table.Cell().Element(CeldaBody).Text(item.CodigoProyecto);
                                    table.Cell().Element(CeldaBody).Text(item.RolEnProyecto ?? "-");
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.FechaHoraInicio.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.FechaHoraFin?.ToString("dd/MM/yyyy") ?? "En curso");
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text(estadoTexto)
                                        .FontColor(estadoColor).Bold();
                                    counter++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(9)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay asignaciones de personal registradas")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }

        public async Task<byte[]> GenerarPdfDesglosePaquetesAsync(IEnumerable<PaqueteDesgloseMaterialDto> desglose)
        {
            var (razonSocial, ruc, direccion, telefono, email) = await ObtenerConfiguracionEmpresaAsync();
            var logoBase64 = await ObtenerLogoBase64Async();

            var datos = desglose.ToList();
            var totalRegistros = datos.Count;
            var totalSellados = datos.Sum(d => d.PaquetesSellados);
            var totalAbiertos = datos.Sum(d => d.PaquetesAbiertos);
            var totalUnidades = datos.Sum(d => d.TotalUnidadesEnPaquetes);
            var inconsistentes = datos.Count(d => !d.EstaConsistente);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page, true);

                    page.Header().Element(c =>
                        AgregarEncabezado(c, "Desglose de Paquetes por Material",
                            $"Total: {totalRegistros} materiales | Paquetes Sellados: {totalSellados} | Abiertos: {totalAbiertos} | Inconsistentes: {inconsistentes}",
                            razonSocial, ruc, direccion, telefono, email, logoBase64));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Total Materiales", totalRegistros.ToString(), PdfColors.Header));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Paquetes Sellados", totalSellados.ToString(), PdfColors.Success));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Paquetes Abiertos", totalAbiertos.ToString(), PdfColors.Warning));
                            row.RelativeItem().Element(c => TarjetaResumen(c, "Inconsistentes", inconsistentes.ToString(), PdfColors.Danger));
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CeldaHeader).Text("Código");
                                header.Cell().Element(CeldaHeader).Text("Material");
                                header.Cell().Element(CeldaHeader).Text("Presentación");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Sellados");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Unid. Sell.");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Abiertos");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Unid. Abiert.");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Total Paq.");
                                header.Cell().Element(CeldaHeader).AlignCenter().Text("Consistencia");
                            });

                            if (datos.Any())
                            {
                                foreach (var item in datos)
                                {
                                    var consistenciaColor = item.EstaConsistente ? PdfColors.Success : PdfColors.Danger;
                                    var consistenciaTexto = item.EstaConsistente ? "✅ OK" : "⚠️ Inc";

                                    table.Cell().Element(CeldaBody).Text(item.CodigoBarras);
                                    table.Cell().Element(CeldaBody).Text(item.Material);
                                    table.Cell().Element(CeldaBody).Text(item.Presentacion);
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.PaquetesSellados.ToString());
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.UnidadesEnSellados.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.PaquetesAbiertos.ToString());
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.UnidadesEnAbiertos.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignCenter().Text(item.TotalUnidadesEnPaquetes.ToString("N2"));
                                    table.Cell().Element(CeldaBody).AlignCenter()
                                        .Text(consistenciaTexto)
                                        .FontColor(consistenciaColor).Bold();
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(9)
                                    .Element(CeldaBody)
                                    .AlignCenter()
                                    .Text("No hay datos de paquetes")
                                    .Italic()
                                    .FontColor(PdfColors.TextMuted);
                            }
                        });
                    });

                    page.Footer().Element(c => AgregarPiePagina(c, razonSocial));
                });
            }).GeneratePdf();
        }
    }
}