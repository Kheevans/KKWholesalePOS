
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Reports;
using KKWholesalePOS.Core.Services;
using System.Drawing.Printing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Drawing;

namespace KKWholesalePOS.UI
{
    public partial class MainForm : Form
    {
        private readonly UserSessionService _userSession;
        private readonly LoginService _loginService;
        private readonly ICustomerRepository _customerRepository;
        private readonly ReportingService _reportingService;
        private readonly RegularSaleService _regularSaleService;
        private readonly System.Drawing.Color _normalButtonColor =
            System.Drawing.Color.FromArgb(31, 41, 55);

        private readonly System.Drawing.Color _selectedButtonColor =
            System.Drawing.Color.FromArgb(59, 130, 246);
        private readonly PrintDocument _reportPrintDocument = new();
        private int _printRowIndex;
        private SalesReportResult? _currentSalesReport;

        public MainForm(
            UserSessionService userSession,
            LoginService loginService,
            ICustomerRepository customerRepository,
            ReportingService reportingService,
            RegularSaleService regularSaleService)
        {
            InitializeComponent();
            btnExportPdf.Click += btnExportPdf_Click;
            btnExportExcel.Click += btnExportExcel_Click;
            btnExportWord.Click += btnExportWord_Click;

            btnExportPdf.Enabled = false;
            btnExportExcel.Enabled = false;
            btnExportWord.Enabled = false;
            _reportPrintDocument.PrintPage += ReportPrintDocument_PrintPage;

            btnPrintReport.Click += btnPrintReport_Click;
            _reportingService = reportingService;

            _userSession = userSession;
            _loginService = loginService;
            _customerRepository = customerRepository;
            _regularSaleService = regularSaleService;

            ConfigureReportFilters();
            LoadCustomers();

            cmbReportType.SelectedIndexChanged += cmbReportType_SelectedIndexChanged;

            dtpStartDate.ValueChanged += dtpStartDate_ValueChanged;
            dtpEndDate.ValueChanged += dtpEndDate_ValueChanged;

            // Logout
            btnLogout.Click += btnLogout_Click;

            // Navigation
            btnDashboard.Click += btnDashboard_Click;
            btnReports.Click += btnReports_Click;
            btnUserManagement.Click += btnUserManagement_Click;
            btnSettings.Click += btnSettings_Click;

            UpdateSessionDisplay();

            // Open Dashboard by default
            ShowPage(
                pnlDashboard,
                "Dashboard",
                btnDashboard);
        }

        private void LoadCustomers()
        {
            var customers = _customerRepository.GetAll();

            cmbCustomer.DataSource = null;
            cmbCustomer.DisplayMember = nameof(Customer.Name);
            cmbCustomer.ValueMember = nameof(Customer.Id);
            cmbCustomer.DataSource = customers;
            cmbCustomer.SelectedIndex = -1;
        }

        private void ConfigureReportFilters()
        {
            cmbReportType.Items.Clear();

            cmbReportType.Items.Add("Sales Report");
            cmbReportType.Items.Add("Credit Statement");
            cmbReportType.Items.Add("Consignment Statement");
            cmbReportType.Items.Add("Inventory Report");

            cmbReportType.SelectedIndex = 0;

            dtpStartDate.Value = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

            dtpEndDate.Value = DateTime.Today;

            dtpStartDate.MaxDate = DateTime.Today;
            dtpEndDate.MaxDate = DateTime.Today;

            btnGenerateReport.Click += btnGenerateReport_Click;
        }

        private void cmbReportType_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            btnExportPdf.Enabled = false;
            btnExportExcel.Enabled = false;
            btnExportWord.Enabled = false;

            bool requiresCustomer =
                cmbReportType.SelectedIndex == 1 ||
                cmbReportType.SelectedIndex == 2;

            lblCustomer.Visible = requiresCustomer;
            cmbCustomer.Visible = requiresCustomer;

            if (!requiresCustomer)
            {
                cmbCustomer.SelectedIndex = -1;
            }
        }

        private void dtpStartDate_ValueChanged(
            object sender, EventArgs e)
        {
            btnExportPdf.Enabled = false;
            btnExportExcel.Enabled = false;
            btnExportWord.Enabled = false;

            if (dtpStartDate.Value.Date > dtpEndDate.Value.Date)
            {
                dtpEndDate.Value = dtpStartDate.Value.Date;
            }
        }

        private void dtpEndDate_ValueChanged(
            object sender, EventArgs e)
        {
            btnExportPdf.Enabled = false;
            btnExportExcel.Enabled = false;
            btnExportWord.Enabled = false;

            if (dtpEndDate.Value.Date < dtpStartDate.Value.Date)
            {
                dtpStartDate.Value = dtpEndDate.Value.Date;
            }
        }

        private void UpdateSessionDisplay()
        {
            var user = _userSession.CurrentUser;

            if (user == null)
            {
                lblCurrentUser.Text = "Not signed in";
                tsslSessionStatus.Text = "No active session";
            }
            else
            {
                lblCurrentUser.Text =
                    $"Signed in as: {user.Username}";

                tsslSessionStatus.Text =
                    $"Session active - {user.Username}";
            }

            tsslSessionStatus.Spring = false;
            tsslSessionStatus.AutoSize = false;
            tsslSessionStatus.Width = 300;

            tsslDateTime.Spring = false;
            tsslDateTime.AutoSize = false;
            tsslDateTime.Width = 200;
            tsslDateTime.TextAlign =
                ContentAlignment.MiddleRight;

            tsslDateTime.Text =
                DateTime.Now.ToString("dd MMM yyyy");
        }

        private void ShowPage(
            Panel selectedPanel,
            string pageTitle,
            Button selectedButton)
        {
            // Hide all pages
            pnlDashboard.Visible = false;
            pnlReports.Visible = false;
            pnlUserManagement.Visible = false;
            pnlSettings.Visible = false;

            // Display selected page
            selectedPanel.Visible = true;
            selectedPanel.BringToFront();

            // Update page heading
            lblPageTitle.Text = pageTitle;

            lblWelcome.Text = pageTitle == "Dashboard"
                ? "Welcome to K&K Wholesale POS"
                : $"Manage {pageTitle.ToLower()}.";

            // Reset navigation button colours
            btnDashboard.BackColor = _normalButtonColor;
            btnReports.BackColor = _normalButtonColor;
            btnUserManagement.BackColor = _normalButtonColor;
            btnSettings.BackColor = _normalButtonColor;

            // Highlight selected navigation button
            selectedButton.BackColor = _selectedButtonColor;
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            ShowPage(pnlDashboard, "Dashboard", btnDashboard);
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            ShowPage(pnlReports, "Reports", btnReports);
        }

        private void btnUserManagement_Click(object sender, EventArgs e)
        {
            ShowPage(
                pnlUserManagement,
                "User Management",
                btnUserManagement);
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            ShowPage(pnlSettings, "Settings", btnSettings);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            _loginService.Logout();

            Hide();

            using var loginForm = new LoginForm(_loginService);

            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                UpdateSessionDisplay();

                ShowPage(
                    pnlDashboard,
                    "Dashboard",
                    btnDashboard);

                Show();
            }
            else
            {
                Close();
            }
        }

        private void btnGenerateReport_Click(object? sender, EventArgs e)
        {
            try
            {
                DateTime startDate = dtpStartDate.Value.Date;
                DateTime endDate = dtpEndDate.Value.Date;

                switch (cmbReportType.SelectedIndex)
                {
                    case 0:
                        DisplaySalesReport(
                            _reportingService.GetSalesReport(startDate, endDate));
                        break;

                    case 1:
                        if (cmbCustomer.SelectedValue is not int creditCustomerId)
                        {
                            MessageBox.Show(
                                "Please select a customer.",
                                "Customer Required",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        DisplayCreditStatement(
                            _reportingService.GetCreditStatement(
                                creditCustomerId, startDate, endDate));
                        break;

                    case 2:
                        if (cmbCustomer.SelectedValue is not int consignmentCustomerId)
                        {
                            MessageBox.Show(
                                "Please select a customer.",
                                "Customer Required",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        DisplayConsignmentStatement(
                            _reportingService.GetConsignmentStatement(
                                consignmentCustomerId, startDate, endDate));
                        break;

                    case 3:
                        DisplayInventoryReport(
                            _reportingService.GetInventoryReport());
                        break;

                    default:
                        MessageBox.Show("Please select a report type.");
                        break;
                }
            }
            catch (NotImplementedException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Feature Not Implemented (ノಠ益ಠ)ノ彡┻━┻\r\n",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to generate the report: {ex.Message}",
                    "Reporting Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnPrintReport_Click(object? sender, EventArgs e)
        {
            if (dgvReportResults.Rows.Count == 0)
            {
                MessageBox.Show(
                    "There is no report data to print.",
                    "Print Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            try
            {
                _printRowIndex = 0;

                using PrintPreviewDialog previewDialog = new()
                {
                    Document = _reportPrintDocument,
                    Width = 1000,
                    Height = 700
                };

                previewDialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to preview the report: {ex.Message}",
                    "Print Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void ReportPrintDocument_PrintPage(
    object sender,
    PrintPageEventArgs e)
        {
            if (e.Graphics == null)
                return;

            Graphics graphics = e.Graphics;
            Rectangle bounds = e.MarginBounds;

            using System.Drawing.Font titleFont = new(
                "Arial", 16, FontStyle.Bold);

            using System.Drawing.Font summaryFont = new(
                "Arial", 9, FontStyle.Regular);

            using System.Drawing.Font headerFont = new(
                "Arial", 9, FontStyle.Bold);

            using System.Drawing.Font rowFont = new(
                "Arial", 8, FontStyle.Regular);

            using Pen borderPen = new(System.Drawing.Color.Black);

            int y = bounds.Top;

            // Report title
            graphics.DrawString(
                "K&K WHOLESALE",
                titleFont,
                Brushes.Black,
                bounds.Left,
                y);

            y += 35;

            // Report summary
            graphics.DrawString(
                lblReportSummary.Text,
                summaryFont,
                Brushes.Black,
                new RectangleF(
                    bounds.Left,
                    y,
                    bounds.Width,
                    40));

            y += 45;

            int columnCount = dgvReportResults.Columns.Count;

            if (columnCount == 0)
            {
                e.HasMorePages = false;
                return;
            }

            float columnWidth = (float)bounds.Width / columnCount;
            int rowHeight = 30;

            // Column headings
            for (int i = 0; i < columnCount; i++)
            {
                RectangleF cell = new(
                    bounds.Left + i * columnWidth,
                    y,
                    columnWidth,
                    rowHeight);

                graphics.DrawRectangle(
                    borderPen,
                    Rectangle.Round(cell));

                graphics.DrawString(
                    dgvReportResults.Columns[i].HeaderText,
                    headerFont,
                    Brushes.Black,
                    cell);
            }

            y += rowHeight;

            // Report rows
            while (_printRowIndex < dgvReportResults.Rows.Count)
            {
                DataGridViewRow row =
                    dgvReportResults.Rows[_printRowIndex];

                if (row.IsNewRow)
                {
                    _printRowIndex++;
                    continue;
                }

                if (y + rowHeight > bounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                for (int i = 0; i < columnCount; i++)
                {
                    RectangleF cell = new(
                        bounds.Left + i * columnWidth,
                        y,
                        columnWidth,
                        rowHeight);

                    graphics.DrawRectangle(
                        borderPen,
                        Rectangle.Round(cell));

                    string value =
                        row.Cells[i].Value?.ToString() ?? "";

                    graphics.DrawString(
                        value,
                        rowFont,
                        Brushes.Black,
                        cell);
                }

                y += rowHeight;
                _printRowIndex++;
            }

            e.HasMorePages = false;
        }

        private void DisplaySalesReport(SalesReportResult report)
        {
            _currentSalesReport = report;

            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Transactions.ToList();

            lblReportSummary.Text =
                $"Sales Report | {report.StartDate:dd MMM yyyy} - " +
                $"{report.EndDate:dd MMM yyyy} | " +
                $"Transactions: {report.TransactionCount} | " +
                $"Total Revenue: {report.TotalSalesRevenue:C}";

            btnPrintReport.Enabled = report.TransactionCount > 0;

            btnExportPdf.Enabled = report.TransactionCount > 0;
            btnExportExcel.Enabled = report.TransactionCount > 0;
            btnExportWord.Enabled = report.TransactionCount > 0;
        }

        private void DisplayCreditStatement(CreditStatementResult report)
        {
            _currentSalesReport = null;

            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Transactions.ToList();

            lblReportSummary.Text =
                $"Credit Statement: {report.CustomerName} | " +
                $"Purchases: {report.TotalPurchases:C} | " +
                $"Fees: {report.TotalFees:C} | " +
                $"Payments: {report.TotalPayments:C} | " +
                $"Outstanding: {report.TotalOutstanding:C}";

            btnPrintReport.Enabled = report.Transactions.Count > 0;
            btnExportPdf.Enabled = true;
            btnExportExcel.Enabled = true;
            btnExportWord.Enabled = true;
        }

        private void DisplayConsignmentStatement(
            ConsignmentStatementResult report)
        {
            _currentSalesReport = null;
            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Transactions.ToList();

            lblReportSummary.Text =
                $"Consignment Statement: {report.CustomerName} | " +
                $"Original Value: {report.TotalOriginalValue:C} | " +
                $"Accepted Returns: {report.TotalAcceptedReturns:C} | " +
                $"Fees: {report.TotalFees:C} | " +
                $"Payments: {report.TotalPayments:C} | " +
                $"Outstanding: {report.TotalOutstanding:C}";

            btnPrintReport.Enabled = report.Transactions.Count > 0;
            btnExportPdf.Enabled = true;
            btnExportExcel.Enabled = true;
            btnExportWord.Enabled = true;
        }

        private void DisplayInventoryReport(InventoryReportResult report)
        {
            _currentSalesReport = null;

            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Products.ToList();

            lblReportSummary.Text =
                $"Inventory Report | Products: {report.TotalProducts} | " +
                $"Low Stock Items: {report.LowStockProductCount} | " +
                $"Generated: {report.GeneratedAt:dd MMM yyyy HH:mm}";

            btnPrintReport.Enabled = report.TotalProducts > 0;
            btnExportPdf.Enabled = true;
            btnExportExcel.Enabled = true;
            btnExportWord.Enabled = true;
        }

        private void btnExportPdf_Click(object? sender, EventArgs e)
        {
            if (_currentSalesReport == null ||
                _currentSalesReport.TransactionCount == 0)
            {
                MessageBox.Show(
                    "Please generate a sales report before exporting.",
                    "PDF Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using SaveFileDialog saveDialog = new()
            {
                Title = "Export Sales Report",
                Filter = "PDF Files (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                AddExtension = true,
                FileName = $"Sales_Report_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                SalesReportResult report = _currentSalesReport;

                QuestPDF.Fluent.Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(35);

                        page.DefaultTextStyle(style => style.FontSize(9));

                        page.Header()
                            .Column(column =>
                            {
                                column.Item()
                                    .Text("K&K WHOLESALE")
                                    .FontSize(20)
                                    .Bold();

                                column.Item()
                                    .Text("Sales Report")
                                    .FontSize(14)
                                    .SemiBold();

                                column.Item()
                                    .Text(
                                        $"{report.StartDate:dd MMM yyyy} - " +
                                        $"{report.EndDate:dd MMM yyyy}");
                            });

                        page.Content()
                            .PaddingVertical(20)
                            .Column(column =>
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderStyle)
                                            .Text("Sale ID");

                                        header.Cell().Element(HeaderStyle)
                                            .Text("Receipt");

                                        header.Cell().Element(HeaderStyle)
                                            .Text("Date");

                                        header.Cell().Element(HeaderStyle)
                                            .Text("Cashier");

                                        header.Cell().Element(HeaderStyle)
                                            .Text("Total");
                                    });

                                    foreach (SalesReportRow transaction
                                             in report.Transactions)
                                    {
                                        table.Cell().Element(CellStyle)
                                            .Text(transaction.SaleId.ToString());

                                        table.Cell().Element(CellStyle)
                                            .Text(transaction.ReceiptNumber);

                                        table.Cell().Element(CellStyle)
                                            .Text(transaction.SaleDate.ToString("dd MMM yyyy"));

                                        table.Cell().Element(CellStyle)
                                            .Text(transaction.CashierName);

                                        table.Cell().Element(CellStyle)
                                            .Text(transaction.TotalAmount.ToString("C"));
                                    }
                                });

                                column.Item()
                                    .PaddingTop(20)
                                    .AlignRight()
                                    .Text($"Transactions: {report.TransactionCount}")
                                    .SemiBold();

                                column.Item()
                                    .AlignRight()
                                    .Text($"Total Revenue: {report.TotalSalesRevenue:C}")
                                    .FontSize(12)
                                    .Bold();
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("K&K Wholesale | Page ");
                                text.CurrentPageNumber();
                                text.Span(" of ");
                                text.TotalPages();
                            });
                    });
                }).GeneratePdf(saveDialog.FileName);

                MessageBox.Show(
                    "Sales report exported successfully.",
                    "PDF Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to export PDF: {ex.Message}",
                    "PDF Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void btnExportExcel_Click(object? sender, EventArgs e)
        {
            if (_currentSalesReport == null ||
                _currentSalesReport.TransactionCount == 0)
            {
                MessageBox.Show(
                    "Please generate a sales report before exporting.",
                    "Excel Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using SaveFileDialog saveDialog = new()
            {
                Title = "Export Sales Report to Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                FileName = $"Sales_Report_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                SalesReportResult report = _currentSalesReport;

                using XLWorkbook workbook = new();
                IXLWorksheet worksheet = workbook.Worksheets.Add("Sales Report");

                // Report heading
                worksheet.Cell("A1").Value = "K&K WHOLESALE";
                worksheet.Range("A1:E1").Merge();

                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 18;

                worksheet.Cell("A2").Value = "Sales Report";
                worksheet.Range("A2:E2").Merge();
                worksheet.Cell("A2").Style.Font.Bold = true;
                worksheet.Cell("A2").Style.Font.FontSize = 14;

                worksheet.Cell("A3").Value =
                    $"Period: {report.StartDate:dd MMM yyyy} - {report.EndDate:dd MMM yyyy}";

                worksheet.Range("A3:E3").Merge();

                // Column headings
                int headerRow = 5;

                worksheet.Cell(headerRow, 1).Value = "Sale ID";
                worksheet.Cell(headerRow, 2).Value = "Receipt Number";
                worksheet.Cell(headerRow, 3).Value = "Sale Date";
                worksheet.Cell(headerRow, 4).Value = "Cashier";
                worksheet.Cell(headerRow, 5).Value = "Total Amount";

                var header = worksheet.Range(headerRow, 1, headerRow, 5);

                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;
                header.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                // Transaction records
                int currentRow = headerRow + 1;

                foreach (SalesReportRow transaction in report.Transactions)
                {
                    worksheet.Cell(currentRow, 1).Value = transaction.SaleId;
                    worksheet.Cell(currentRow, 2).Value = transaction.ReceiptNumber;
                    worksheet.Cell(currentRow, 3).Value = transaction.SaleDate;
                    worksheet.Cell(currentRow, 4).Value = transaction.CashierName;
                    worksheet.Cell(currentRow, 5).Value =
                        (double)transaction.TotalAmount;

                    worksheet.Cell(currentRow, 3).Style.DateFormat.Format =
                        "dd MMM yyyy HH:mm";

                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format =
                        "\"J$\"#,##0.00";

                    currentRow++;
                }

                // Summary
                int summaryRow = currentRow + 1;

                worksheet.Cell(summaryRow, 4).Value = "Transactions:";
                worksheet.Cell(summaryRow, 5).Value = report.TransactionCount;

                worksheet.Cell(summaryRow + 1, 4).Value = "Total Revenue:";
                worksheet.Cell(summaryRow + 1, 5).Value =
                    (double)report.TotalSalesRevenue;

                worksheet.Cell(summaryRow, 4).Style.Font.Bold = true;
                worksheet.Cell(summaryRow + 1, 4).Style.Font.Bold = true;
                worksheet.Cell(summaryRow + 1, 5).Style.Font.Bold = true;

                worksheet.Cell(summaryRow + 1, 5)
                    .Style.NumberFormat.Format = "\"J$\"#,##0.00";

                // Formatting
                worksheet.Columns().AdjustToContents();
                worksheet.SheetView.FreezeRows(headerRow);

                worksheet.Range(
                    headerRow, 1,
                    currentRow - 1, 5).SetAutoFilter();

                workbook.SaveAs(saveDialog.FileName);

                MessageBox.Show(
                    "Sales report exported successfully to Excel.",
                    "Excel Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to export Excel file: {ex.Message}",
                    "Excel Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private void btnExportWord_Click(object? sender, EventArgs e)
        {
            if (_currentSalesReport == null ||
                _currentSalesReport.TransactionCount == 0)
            {
                MessageBox.Show(
                    "Please generate a sales report before exporting.",
                    "Word Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using SaveFileDialog saveDialog = new()
            {
                Title = "Export Sales Report to Word",
                Filter = "Word Document (*.docx)|*.docx",
                DefaultExt = "docx",
                AddExtension = true,
                FileName = $"Sales_Report_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                SalesReportResult report = _currentSalesReport;

                using WordprocessingDocument document =
                    WordprocessingDocument.Create(
                        saveDialog.FileName,
                        WordprocessingDocumentType.Document);

                MainDocumentPart mainPart =
                    document.AddMainDocumentPart();

                mainPart.Document =
                    new DocumentFormat.OpenXml.Wordprocessing.Document();

                Body body = new();

                // Company heading
                body.Append(CreateWordParagraph(
                    "K&K WHOLESALE",
                    true,
                    "32"));

                body.Append(CreateWordParagraph(
                    "Sales Report",
                    true,
                    "24"));

                body.Append(CreateWordParagraph(
                    $"Period: {report.StartDate:dd MMM yyyy} - " +
                    $"{report.EndDate:dd MMM yyyy}",
                    false,
                    "20"));

                // Transaction table
                Table table = new();

                TableProperties tableProperties = new(
                    new TableBorders(
                        new TopBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new BottomBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new LeftBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new RightBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new InsideHorizontalBorder
                        {
                            Val = BorderValues.Single,
                            Size = 4
                        },
                        new InsideVerticalBorder
                        {
                            Val = BorderValues.Single,
                            Size = 4
                        }));

                table.AppendChild(tableProperties);

                // Header row
                TableRow headerRow = new();

                string[] headers =
                {
            "Sale ID",
            "Receipt",
            "Date",
            "Cashier",
            "Total"
        };

                foreach (string header in headers)
                {
                    headerRow.Append(CreateWordCell(header, true));
                }

                table.Append(headerRow);

                // Transaction rows
                foreach (SalesReportRow transaction in report.Transactions)
                {
                    TableRow row = new();

                    row.Append(CreateWordCell(
                        transaction.SaleId.ToString()));

                    row.Append(CreateWordCell(
                        transaction.ReceiptNumber));

                    row.Append(CreateWordCell(
                        transaction.SaleDate.ToString("dd MMM yyyy")));

                    row.Append(CreateWordCell(
                        transaction.CashierName));

                    row.Append(CreateWordCell(
                        transaction.TotalAmount.ToString("C")));

                    table.Append(row);
                }

                body.Append(table);

                // Report summary
                body.Append(CreateWordParagraph(
                    $"Transactions: {report.TransactionCount}",
                    true,
                    "20"));

                body.Append(CreateWordParagraph(
                    $"Total Revenue: {report.TotalSalesRevenue:C}",
                    true,
                    "24"));

                mainPart.Document.Append(body);
                mainPart.Document.Save();

                MessageBox.Show(
                    "Sales report exported successfully to Word.",
                    "Word Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to export Word document: {ex.Message}",
                    "Word Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static Paragraph CreateWordParagraph(
            string text,
            bool bold,
            string fontSize)
        {
            RunProperties runProperties = new();

            if (bold)
                runProperties.Append(new Bold());

            runProperties.Append(new FontSize
            {
                Val = fontSize
            });

            Run run = new(
                runProperties,
                new Text(text)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });

            return new Paragraph(run);
        }

        private static TableCell CreateWordCell(
            string text,
            bool bold = false)
        {
            RunProperties runProperties = new();

            if (bold)
                runProperties.Append(new Bold());

            Run run = new(
                runProperties,
                new Text(text)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });

            Paragraph paragraph = new(run);

            return new TableCell(
                paragraph,
                new TableCellProperties(
                    new TableCellVerticalAlignment
                    {
                        Val = TableVerticalAlignmentValues.Center
                    }));
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .Border(1)
                .Padding(5);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }
    }
}
