
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Reports;
using KKWholesalePOS.Core.Services;
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
        private readonly Color _normalButtonColor =
            Color.FromArgb(31, 41, 55);

        private readonly Color _selectedButtonColor =
            Color.FromArgb(59, 130, 246);

        public MainForm(
            UserSessionService userSession,
            LoginService loginService,
            ICustomerRepository customerRepository,
            ReportingService reportingService,
            RegularSaleService regularSaleService)
        {
            InitializeComponent();
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
            if (dtpStartDate.Value.Date > dtpEndDate.Value.Date)
            {
                dtpEndDate.Value = dtpStartDate.Value.Date;
            }
        }

        private void dtpEndDate_ValueChanged(
            object sender, EventArgs e)
        {
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

        private void DisplaySalesReport(SalesReportResult report)
        {
            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Transactions.ToList();

            lblReportSummary.Text =
                $"Sales Report | {report.StartDate:dd MMM yyyy} - " +
                $"{report.EndDate:dd MMM yyyy} | " +
                $"Transactions: {report.TransactionCount} | " +
                $"Total Revenue: {report.TotalSalesRevenue:C}";

            btnPrintReport.Enabled = report.TransactionCount > 0;
        }

        private void DisplayCreditStatement(CreditStatementResult report)
        {
            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Transactions.ToList();

            lblReportSummary.Text =
                $"Credit Statement: {report.CustomerName} | " +
                $"Purchases: {report.TotalPurchases:C} | " +
                $"Fees: {report.TotalFees:C} | " +
                $"Payments: {report.TotalPayments:C} | " +
                $"Outstanding: {report.TotalOutstanding:C}";

            btnPrintReport.Enabled = report.Transactions.Count > 0;
        }

        private void DisplayConsignmentStatement(
            ConsignmentStatementResult report)
        {
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
        }

        private void DisplayInventoryReport(InventoryReportResult report)
        {
            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Products.ToList();

            lblReportSummary.Text =
                $"Inventory Report | Products: {report.TotalProducts} | " +
                $"Low Stock Items: {report.LowStockProductCount} | " +
                $"Generated: {report.GeneratedAt:dd MMM yyyy HH:mm}";

            btnPrintReport.Enabled = report.TotalProducts > 0;
        }
    }
}
