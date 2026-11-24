
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
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;

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
        private readonly UserManagementService _userManagementService;
        private readonly RoleManagementService _roleManagementService;
        private readonly PermissionManagementService _permissionManagementService;
        private readonly AuditLogService _auditLogService;
        private readonly AuthorizationService _authorizationService;
        private readonly ConsignmentReturnService _consignmentReturnService;
        private readonly BusinessSettingsService _businessSettingsService;
        private Label lblOutstandingCreditValue = null!;
        private Label lblOutstandingConsignmentValue = null!;
        private Label lblLowStockValue = null!;
        private Label lblOverdueCreditValue = null!;
        private Label lblTransactionsToday = null!;
        private Label lblOutOfStockValue = null!;
        private DataGridView _usersGrid = null!;
        private DataGridView _rolesGrid = null!;
        private DataGridView _permissionsGrid = null!;
        private DataGridView _auditLogGrid = null!;
        private Label _permissionCountLabel = null!;
        private TabControl _userManagementTabControl = null!;
        private TabPage _usersTab = null!;
        private TabPage _rolesTab = null!;
        private TabPage _permissionsTab = null!;
        private TabPage _auditLogTab = null!;
        private Button _createUserButton = null!;
        private Button _activateUserButton = null!;
        private Button _deactivateUserButton = null!;
        private Button _assignRoleButton = null!;
        private Button _removeRoleButton = null!;
        private Button _createRoleButton = null!;
        private Button _assignPermissionButton = null!;
        private Button _removePermissionButton = null!;
        // Business information
        private TextBox txtBusinessName = null!;
        private TextBox txtBusinessAddress = null!;
        private TextBox txtPhoneNumber = null!;
        private TextBox txtEmailAddress = null!;

        // Receipt settings
        private TextBox txtReceiptHeader = null!;
        private TextBox txtReceiptFooter = null!;
        private System.Windows.Forms.CheckBox chkShowBusinessContactDetails = null!;
        // System preferences
        private ComboBox cmbCurrencyCode = null!;
        private ComboBox cmbDateFormat = null!;

        // Inventory preferences
        private NumericUpDown nudDefaultMinimumStockLevel = null!;

        // Settings action
        private Button btnSaveSettings = null!;
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
            DashboardService dashboardService,
            UserManagementService userManagementService,
            RoleManagementService roleManagementService,
            PermissionManagementService permissionManagementService,
            AuditLogService auditLogService,
            AuthorizationService authorizationService,
            ConsignmentReturnService consignmentReturnService)
            BusinessSettingsService businessSettingsService)
        {
            InitializeComponent();

            this.FormClosing += MainForm_FormClosing;

            ConfigureDashboardLayout();
            ConfigureUserManagementLayout();
            ConfigureSettingsLayout();

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
            _userManagementService = userManagementService;
            _roleManagementService = roleManagementService;
            _permissionManagementService = permissionManagementService;
            _auditLogService = auditLogService;
            _authorizationService = authorizationService;
            _consignmentReturnService = consignmentReturnService;
            _businessSettingsService = businessSettingsService;

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

            dtpStartDate.ValueChanged += dtpStartDate_ValueChanged;
            dtpEndDate.ValueChanged += dtpEndDate_ValueChanged;

            // Logout
            btnLogout.Click += btnLogout_Click;

            // Navigation
            btnDashboard.Click += btnDashboard_Click;
            btnReports.Click += btnReports_Click;
            btnConsignments.Click += btnConsignments_Click;
            btnUserManagement.Click += btnUserManagement_Click;
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

        private void ConfigureSettingsLayout()
        {
            pnlSettings.Controls.Clear();

            pnlSettings.AutoScroll = true;
            pnlSettings.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            pnlSettings.Padding = new Padding(20);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10),
                BackColor = System.Drawing.Color.Transparent
            };

            mainLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));

            // Business Information
            var businessGroup = CreateSettingsGroup(
                "Business Information");

            txtBusinessName = CreateSettingsTextBox();
            txtBusinessAddress = CreateSettingsTextBox();
            txtPhoneNumber = CreateSettingsTextBox();
            txtEmailAddress = CreateSettingsTextBox();

            AddSettingsField(businessGroup, "Business Name", txtBusinessName);
            AddSettingsField(businessGroup, "Address", txtBusinessAddress);
            AddSettingsField(businessGroup, "Telephone", txtPhoneNumber);
            AddSettingsField(businessGroup, "Email Address", txtEmailAddress);

            // Receipt Settings
            var receiptGroup = CreateSettingsGroup("Receipt Settings");

            txtReceiptHeader = CreateSettingsTextBox();
            txtReceiptFooter = CreateSettingsTextBox();

            chkShowBusinessContactDetails = new System.Windows.Forms.CheckBox
            {
                Text = "Show business contact details on receipts",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 10, 3, 10)
            };

            AddSettingsField(receiptGroup, "Receipt Header", txtReceiptHeader);
            AddSettingsField(receiptGroup, "Receipt Footer", txtReceiptFooter);

            receiptGroup.Controls.Add(chkShowBusinessContactDetails);

            // System Preferences
            var systemGroup = CreateSettingsGroup("System Preferences");

            cmbCurrencyCode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Width = 250
            };

            cmbCurrencyCode.Items.AddRange(new object[]
            {
        "JMD",
        "USD"
            });

            cmbDateFormat = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Width = 250
            };

            cmbDateFormat.Items.AddRange(new object[]
            {
        "dd/MM/yyyy",
        "MM/dd/yyyy",
        "yyyy-MM-dd"
            });

            AddSettingsField(systemGroup, "Currency", cmbCurrencyCode);
            AddSettingsField(systemGroup, "Date Format", cmbDateFormat);

            // Inventory Preferences
            var inventoryGroup = CreateSettingsGroup(
                "Inventory Preferences");

            nudDefaultMinimumStockLevel = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100000,
                Width = 250,
                Font = new Font("Segoe UI", 10F)
            };

            AddSettingsField(
                inventoryGroup,
                "Default Minimum Stock Level",
                nudDefaultMinimumStockLevel);

            // Save button
            btnSaveSettings = new Button
            {
                Text = "Save Settings",
                Width = 150,
                Height = 40,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Margin = new Padding(3, 15, 3, 10)
            };

            btnSaveSettings.FlatAppearance.BorderSize = 0;
            btnSaveSettings.Click += btnSaveSettings_Click;

            // Add sections
            mainLayout.Controls.Add(businessGroup);
            mainLayout.Controls.Add(receiptGroup);
            mainLayout.Controls.Add(systemGroup);
            mainLayout.Controls.Add(inventoryGroup);
            mainLayout.Controls.Add(btnSaveSettings);

            pnlSettings.Controls.Add(mainLayout);
        }

        private GroupBox CreateSettingsGroup(string title)
        {
            return new GroupBox
            {
                Text = title,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Padding = new Padding(15),
                Margin = new Padding(5, 5, 5, 15)
            };
        }

        private TextBox CreateSettingsTextBox()
        {
            return new TextBox
            {
                Width = 350,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 5, 3, 10)
            };
        }

        private void AddSettingsField(
            System.Windows.Forms.GroupBox group,
            string labelText,
            System.Windows.Forms.Control input)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(3, 5, 3, 5)
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 180F));

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));

            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 8, 3, 8)
            };

            input.Anchor = AnchorStyles.Left;

            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(input, 1, 0);

            group.Controls.Add(layout);
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

        private void LoadUsers()
        {
            _usersGrid.Rows.Clear();

            var users = _userManagementService.GetAllUsers();

            foreach (var user in users)
            {
                string roles = user.UserRoles.Any()
                    ? string.Join(", ", user.UserRoles
                        .Where(ur => ur.Role != null)
                        .Select(ur => ur.Role.Name))
                    : "No roles assigned";

                _usersGrid.Rows.Add(
                    user.Id,
                    user.Username,
                    roles,
                    user.IsActive ? "Active" : "Inactive");
            }
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

        private void ApplyNavigationPermissions()
        {
            btnDashboard.Enabled = HasPermission("Dashboard.View");
            btnReports.Enabled = HasPermission("Reports.View");
            btnConsignments.Enabled = HasPermission("Consignments.Return");

            btnUserManagement.Enabled =
                HasPermission("Users.Manage") ||
                HasPermission("Roles.Manage") ||
                HasPermission("Permissions.Manage") ||
                HasPermission("AuditLog.View") ||
                HasPermission("Consignments.Issue") ||
                HasPermission("Consignments.Return");

            btnSettings.Enabled = HasPermission("Settings.Manage");

            ApplyManagementTabPermissions();
            ApplyManagementActionPermissions();
        }


        private void ShowPage(
            Panel selectedPanel,
            string pageTitle,
            Button selectedButton)
        {
            if (!CanAccessPage(pageTitle))
            {
                MessageBox.Show("You do not have permission to access this page.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (pageTitle == "User Management")
            {
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
            }

            // Hide all pages
            pnlDashboard.Visible = false;
            pnlReports.Visible = false;
            pnlUserManagement.Visible = false;
            pnlSettings.Visible = false;
            pnlConsignments.Visible = false;

            // Display selected page
            selectedPanel.Visible = true;
            selectedPanel.BringToFront();

            // Update page heading
            lblPageTitle.Text = pageTitle;

            lblWelcome.Text = pageTitle switch
            {
                "Dashboard" => "Welcome to K&K Wholesale POS",
                "Reports" => "View and generate business reports.",
                "Consignments" => "Issue consignments, process returns and record settlements.",
                "User Management" => "Manage system users, roles and permissions.",
                "Settings" => "Configure your business and system preferences.",
                _ => string.Empty
            };

            // Reset navigation button colours
            btnDashboard.BackColor = _normalButtonColor;
            btnReports.BackColor = _normalButtonColor;
            btnConsignments.BackColor = _normalButtonColor;
            btnUserManagement.BackColor = _normalButtonColor;
            btnSettings.BackColor = _normalButtonColor;

            // Highlight selected navigation button
            selectedButton.BackColor = _selectedButtonColor;
        }

        private void ShowDefaultPage()
        {
            if (CanAccessPage("Dashboard"))
            {
                ShowPage(pnlDashboard, "Dashboard", btnDashboard);
                LoadDashboardSummary();
            }
            else if (CanAccessPage("Reports"))
            {
                ShowPage(pnlReports, "Reports", btnReports);
            }
            else if (CanAccessPage("User Management"))
            {
                ShowPage(pnlUserManagement, "User Management", btnUserManagement);
            }
            else if (CanAccessPage("Settings"))
            {
                ShowPage(pnlSettings, "Settings", btnSettings);
            }
            else
            {
                MessageBox.Show(
                    "Your account does not have access to any system pages. Please contact the system administrator.",
                    "No Access",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            if (!CanAccessPage("Dashboard"))
                return;

            ShowPage(pnlDashboard, "Dashboard", btnDashboard);
            LoadDashboardSummary();
        }

        private void LoadBusinessSettings()
        {
            try
            {
                var settings = _businessSettingsService.GetSettings();

                txtBusinessName.Text = settings.BusinessName;
                txtBusinessAddress.Text = settings.BusinessAddress;
                txtPhoneNumber.Text = settings.PhoneNumber;
                txtEmailAddress.Text = settings.EmailAddress;

                txtReceiptHeader.Text = settings.ReceiptHeader;
                txtReceiptFooter.Text = settings.ReceiptFooter;

                chkShowBusinessContactDetails.Checked =
                    settings.ShowBusinessContactDetails;

                cmbCurrencyCode.SelectedItem = settings.CurrencyCode;
                cmbDateFormat.SelectedItem = settings.DateFormat;

                nudDefaultMinimumStockLevel.Value =
                    settings.DefaultMinimumStockLevel;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "You do not have permission to access business settings.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to load business settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                var settings = _businessSettingsService.GetSettings();

                settings.BusinessName = txtBusinessName.Text.Trim();
                settings.BusinessAddress = txtBusinessAddress.Text.Trim();
                settings.PhoneNumber = txtPhoneNumber.Text.Trim();
                settings.EmailAddress = txtEmailAddress.Text.Trim();

                settings.ReceiptHeader = txtReceiptHeader.Text.Trim();
                settings.ReceiptFooter = txtReceiptFooter.Text.Trim();

                settings.ShowBusinessContactDetails =
                    chkShowBusinessContactDetails.Checked;

                settings.CurrencyCode =
                    cmbCurrencyCode.SelectedItem?.ToString() ?? "JMD";

                settings.DateFormat =
                    cmbDateFormat.SelectedItem?.ToString() ?? "dd/MM/yyyy";

                settings.DefaultMinimumStockLevel =
                    (int)nudDefaultMinimumStockLevel.Value;

                _businessSettingsService.UpdateSettings(settings);

                MessageBox.Show(
                    "Business settings saved successfully.",
                    "Settings Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "You do not have permission to update business settings.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to save business settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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
            if (!RequirePermission("Settings.Manage"))
            {
                return;
            }

            ShowPage(pnlSettings, "Settings", btnSettings);

            LoadBusinessSettings();
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
                ApplyNavigationPermissions();
                ShowDefaultPage();
                Show();
            }
            else
            {
                Close();
            }
        }

        private bool RequirePermission(string permissionName)
        {
            if (HasPermission(permissionName))
                return true;

            MessageBox.Show(
                "You do not have permission to perform this action.",
                "Access Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return false;
        }

        private void btnGenerateReport_Click(object? sender, EventArgs e)
        {
            if (!RequirePermission("Reports.View"))
                return;
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
            if (!RequirePermission("Reports.View"))
                return;

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
            if (!RequirePermission("Reports.Export"))
                return;
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
            if (!RequirePermission("Reports.Export"))
                return;
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
            if (!RequirePermission("Reports.Export"))
                return;
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


        private void ConfigureUserManagementLayout()
        {
            pnlUserManagement.Controls.Clear();

            _userManagementTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 10F),
                Padding = new Point(15, 5)
            };

            _usersTab = new TabPage("Users")
            {
                Name = "tabUsers",
                BackColor = System.Drawing.Color.White
            };

            _rolesTab = new TabPage("Roles")
            {
                Name = "tabRoles",
                BackColor = System.Drawing.Color.White
            };

            _permissionsTab = new TabPage("Permissions")
            {
                Name = "tabPermissions",
                BackColor = System.Drawing.Color.White
            };

            _auditLogTab = new TabPage("Audit Log")
            {
                Name = "tabAuditLog",
                BackColor = System.Drawing.Color.White
            };

            _userManagementTabControl.TabPages.Add(_usersTab);
            _userManagementTabControl.TabPages.Add(_rolesTab);
            _userManagementTabControl.TabPages.Add(_permissionsTab);
            _userManagementTabControl.TabPages.Add(_auditLogTab);

            pnlUserManagement.Controls.Add(_userManagementTabControl);

            ConfigureUsersTab(_usersTab);
            ConfigureRolesTab(_rolesTab);
            ConfigurePermissionsTab(_permissionsTab);
            ConfigureAuditLogTab(_auditLogTab);
        }

        private void ApplyManagementTabPermissions()
        {
            _userManagementTabControl.TabPages.Clear();

            if (HasPermission("Users.Manage") ||
                HasPermission("Roles.Manage"))
            {
                _userManagementTabControl.TabPages.Add(_usersTab);
            }

            if (HasPermission("Roles.Manage") ||
                HasPermission("Permissions.Manage"))
            {
                _userManagementTabControl.TabPages.Add(_rolesTab);
            }

            if (HasPermission("Permissions.Manage"))
            {
                _userManagementTabControl.TabPages.Add(_permissionsTab);
            }

            if (HasPermission("AuditLog.View"))
            {
                _userManagementTabControl.TabPages.Add(_auditLogTab);
            }
        }

        private void ApplyManagementActionPermissions()
        {
            bool canManageUsers = HasPermission("Users.Manage");
            bool canManageRoles = HasPermission("Roles.Manage");
            bool canManagePermissions = HasPermission("Permissions.Manage");

            _createUserButton.Enabled = canManageUsers;
            _activateUserButton.Enabled = canManageUsers;
            _deactivateUserButton.Enabled = canManageUsers;

            _assignRoleButton.Enabled = canManageRoles;
            _removeRoleButton.Enabled = canManageRoles;

            _createRoleButton.Enabled = canManageRoles;

            _assignPermissionButton.Enabled = canManagePermissions;
            _removePermissionButton.Enabled = canManagePermissions;
        }

        private void ConfigureRolesTab(TabPage rolesTab)
        {
            rolesTab.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20),
                BackColor = System.Drawing.Color.White
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));

            var headingPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var titleLabel = new Label
            {
                Text = "System Roles",
                Font = new System.Drawing.Font(
                    "Segoe UI", 18F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var descriptionLabel = new Label
            {
                Text = "Create roles and manage their permissions.",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.Gray,
                AutoSize = true,
                Location = new Point(2, 45)
            };

            headingPanel.Controls.Add(titleLabel);
            headingPanel.Controls.Add(descriptionLabel);

            var rolesGrid = new DataGridView
            {
                Name = "dgvRoles",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = System.Drawing.Color.White
            };

            rolesGrid.Columns.Add("Id", "ID");
            rolesGrid.Columns.Add("Name", "Role Name");
            rolesGrid.Columns.Add("Description", "Description");
            rolesGrid.Columns.Add("Permissions", "Assigned Permissions");

            rolesGrid.Columns["Id"]!.Visible = false;
            rolesGrid.Columns["Name"]!.FillWeight = 25;
            rolesGrid.Columns["Description"]!.FillWeight = 35;
            rolesGrid.Columns["Permissions"]!.FillWeight = 40;

            _rolesGrid = rolesGrid;

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };

            _createRoleButton = new Button
            {
                Name = "btnCreateRole",
                Text = "Create Role",
                Size = new System.Drawing.Size(130, 38),
                BackColor = System.Drawing.Color.FromArgb(59, 130, 246),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            _assignPermissionButton = new Button
            {
                Name = "btnAssignPermission",
                Text = "Assign Permission",
                Size = new System.Drawing.Size(150, 38),
                BackColor = System.Drawing.Color.FromArgb(34, 139, 94),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            _removePermissionButton = new Button
            {
                Name = "btnRemovePermission",
                Text = "Remove Permission",
                Size = new System.Drawing.Size(160, 38),
                BackColor = System.Drawing.Color.FromArgb(220, 75, 75),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            buttonPanel.Controls.Add(_createRoleButton);
            buttonPanel.Controls.Add(_assignPermissionButton);
            buttonPanel.Controls.Add(_removePermissionButton);

            _createRoleButton.Click += (sender, e) => ShowCreateRoleDialog();
            _assignPermissionButton.Click += (sender, e) => ShowAssignPermissionDialog();
            _removePermissionButton.Click += (sender, e) => ShowRemovePermissionDialog();

            layout.Controls.Add(headingPanel, 0, 0);
            layout.Controls.Add(rolesGrid, 0, 1);
            layout.Controls.Add(buttonPanel, 0, 2);

            rolesTab.Controls.Add(layout);
        }


        private void ConfigurePermissionsTab(TabPage permissionsTab)
        {
            permissionsTab.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20),
                BackColor = System.Drawing.Color.White
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            var headingPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var titleLabel = new Label
            {
                Text = "System Permissions",
                Font = new System.Drawing.Font(
                    "Segoe UI", 18F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var descriptionLabel = new Label
            {
                Text = "View all permissions available within the system.",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.Gray,
                AutoSize = true,
                Location = new Point(2, 45)
            };

            headingPanel.Controls.Add(titleLabel);
            headingPanel.Controls.Add(descriptionLabel);

            var permissionsGrid = new DataGridView
            {
                Name = "dgvPermissions",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = System.Drawing.Color.White
            };

            permissionsGrid.Columns.Add("Id", "ID");
            permissionsGrid.Columns.Add("Name", "Permission Name");
            permissionsGrid.Columns.Add("Description", "Description");

            permissionsGrid.Columns["Id"]!.Visible = false;
            permissionsGrid.Columns["Name"]!.FillWeight = 35;
            permissionsGrid.Columns["Description"]!.FillWeight = 65;

            _permissionsGrid = permissionsGrid;

            var footerPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var permissionCountLabel = new Label
            {
                Text = "Total permissions: 0",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(2, 15)
            };

            _permissionCountLabel = permissionCountLabel;

            footerPanel.Controls.Add(permissionCountLabel);

            layout.Controls.Add(headingPanel, 0, 0);
            layout.Controls.Add(permissionsGrid, 0, 1);
            layout.Controls.Add(footerPanel, 0, 2);

            permissionsTab.Controls.Add(layout);
        }


        private void ConfigureAuditLogTab(TabPage auditLogTab)
        {
            auditLogTab.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20),
                BackColor = System.Drawing.Color.White
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var headingPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var refreshButton = new Button
            {
                Text = "Refresh",
                Size = new System.Drawing.Size(120, 35),
                Location = new Point(0, 5),
                BackColor = System.Drawing.Color.FromArgb(59, 130, 246),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            buttonPanel.Controls.Add(refreshButton);

            refreshButton.Click += (sender, e) =>
            {
                if (!RequirePermission("AuditLog.View"))
                    return;

                LoadAuditLog();
            };

            var titleLabel = new Label
            {
                Text = "System Audit Log",
                Font = new System.Drawing.Font(
                    "Segoe UI", 18F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var descriptionLabel = new Label
            {
                Text = "Review recorded activities and changes made within the system.",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.Gray,
                AutoSize = true,
                Location = new Point(2, 45)
            };

            headingPanel.Controls.Add(titleLabel);
            headingPanel.Controls.Add(descriptionLabel);

            var auditGrid = new DataGridView
            {
                Name = "dgvAuditLog",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = System.Drawing.Color.White,
                AutoGenerateColumns = false
            };

            auditGrid.Columns.Add("Timestamp", "Date & Time");
            auditGrid.Columns.Add("Username", "Username");
            auditGrid.Columns.Add("Action", "Action");
            auditGrid.Columns.Add("EntityType", "Entity");
            auditGrid.Columns.Add("EntityId", "Entity ID");
            auditGrid.Columns.Add("Details", "Details");

            auditGrid.Columns["Timestamp"]!.FillWeight = 20;
            auditGrid.Columns["Username"]!.FillWeight = 10;
            auditGrid.Columns["Action"]!.FillWeight = 20;
            auditGrid.Columns["EntityType"]!.FillWeight = 15;
            auditGrid.Columns["EntityId"]!.FillWeight = 10;
            auditGrid.Columns["Details"]!.FillWeight = 35;

            _auditLogGrid = auditGrid;

            layout.Controls.Add(headingPanel, 0, 0);
            layout.Controls.Add(buttonPanel, 0, 1);
            layout.Controls.Add(auditGrid, 0, 2);

            auditLogTab.Controls.Add(layout);
        }


        private void ShowCreateRoleDialog()
        {
            if (!RequirePermission("Roles.Manage"))
                return;
            using var dialog = new Form
            {
                Text = "Create Role",
                Size = new System.Drawing.Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var nameLabel = new Label
            {
                Text = "Role Name:",
                Location = new Point(20, 25),
                AutoSize = true
            };

            var nameTextBox = new TextBox
            {
                Location = new Point(20, 50),
                Width = 340
            };

            var descriptionLabel = new Label
            {
                Text = "Description:",
                Location = new Point(20, 90),
                AutoSize = true
            };

            var descriptionTextBox = new TextBox
            {
                Location = new Point(20, 115),
                Width = 340
            };

            var saveButton = new Button
            {
                Text = "Create",
                DialogResult = DialogResult.None,
                Location = new Point(180, 165),
                Size = new System.Drawing.Size(85, 32)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(275, 165),
                Size = new System.Drawing.Size(85, 32)
            };

            saveButton.Click += (sender, e) =>
            {
                try
                {
                    _roleManagementService.CreateRole(
                        nameTextBox.Text,
                        descriptionTextBox.Text);

                    _auditLogService.LogAction(
                        _userSession.CurrentUser!.Id,
                        "Create Role",
                        "Role",
                        details: $"Created role: {nameTextBox.Text.Trim()}");

                    MessageBox.Show(
                        "Role created successfully.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();

                    LoadRoles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Unable to Create Role",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

            dialog.Controls.AddRange(new System.Windows.Forms.Control[]            {
        nameLabel,
        nameTextBox,
        descriptionLabel,
        descriptionTextBox,
        saveButton,
        cancelButton
            });

            dialog.AcceptButton = saveButton;
            dialog.CancelButton = cancelButton;

            dialog.ShowDialog(this);
        }

        private void LoadRoles()
        {
            _rolesGrid.Rows.Clear();

            var roles = _roleManagementService.GetAllRoles();

            foreach (var role in roles)
            {
                var permissions = _permissionManagementService
                    .GetRolePermissions(role.Id);

                _rolesGrid.Rows.Add(
                    role.Id,
                    role.Name,
                    role.Description,
                    string.Join(", ", permissions.Select(p => p.Name)));
            }
        }

        private void LoadPermissions()
        {
            _permissionsGrid.Rows.Clear();

            var permissions = _permissionManagementService.GetAllPermissions();

            foreach (var permission in permissions)
            {
                _permissionsGrid.Rows.Add(
                    permission.Id,
                    permission.Name,
                    permission.Description);
            }

            _permissionCountLabel.Text =
                $"Total permissions: {permissions.Count}";
        }


        private void LoadAuditLog()
        {
            _auditLogGrid.Rows.Clear();

            var entries = _auditLogService.GetAllEntries()
                .OrderByDescending(entry => entry.Timestamp)
                .ToList();

            foreach (var entry in entries)
            {
                _auditLogGrid.Rows.Add(
                    entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    entry.User?.Username ?? "System",
                    entry.Action,
                    entry.EntityType,
                    entry.EntityId ?? string.Empty,
                    entry.Details ?? string.Empty);
            }
        }

        private void ShowAssignPermissionDialog()
        {
            if (!RequirePermission("Permissions.Manage"))
                return;
            if (_rolesGrid.CurrentRow == null)
            {
                MessageBox.Show(
                    "Please select a role first.",
                    "Assign Permission",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            int roleId = Convert.ToInt32(
                _rolesGrid.CurrentRow.Cells["Id"].Value);

            string roleName = Convert.ToString(
                _rolesGrid.CurrentRow.Cells["Name"].Value) ?? string.Empty;

            var allPermissions = _permissionManagementService.GetAllPermissions();
            var assignedPermissions = _permissionManagementService.GetRolePermissions(roleId);

            var availablePermissions = allPermissions
                .Where(p => !assignedPermissions.Any(
                    assigned => assigned.Id == p.Id))
                .ToList();

            if (availablePermissions.Count == 0)
            {
                MessageBox.Show(
                    "All available permissions have already been assigned to this role.",
                    "Assign Permission",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using Form dialog = new Form
            {
                Text = $"Assign Permission - {roleName}",
                Size = new System.Drawing.Size(450, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var permissionLabel = new Label
            {
                Text = "Select a permission to assign:",
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var permissionComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 55),
                Size = new System.Drawing.Size(390, 30),
                DataSource = availablePermissions,
                DisplayMember = "Name",
                ValueMember = "Id"
            };

            var descriptionLabel = new Label
            {
                Location = new Point(20, 100),
                Size = new System.Drawing.Size(390, 70),
                ForeColor = System.Drawing.Color.Gray
            };

            permissionComboBox.SelectedIndexChanged += (sender, e) =>
            {
                if (permissionComboBox.SelectedItem is KKWholesalePOS.Core.Models.Others.Permission selected)
                {
                    descriptionLabel.Text = selected.Description;
                }
            };

            var assignButton = new Button
            {
                Text = "Assign",
                Size = new System.Drawing.Size(120, 38),
                Location = new Point(160, 240),
                BackColor = System.Drawing.Color.FromArgb(34, 139, 94),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new System.Drawing.Size(120, 38),
                Location = new Point(290, 240),
                DialogResult = DialogResult.Cancel
            };

            assignButton.Click += (sender, e) =>
            {
                if (permissionComboBox.SelectedItem is not KKWholesalePOS.Core.Models.Others.Permission selected)
                {
                    MessageBox.Show("Please select a permission.");
                    return;
                }

                try
                {
                    _permissionManagementService.AssignPermissionToRole(
                        roleId, selected.Id);

                    _auditLogService.LogAction(
                        _userSession.CurrentUser!.Id,
                        "Assign Permission",
                        "Role",
                        roleId.ToString(),
                        $"Assigned permission '{selected.Name}' to role '{roleName}'.");

                    LoadRoles();

                    MessageBox.Show(
                        "Permission assigned successfully.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            dialog.Controls.AddRange(new System.Windows.Forms.Control[]
            {
        permissionLabel,
        permissionComboBox,
        descriptionLabel,
        assignButton,
        cancelButton
            });

            dialog.AcceptButton = assignButton;
            dialog.CancelButton = cancelButton;

            dialog.ShowDialog(this);
        }


        private void ShowRemovePermissionDialog()
        {
            if (!RequirePermission("Permissions.Manage"))
                return;
            if (_rolesGrid.CurrentRow == null)
            {
                MessageBox.Show(
                    "Please select a role first.",
                    "Remove Permission",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            int roleId = Convert.ToInt32(
                _rolesGrid.CurrentRow.Cells["Id"].Value);

            string roleName = Convert.ToString(
                _rolesGrid.CurrentRow.Cells["Name"].Value) ?? string.Empty;

            var assignedPermissions =
                _permissionManagementService.GetRolePermissions(roleId);

            if (assignedPermissions.Count == 0)
            {
                MessageBox.Show(
                    "This role does not have any assigned permissions.",
                    "Remove Permission",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using Form dialog = new Form
            {
                Text = $"Remove Permission - {roleName}",
                Size = new System.Drawing.Size(450, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var permissionLabel = new Label
            {
                Text = "Select a permission to remove:",
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var permissionComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 55),
                Size = new System.Drawing.Size(390, 30),
                DataSource = assignedPermissions,
                DisplayMember = "Name",
                ValueMember = "Id"
            };

            var descriptionLabel = new Label
            {
                Location = new Point(20, 100),
                Size = new System.Drawing.Size(390, 70),
                ForeColor = System.Drawing.Color.Gray
            };

            permissionComboBox.SelectedIndexChanged += (sender, e) =>
            {
                if (permissionComboBox.SelectedItem is KKWholesalePOS.Core.Models.Others.Permission selected)
                {
                    descriptionLabel.Text = selected.Description;
                }
            };

            var removeButton = new Button
            {
                Text = "Remove",
                Size = new System.Drawing.Size(120, 38),
                Location = new Point(160, 240),
                BackColor = System.Drawing.Color.FromArgb(220, 75, 75),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new System.Drawing.Size(120, 38),
                Location = new Point(290, 240),
                DialogResult = DialogResult.Cancel
            };

            removeButton.Click += (sender, e) =>
            {
                if (permissionComboBox.SelectedItem is not KKWholesalePOS.Core.Models.Others.Permission selected)
                {
                    MessageBox.Show("Please select a permission.");
                    return;
                }

                var confirmation = MessageBox.Show(
                    $"Remove '{selected.Name}' from the '{roleName}' role?",
                    "Confirm Removal",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmation != DialogResult.Yes)
                    return;

                try
                {
                    _permissionManagementService.RemovePermissionFromRole(
                        roleId, selected.Id);

                    _auditLogService.LogAction(
                        _userSession.CurrentUser!.Id,
                        "Remove Permission",
                        "Role",
                        roleId.ToString(),
                        $"Removed permission '{selected.Name}' from role '{roleName}'.");

                    LoadRoles();

                    MessageBox.Show(
                        "Permission removed successfully.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            dialog.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                permissionLabel,
                permissionComboBox,
                descriptionLabel,
                removeButton,
                cancelButton
            });

            dialog.AcceptButton = removeButton;
            dialog.CancelButton = cancelButton;

            dialog.ShowDialog(this);
        }

        private void ConfigureUsersTab(TabPage usersTab)
        {
            usersTab.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20),
                BackColor = System.Drawing.Color.White
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));

            var headingPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var titleLabel = new Label
            {
                Text = "System Users",
                Font = new System.Drawing.Font(
                    "Segoe UI", 18F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var descriptionLabel = new Label
            {
                Text = "Create accounts and manage user access.",
                Font = new System.Drawing.Font("Segoe UI", 10F),
                ForeColor = System.Drawing.Color.Gray,
                AutoSize = true,
                Location = new Point(2, 45)
            };

            headingPanel.Controls.Add(titleLabel);
            headingPanel.Controls.Add(descriptionLabel);

            var usersGrid = new DataGridView
            {
                Name = "dgvUsers",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = System.Drawing.Color.White
            };

            usersGrid.Columns.Add("Id", "ID");
            usersGrid.Columns.Add("Username", "Username");
            usersGrid.Columns.Add("Roles", "Assigned Roles");
            usersGrid.Columns.Add("Status", "Status");

            usersGrid.Columns["Id"]!.Visible = false;
            usersGrid.Columns["Username"]!.FillWeight = 30;
            usersGrid.Columns["Roles"]!.FillWeight = 50;
            usersGrid.Columns["Status"]!.FillWeight = 20;

            _usersGrid = usersGrid;

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };

            _createUserButton = new Button
            {
                Name = "btnCreateUser",
                Text = "Create User",
                Size = new System.Drawing.Size(130, 38),
                BackColor = System.Drawing.Color.FromArgb(59, 130, 246),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            _activateUserButton = new Button
            {
                Name = "btnActivateUser",
                Text = "Activate",
                Size = new System.Drawing.Size(130, 38)
            };

            _deactivateUserButton = new Button
            {
                Name = "btnDeactivateUser",
                Text = "Deactivate",
                Size = new System.Drawing.Size(130, 38)
            };

            _assignRoleButton = new Button
            {
                Name = "btnAssignRole",
                Text = "Assign Role",
                Size = new System.Drawing.Size(130, 38)
            };

            _removeRoleButton = new Button
            {
                Name = "btnRemoveRole",
                Text = "Remove Role",
                Size = new System.Drawing.Size(150, 38)
            };

            _createUserButton.Click += (sender, e) => ShowCreateUserDialog();
            _activateUserButton.Click += (sender, e) => ActivateSelectedUser();
            _deactivateUserButton.Click += (sender, e) => DeactivateSelectedUser();
            _assignRoleButton.Click += (sender, e) => ShowAssignRoleDialog();
            _removeRoleButton.Click += (sender, e) => ShowRemoveRoleDialog();

            buttonPanel.Controls.Add(_createUserButton);
            buttonPanel.Controls.Add(_activateUserButton);
            buttonPanel.Controls.Add(_deactivateUserButton);
            buttonPanel.Controls.Add(_assignRoleButton);
            buttonPanel.Controls.Add(_removeRoleButton);

            layout.Controls.Add(headingPanel, 0, 0);
            layout.Controls.Add(usersGrid, 0, 1);
            layout.Controls.Add(buttonPanel, 0, 2);

            usersTab.Controls.Add(layout);
        }


        private void ShowAssignRoleDialog()
        {
            if (!RequirePermission("Roles.Manage"))
                return;
            if (!TryGetSelectedUserId(out int userId))
                return;

            var allRoles = _roleManagementService.GetAllRoles();
            var assignedRoles = _roleManagementService.GetUserRoles(userId);

            var availableRoles = allRoles
                .Where(role => assignedRoles.All(
                    assigned => assigned.Id != role.Id))
                .ToList();

            if (availableRoles.Count == 0)
            {
                MessageBox.Show(
                    "There are no additional roles available to assign.",
                    "No Available Roles",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using var dialog = new Form
            {
                Text = "Assign Role",
                Size = new System.Drawing.Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var roleLabel = new Label
            {
                Text = "Select a role:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            var roleComboBox = new ComboBox
            {
                Location = new Point(20, 50),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(Role.Name),
                ValueMember = nameof(Role.Id),
                DataSource = availableRoles
            };

            var assignButton = new Button
            {
                Text = "Assign",
                Location = new Point(190, 105),
                Size = new System.Drawing.Size(80, 35),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(280, 105),
                Size = new System.Drawing.Size(80, 35),
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                roleLabel,
                roleComboBox,
                assignButton,
                cancelButton
            });

            dialog.AcceptButton = assignButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (roleComboBox.SelectedValue is not int roleId)
                return;

            try
            {
                _roleManagementService.AssignRoleToUser(userId, roleId);

                _auditLogService.LogAction(
                    _userSession.CurrentUser?.Id,
                    "Assign Role",
                    "User",
                    userId.ToString(),
                    $"Assigned role ID {roleId} to user ID {userId}.");

                LoadUsers();

                MessageBox.Show(
                    "Role assigned successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Assign Role",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private void ShowRemoveRoleDialog()
        {
            if (!RequirePermission("Roles.Manage"))
                return;
            if (!TryGetSelectedUserId(out int userId))
                return;

            var assignedRoles = _roleManagementService.GetUserRoles(userId);

            if (assignedRoles.Count == 0)
            {
                MessageBox.Show(
                    "This user does not have any assigned roles.",
                    "No Assigned Roles",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using var dialog = new Form
            {
                Text = "Remove Role",
                Size = new System.Drawing.Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var roleLabel = new Label
            {
                Text = "Select a role to remove:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            var roleComboBox = new ComboBox
            {
                Location = new Point(20, 50),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(Role.Name),
                ValueMember = nameof(Role.Id),
                DataSource = assignedRoles
            };

            var removeButton = new Button
            {
                Text = "Remove",
                Location = new Point(190, 105),
                Size = new System.Drawing.Size(90, 35),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(280, 105),
                Size = new System.Drawing.Size(90, 35),
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                roleLabel,
                roleComboBox,
                removeButton,
                cancelButton
            });

            dialog.AcceptButton = removeButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (roleComboBox.SelectedValue is not int roleId)
                return;

            var confirmation = MessageBox.Show(
                "Are you sure you want to remove this role from the user?",
                "Confirm Role Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
                return;

            try
            {
                string roleName = assignedRoles
                    .First(role => role.Id == roleId).Name;

                _roleManagementService.RemoveRoleFromUser(userId, roleId);

                _auditLogService.LogAction(
                    _userSession.CurrentUser?.Id,
                    "Remove Role",
                    "User",
                    userId.ToString(),
                    $"Removed role '{roleName}' from user ID {userId}.");

                LoadUsers();

                MessageBox.Show(
                    "Role removed successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Remove Role",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private bool HasPermission(string permissionName)
        {
            var currentUser = _userSession.CurrentUser;

            return currentUser != null &&
                   _authorizationService.HasPermission(
                       currentUser,
                       permissionName);
        }


        private bool CanAccessPage(string pageTitle)
        {
            return pageTitle switch
            {
                "Dashboard" => HasPermission("Dashboard.View"),
                "Reports" => HasPermission("Reports.View"),
                "Consignments" => 
                    HasPermission("Consignments.Issue") ||
                    HasPermission("Consignments.Return"),
                "User Management" =>
                    HasPermission("Users.Manage") ||
                    HasPermission("Roles.Manage") ||
                    HasPermission("Permissions.Manage") ||
                    HasPermission("AuditLog.View"),
                "Settings" => HasPermission("Settings.Manage"),
                _ => false
            };
        }

        private bool TryGetSelectedUserId(out int userId)
        {
            userId = 0;

            if (_usersGrid.CurrentRow == null)
            {
                MessageBox.Show(
                    "Please select a user first.",
                    "No User Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            object? value = _usersGrid.CurrentRow.Cells["Id"].Value;

            if (value == null || !int.TryParse(value.ToString(), out userId))
            {
                MessageBox.Show(
                    "Unable to identify the selected user.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        private void ShowCreateUserDialog()
        {
            if (!RequirePermission("Users.Manage"))
                return;
            using var dialog = new Form
            {
                Text = "Create User",
                Size = new System.Drawing.Size(450, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var usernameLabel = new Label
            {
                Text = "Username",
                Location = new Point(20, 20),
                AutoSize = true
            };

            var usernameTextBox = new TextBox
            {
                Location = new Point(20, 45),
                Width = 350
            };

            var passwordLabel = new Label
            {
                Text = "Password",
                Location = new Point(20, 85),
                AutoSize = true
            };

            var passwordTextBox = new TextBox
            {
                Location = new Point(20, 110),
                Width = 350,
                UseSystemPasswordChar = true
            };

            var createButton = new Button
            {
                Text = "Create",
                Location = new Point(190, 165),
                Size = new System.Drawing.Size(85, 35),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(285, 165),
                Size = new System.Drawing.Size(85, 35),
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                usernameLabel,
                usernameTextBox,
                passwordLabel,
                passwordTextBox,
                createButton,
                cancelButton
            });

            dialog.AcceptButton = createButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var user = _userManagementService.CreateUser(
                    usernameTextBox.Text,
                    passwordTextBox.Text);

                _auditLogService.LogAction(
                    _userSession.CurrentUser?.Id,
                    "Create",
                    "User",
                    user.Id.ToString(),
                    $"Created user account: {user.Username}");

                LoadUsers();

                MessageBox.Show(
                    "User account created successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Create User",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void DeactivateSelectedUser()
        {
            if (!RequirePermission("Users.Manage"))
                return;
            if (!TryGetSelectedUserId(out int userId))
            {
                return;
            }

            if (_userSession.CurrentUser?.Id == userId)
            {
                MessageBox.Show(
                    "You cannot deactivate the account currently being used.",
                    "Action Not Allowed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var confirmation = MessageBox.Show(
                "Are you sure you want to deactivate this user?",
                "Confirm Deactivation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _userManagementService.DeactivateUser(userId);

                _auditLogService.LogAction(
                    _userSession.CurrentUser?.Id,
                    "Deactivate",
                    "User",
                    userId.ToString(),
                    "User account deactivated.");

                LoadUsers();

                MessageBox.Show(
                    "User deactivated successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Deactivate User",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ActivateSelectedUser()
        {
            if (!RequirePermission("Users.Manage"))
                return;
            if (!TryGetSelectedUserId(out int userId))
            {
                return;
            }

            var confirmation = MessageBox.Show(
                "Are you sure you want to activate this user?",
                "Confirm Activation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _userManagementService.ActivateUser(userId);

                _auditLogService.LogAction(
                    _userSession.CurrentUser?.Id,
                    "Activate",
                    "User",
                    userId.ToString(),
                    "User account activated.");

                LoadUsers();

                MessageBox.Show(
                    "User activated successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Activate User",
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

        private void btnUserManagement_Click_1(object sender, EventArgs e)
        {

        }
    }
}
