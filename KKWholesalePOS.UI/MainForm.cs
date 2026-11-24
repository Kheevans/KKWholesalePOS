using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Credit;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Reports;
using KKWholesalePOS.Core.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Drawing;
using System.Drawing.Printing;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;

namespace KKWholesalePOS.UI
{
    public partial class MainForm : Form
    {
        // ==========================================
        // Services and Dependencies
        // ==========================================
        private readonly UserSessionService _userSession;
        private readonly LoginService _loginService;
        private readonly ICustomerRepository _customerRepository;
        private readonly ReportingService _reportingService;
        private readonly RegularSaleService _regularSaleService;
        private readonly DashboardService _dashboardService;
        private readonly UserManagementService _userManagementService;
        private readonly RoleManagementService _roleManagementService;
        private readonly PermissionManagementService _permissionManagementService;
        private readonly AuditLogService _auditLogService;
        private readonly AuthorizationService _authorizationService;
        private readonly BusinessSettingsService _businessSettingsService;
        private readonly CreditService _creditService;
        private readonly ConsignmentReturnService _consignmentReturnService;

        // ==========================================
        // Shared UI Styling
        // ==========================================
        private readonly System.Drawing.Color _normalButtonColor =
            System.Drawing.Color.FromArgb(31, 41, 55);
        private readonly System.Drawing.Color _selectedButtonColor =
            System.Drawing.Color.FromArgb(59, 130, 246);

        // ==========================================
        // Dashboard Controls
        // ==========================================
        private Label lblOutstandingCreditValue = null!;
        private Label lblOutstandingConsignmentValue = null!;
        private Label lblLowStockValue = null!;
        private Label lblOverdueCreditValue = null!;
        private Label lblTransactionsToday = null!;
        private Label lblOutOfStockValue = null!;
        private Panel _pnlOutstandingCreditCard = null!;
        private Panel _pnlOutstandingConsignmentCard = null!;
        private Panel _pnlLowStockCard = null!;
        private Panel _pnlOverdueCreditCard = null!;
        private Panel _pnlOutOfStockCard = null!;

        // ==========================================
        // Credit Management Controls
        // ==========================================
        private Button _btnCreditManagement = null!;
        private Button _btnSearchCreditAccounts = null!;
        private Button _btnViewCreditPayments = null!;
        private Button _btnRecordCreditPayment = null!;
        private TextBox _txtCreditCustomerSearch = null!;
        private ComboBox _cmbCreditCustomer = null!;
        private DataGridView _creditAccountsGrid = null!;
        private List<KKWholesalePOS.Core.Models.Others.Customer> _allCreditCustomers = new();
        private Panel _pnlCreditManagement = null!;

        // ==========================================
        // User Management Controls
        // ==========================================
        private TabControl _userManagementTabControl = null!;
        private TabPage _usersTab = null!;
        private TabPage _rolesTab = null!;
        private TabPage _permissionsTab = null!;
        private TabPage _auditLogTab = null!;
        private DataGridView _usersGrid = null!;
        private DataGridView _rolesGrid = null!;
        private DataGridView _permissionsGrid = null!;
        private DataGridView _auditLogGrid = null!;
        private Label _permissionCountLabel = null!;
        private Button _createUserButton = null!;
        private Button _activateUserButton = null!;
        private Button _deactivateUserButton = null!;
        private Button _createRoleButton = null!;
        private Button _assignRoleButton = null!;
        private Button _removeRoleButton = null!;
        private Button _assignPermissionButton = null!;
        private Button _removePermissionButton = null!;

        // ==========================================
        // Business Information Settings
        // ==========================================
        private TextBox txtBusinessName = null!;
        private TextBox txtBusinessAddress = null!;
        private TextBox txtPhoneNumber = null!;
        private TextBox txtEmailAddress = null!;

        // ==========================================
        // Receipt Settings
        // ==========================================
        private TextBox txtReceiptHeader = null!;
        private TextBox txtReceiptFooter = null!;
        private System.Windows.Forms.CheckBox chkShowBusinessContactDetails = null!;

        // ==========================================
        // System Preferences
        // ==========================================
        private ComboBox cmbCurrencyCode = null!;
        private ComboBox cmbDateFormat = null!;

        // ==========================================
        // Inventory Preferences
        // ==========================================
        private NumericUpDown nudDefaultMinimumStockLevel = null!;

        // ==========================================
        // Settings Actions
        // ==========================================
        private Button btnSaveSettings = null!;

        // ==========================================
        // Reporting
        // ==========================================
        private readonly PrintDocument _reportPrintDocument = new();
        private int _printRowIndex;
        private string _currentReportName = "Report";
        private Label lblStatementReference = null!;
        private ComboBox cmbStatementReference = null!;

        public MainForm(
            UserSessionService userSession,
            LoginService loginService,
            ICustomerRepository customerRepository,
            ReportingService reportingService,
            RegularSaleService regularSaleService,
            DashboardService dashboardService,
            UserManagementService userManagementService,
            RoleManagementService roleManagementService,
            PermissionManagementService permissionManagementService,
            AuditLogService auditLogService,
            AuthorizationService authorizationService,
            BusinessSettingsService businessSettingsService,
            CreditService creditService,
            ConsignmentReturnService consignmentReturnService)
        {
            InitializeComponent();

            _reportingService = reportingService;
            _dashboardService = dashboardService;
            _userSession = userSession;
            _loginService = loginService;
            _customerRepository = customerRepository;
            _regularSaleService = regularSaleService;
            _userManagementService = userManagementService;
            _roleManagementService = roleManagementService;
            _permissionManagementService = permissionManagementService;
            _auditLogService = auditLogService;
            _authorizationService = authorizationService;
            _businessSettingsService = businessSettingsService;
            _creditService = creditService;
            _consignmentReturnService = consignmentReturnService;

            this.FormClosing += MainForm_FormClosing;

            ConfigureDashboardLayout();
            ConfigureUserManagementLayout();
            ConfigureCreditManagementLayout();
            ConfigureSettingsLayout();

            tabConsignmentReturns.Controls.Add(
                new ConsignmentReturnsControl(_consignmentReturnService)
                { Dock = DockStyle.Fill });

            btnExportPdf.Click += btnExportPdf_Click;
            btnExportExcel.Click += btnExportExcel_Click;
            btnExportWord.Click += btnExportWord_Click;
            _btnSearchCreditAccounts.Click += btnSearchCreditAccounts_Click;
            _btnViewCreditPayments.Click += btnViewCreditPayments_Click;
            _btnRecordCreditPayment.Click += btnRecordCreditPayment_Click;

            _txtCreditCustomerSearch.TextChanged += (sender, e) =>
            {
                FilterCreditCustomerDropdown(_txtCreditCustomerSearch.Text);
            };

            _txtCreditCustomerSearch.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    btnSearchCreditAccounts_Click(sender, EventArgs.Empty);
                }
                else if (e.KeyCode == Keys.Down && _cmbCreditCustomer.Items.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    _cmbCreditCustomer.Focus();
                    _cmbCreditCustomer.DroppedDown = true;
                }
            };

            _cmbCreditCustomer.SelectedIndexChanged += (sender, e) =>
            {
                if (_cmbCreditCustomer.SelectedValue is int customerId && customerId > 0)
                {
                    LoadCustomerCreditAccounts(customerId);
                }
            };

            btnExportPdf.Enabled = false;
            btnExportExcel.Enabled = false;
            btnExportWord.Enabled = false;
            _reportPrintDocument.PrintPage += ReportPrintDocument_PrintPage;

            btnPrintReport.Click += btnPrintReport_Click;

            if (HasPermission("Users.Manage") ||
                HasPermission("Roles.Manage"))
            {
                LoadUsers();
            }

            if (HasPermission("Roles.Manage") ||
                HasPermission("Permissions.Manage"))
            {
                LoadRoles();
            }

            if (HasPermission("Permissions.Manage"))
            {
                LoadPermissions();
            }

            if (HasPermission("AuditLog.View"))
            {
                LoadAuditLog();
            }
            LoadCustomers();
            ConfigureReportFilters();
            ApplyNavigationPermissions();

            cmbReportType.SelectedIndexChanged += cmbReportType_SelectedIndexChanged;
            cmbCustomer.SelectedIndexChanged += (sender, e) => DisableReportExports();

            dtpStartDate.ValueChanged += dtpStartDate_ValueChanged;
            dtpEndDate.ValueChanged += dtpEndDate_ValueChanged;

            // Logout
            btnLogout.Click += btnLogout_Click;

            // Navigation
            btnDashboard.Click += btnDashboard_Click;
            btnReports.Click += btnReports_Click;
            btnConsignments.Click += btnConsignments_Click;
            btnUserManagement.Click += btnUserManagement_Click;
            _btnCreditManagement.Click += btnCreditManagement_Click;
            btnSettings.Click += btnSettings_Click;

            UpdateSessionDisplay();

            ShowDefaultPage();
        }

        private void btnConsignments_Click(object? sender, EventArgs e)
        {
            ShowPage(pnlConsignments, "Consignments", btnConsignments);
        }

        private void MainForm_FormClosing(
            object? sender,
            FormClosingEventArgs e)
        {
            if (_userSession.CurrentUser != null &&
                e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;

                MessageBox.Show(
                    "You must log out before closing the application.",
                    "Logout Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ConfigureDashboardLayout()
        {
            tlpDashboardCards.SuspendLayout();

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

            lblTransactionsToday = new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular),
                ForeColor = System.Drawing.Color.FromArgb(107, 114, 128),
                Location = new System.Drawing.Point(
                    lblSalesTodayValue.Left,
                    lblSalesTodayValue.Bottom + 6),
                Text = "0 transactions"
            };
            pnlSalesToday.Controls.Add(lblTransactionsToday);

            // Create additional cards once
            _pnlOutstandingCreditCard = CreateDashboardCard(
                "Outstanding Credit",
                out lblOutstandingCreditValue);

            _pnlOutstandingConsignmentCard = CreateDashboardCard(
                "Outstanding Consignment",
                out lblOutstandingConsignmentValue);

            _pnlLowStockCard = CreateDashboardCard(
                "Low Stock Products",
                out lblLowStockValue);

            _pnlOverdueCreditCard = CreateDashboardCard(
                "Overdue Credit Accounts",
                out lblOverdueCreditValue);

            _pnlOutOfStockCard = CreateDashboardCard(
                "Out of Stock Products",
                out lblOutOfStockValue);

            tlpDashboardCards.ResumeLayout();

            ApplyDashboardPermissions();
        }

        private void ApplyDashboardPermissions()
        {
            if (tlpDashboardCards == null || _pnlOutOfStockCard == null)
                return;

            bool canSeeSales =
                HasPermission("Sales.Process") ||
                HasPermission("Reports.View");

            bool canSeeCustomers =
                HasPermission("Customers.Manage") ||
                HasPermission("Credit.Manage") ||
                HasPermission("Reports.View");

            bool canSeeInventory =
                HasPermission("Inventory.View") ||
                HasPermission("Inventory.Receive") ||
                HasPermission("Inventory.Adjust") ||
                HasPermission("Reports.View");

            bool canSeeProducts =
                canSeeInventory ||
                HasPermission("Sales.Process");

            bool canSeeCredit =
                HasPermission("Credit.Manage") ||
                HasPermission("Reports.View");

            bool canSeeConsignment =
                HasPermission("Consignment.Process") ||
                HasPermission("Consignment.Manage") ||
                HasPermission("Consignments.Issue") ||
                HasPermission("Consignments.Return") ||
                HasPermission("Reports.View");

            var visibleCards = new List<Panel>();

            if (canSeeSales)
                visibleCards.Add(pnlSalesToday);

            if (canSeeCustomers)
                visibleCards.Add(pnlActiveCustomers