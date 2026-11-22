
using KKWholesalePOS.Core.Interfaces;
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
using KKWholesalePOS.Core.Models.Others;

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
        private readonly DashboardService _dashboardService;
        private Label lblOutstandingCreditValue = null!;
        private Label lblOutstandingConsignmentValue = null!;
        private Label lblLowStockValue = null!;
        private Label lblOverdueCreditValue = null!;
        private Label lblTransactionsToday = null!;
        private Label lblOutOfStockValue = null!;
        private readonly System.Drawing.Color _selectedButtonColor =
            System.Drawing.Color.FromArgb(59, 130, 246);
        private readonly PrintDocument _reportPrintDocument = new();
        private int _printRowIndex;
        private string _currentReportName = "Report";

        public MainForm(
            UserSessionService userSession,
            LoginService loginService,
            ICustomerRepository customerRepository,
            ReportingService reportingService,
            RegularSaleService regularSaleService,
            DashboardService dashboardService)

        {
            InitializeComponent();
            ConfigureDashboardLayout();
            btnExportPdf.Click += btnExportPdf_Click;
            btnExportExcel.Click += btnExportExcel_Click;
            btnExportWord.Click += btnExportWord_Click;

            btnExportPdf.Enabled = false;
            btnExportExcel.Enabled = false;
            btnExportWord.Enabled = false;
            _reportPrintDocument.PrintPage += ReportPrintDocument_PrintPage;

            btnPrintReport.Click += btnPrintReport_Click;
            _reportingService = reportingService;
            _dashboardService = dashboardService;

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

            LoadDashboardSummary();
        }


        private void ConfigureDashboardLayout()
        {
            tlpDashboardCards.SuspendLayout();

            tlpDashboardCards.Controls.Clear();
            tlpDashboardCards.ColumnStyles.Clear();
            tlpDashboardCards.RowStyles.Clear();

            tlpDashboardCards.ColumnCount = 3;
            tlpDashboardCards.RowCount = 3;

            // Sales Today card layout
            lblSalesTodayTitle.Location = new System.Drawing.Point(20, 8);

            lblSalesTodayValue.Location = new System.Drawing.Point(20, 32);
            lblSalesTodayValue.Font = new System.Drawing.Font(
                "Segoe UI", 24F, System.Drawing.FontStyle.Bold);

            lblTransactionsToday = new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font(
                    "Segoe UI", 9F, System.Drawing.FontStyle.Regular),
                ForeColor = System.Drawing.Color.FromArgb(107, 114, 128),
                Location = new System.Drawing.Point(20, 95),
                Text = "0 transactions"
            };

            pnlSalesToday.Controls.Add(lblTransactionsToday);
            lblTransactionsToday.BringToFront();

            for (int i = 0; i < 3; i++)
            {
                tlpDashboardCards.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 33.333F));

                tlpDashboardCards.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 33.333F));
            }

            Panel[] existingCards =
            {
                pnlTotalProducts,
                pnlActiveCustomers,
                pnlSalesToday,
                pnlPendingConsignments
            };

            foreach (Panel card in existingCards)
            {
                card.Dock = DockStyle.Fill;
                card.Margin = new Padding(8);
            }

            lblActiveCustomersTitle.Text = "Total Customers";
            lblPendingConsignmentsTitle.Text = "Unsettled Consignments";

            // Add existing cards
            tlpDashboardCards.Controls.Add(pnlTotalProducts, 0, 0);
            tlpDashboardCards.Controls.Add(pnlActiveCustomers, 1, 0);
            tlpDashboardCards.Controls.Add(pnlSalesToday, 2, 0);
            tlpDashboardCards.Controls.Add(pnlPendingConsignments, 0, 1);

            // Create additional cards
            Panel outstandingCreditCard = CreateDashboardCard(
                "Outstanding Credit",
                out lblOutstandingCreditValue);

            Panel outstandingConsignmentCard = CreateDashboardCard(
                "Outstanding Consignment",
                out lblOutstandingConsignmentValue);

            Panel lowStockCard = CreateDashboardCard(
                "Low Stock Products",
                out lblLowStockValue);

            Panel overdueCreditCard = CreateDashboardCard(
                "Overdue Credit Accounts",
                out lblOverdueCreditValue);

            Panel outOfStockCard = CreateDashboardCard(
                "Out of Stock Products",
                out lblOutOfStockValue);

            // Add additional cards
            tlpDashboardCards.Controls.Add(outstandingCreditCard, 1, 1);
            tlpDashboardCards.Controls.Add(outstandingConsignmentCard, 2, 1);
            tlpDashboardCards.Controls.Add(lowStockCard, 0, 2);
            tlpDashboardCards.Controls.Add(overdueCreditCard, 1, 2);
            tlpDashboardCards.Controls.Add(outOfStockCard, 2, 2);

            tlpDashboardCards.ResumeLayout();
        }

        private Panel CreateDashboardCard(
            string title,
            out Label valueLabel)
        {
            Panel card = new()
            {
                BackColor = System.Drawing.Color.FromArgb(249, 250, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(8)
            };

            Label titleLabel = new()
            {
                AutoSize = true,
                Font = new System.Drawing.Font(
                    "Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(107, 114, 128),
                Location = new System.Drawing.Point(15, 15),
                Text = title
            };

            valueLabel = new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font(
                    "Segoe UI", 24F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(17, 24, 39),
                Location = new System.Drawing.Point(15, 55),
                Text = "-"
            };

            card.Controls.Add(titleLabel);
            card.Controls.Add(valueLabel);

            return card;
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
            LoadDashboardSummary();
        }

        private void LoadDashboardSummary()
        {
            try
            {
                var summary = _dashboardService.GetDashboardSummary();

                // Product and customer totals
                lblTotalProductsValue.Text =
                    summary.TotalProducts.ToString("N0");

                lblActiveCustomersValue.Text =
                    summary.TotalCustomers.ToString("N0");

                // Today's sales
                lblSalesTodayValue.Text =
                    summary.SalesToday.ToString("C");
                
                lblTransactionsToday.Text =
                    $"{summary.TransactionsToday} transactions";

                // Consignments
                lblPendingConsignmentsValue.Text =
                    summary.PendingConsignments.ToString("N0");

                lblOutstandingConsignmentValue.Text =
                    summary.OutstandingConsignmentBalance.ToString("C");

                // Credit accounts
                lblOutstandingCreditValue.Text =
                    summary.OutstandingCreditBalance.ToString("C");

                lblOverdueCreditValue.Text =
                    summary.OverdueCreditAccounts.ToString("N0");

                // Inventory
                lblLowStockValue.Text =
                    summary.LowStockProducts.ToString("N0");

                lblOutOfStockValue.Text =
                    summary.OutOfStockProducts.ToString("N0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to load dashboard information: {ex.Message}",
                    "Dashboard Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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

                LoadDashboardSummary();

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
            _currentReportName = "Sales Report";

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
            _currentReportName = "Credit Statement";

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
            _currentReportName = "Consignment Statement";

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
            _currentReportName = "Inventory Report";

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

        private List<DataGridViewColumn> GetExportColumns()
        {
            return dgvReportResults.Columns
                .Cast<DataGridViewColumn>()
                .Where(column => column.Visible)
                .ToList();
        }

        private List<DataGridViewRow> GetExportRows()
        {
            return dgvReportResults.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow)
                .ToList();
        }

        private string GetExportFileName()
        {
            string safeName = _currentReportName.Replace(" ", "_");

            return $"{safeName}_{DateTime.Now:yyyyMMdd_HHmm}";
        }

        private void btnExportPdf_Click(object? sender, EventArgs e)
        {
            List<DataGridViewColumn> columns = GetExportColumns();
            List<DataGridViewRow> rows = GetExportRows();

            if (columns.Count == 0)
            {
                MessageBox.Show(
                    "Please generate a report before exporting.",
                    "PDF Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using SaveFileDialog saveDialog = new()
            {
                Title = $"Export {_currentReportName}",
                Filter = "PDF Files (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                AddExtension = true,
                FileName = GetExportFileName()
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                QuestPDF.Fluent.Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.DefaultTextStyle(style => style.FontSize(8));

                        page.Header().Column(column =>
                        {
                            column.Item()
                                .Text("K&K WHOLESALE")
                                .FontSize(20)
                                .Bold();

                            column.Item()
                                .Text(_currentReportName)
                                .FontSize(14)
                                .SemiBold();

                            column.Item()
                                .Text(lblReportSummary.Text);
                        });

                        page.Content()
                            .PaddingVertical(15)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(definition =>
                                {
                                    foreach (var unused in columns)
                                    {
                                        definition.RelativeColumn();
                                    }
                                });

                                table.Header(header =>
                                {
                                    foreach (DataGridViewColumn column in columns)
                                    {
                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text(column.HeaderText);
                                    }
                                });

                                foreach (DataGridViewRow row in rows)
                                {
                                    foreach (DataGridViewColumn column in columns)
                                    {
                                        string value =
                                            row.Cells[column.Index].Value?.ToString() ?? "";

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(value);
                                    }
                                }
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
                    $"{_currentReportName} exported successfully.",
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
            List<DataGridViewColumn> columns = GetExportColumns();
            List<DataGridViewRow> rows = GetExportRows();

            if (columns.Count == 0)
            {
                MessageBox.Show(
                    "Please generate a report before exporting.",
                    "Excel Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using SaveFileDialog saveDialog = new()
            {
                Title = $"Export {_currentReportName} to Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                FileName = GetExportFileName()
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using XLWorkbook workbook = new();

                IXLWorksheet worksheet =
                    workbook.Worksheets.Add(_currentReportName);

                int columnCount = columns.Count;

                // Report heading
                worksheet.Cell("A1").Value = "K&K WHOLESALE";
                worksheet.Range(1, 1, 1, columnCount).Merge();

                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 18;

                // Report type
                worksheet.Cell("A2").Value = _currentReportName;
                worksheet.Range(2, 1, 2, columnCount).Merge();

                worksheet.Cell("A2").Style.Font.Bold = true;
                worksheet.Cell("A2").Style.Font.FontSize = 14;

                // Report summary
                worksheet.Cell("A3").Value = lblReportSummary.Text;
                worksheet.Range(3, 1, 3, columnCount).Merge();

                // Column headings
                int headerRow = 5;

                for (int i = 0; i < columnCount; i++)
                {
                    worksheet.Cell(headerRow, i + 1).Value =
                        columns[i].HeaderText;
                }

                var header = worksheet.Range(
                    headerRow, 1,
                    headerRow, columnCount);

                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;
                header.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                // Report data
                int currentRow = headerRow + 1;

                foreach (DataGridViewRow row in rows)
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        object? value =
                            row.Cells[columns[i].Index].Value;

                        worksheet.Cell(currentRow, i + 1).Value =
                            value?.ToString() ?? "";
                    }

                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();
                worksheet.SheetView.FreezeRows(headerRow);

                if (rows.Count > 0)
                {
                    worksheet.Range(
                        headerRow, 1,
                        currentRow - 1, columnCount).SetAutoFilter();
                }

                workbook.SaveAs(saveDialog.FileName);

                MessageBox.Show(
                    $"{_currentReportName} exported successfully to Excel.",
                    "Excel Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to export Excel: {ex.Message}",
                    "Excel Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnExportWord_Click(object? sender, EventArgs e)
        {
            List<DataGridViewColumn> columns = GetExportColumns();
            List<DataGridViewRow> rows = GetExportRows();

            if (columns.Count == 0)
            {
                MessageBox.Show(
                    "Please generate a report before exporting.",
                    "Word Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using SaveFileDialog saveDialog = new()
            {
                Title = $"Export {_currentReportName} to Word",
                Filter = "Word Document (*.docx)|*.docx",
                DefaultExt = "docx",
                AddExtension = true,
                FileName = GetExportFileName()
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using WordprocessingDocument document =
                    WordprocessingDocument.Create(
                        saveDialog.FileName,
                        WordprocessingDocumentType.Document);

                MainDocumentPart mainPart =
                    document.AddMainDocumentPart();

                mainPart.Document =
                    new DocumentFormat.OpenXml.Wordprocessing.Document();

                Body body = new();

                body.Append(CreateWordParagraph(
                    "K&K WHOLESALE", true, "32"));

                body.Append(CreateWordParagraph(
                    _currentReportName, true, "24"));

                body.Append(CreateWordParagraph(
                    lblReportSummary.Text, false, "20"));

                Table table = new();

                table.AppendChild(new TableProperties(
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
                        })));

                // Column headings
                TableRow headerRow = new();

                foreach (DataGridViewColumn column in columns)
                {
                    headerRow.Append(
                        CreateWordCell(column.HeaderText, true));
                }

                table.Append(headerRow);

                // Report data
                foreach (DataGridViewRow dataRow in rows)
                {
                    TableRow row = new();

                    foreach (DataGridViewColumn column in columns)
                    {
                        string value =
                            dataRow.Cells[column.Index].Value?.ToString() ?? "";

                        row.Append(CreateWordCell(value));
                    }

                    table.Append(row);
                }

                body.Append(table);

                mainPart.Document.Append(body);
                mainPart.Document.Save();

                MessageBox.Show(
                    $"{_currentReportName} exported successfully to Word.",
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
