using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using KKWholesalePOS.Core;
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Models.Credit;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Reports;
using KKWholesalePOS.Core.Models.Sales;
using KKWholesalePOS.Core.Models.Settings;
using KKWholesalePOS.Core.Services;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Drawing;
using System.Drawing.Printing;
using CheckBox = System.Windows.Forms.CheckBox;
using Color = System.Drawing.Color;
using Control = System.Windows.Forms.Control;
using DrawingFontFamily = System.Drawing.FontFamily;
using Font = System.Drawing.Font;
using PdfColors = QuestPDF.Helpers.Colors;
using Size = System.Drawing.Size;
using W = DocumentFormat.OpenXml.Wordprocessing;

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
        private readonly InventoryManagementService _inventoryManagementService;
        private readonly CustomerManagementService _customerManagementService;

        // ==========================================
        // Shared UI Styling
        // ==========================================
        private readonly Color _normalButtonColor = Color.FromArgb(31, 41, 55);
        private readonly Color _selectedButtonColor = Color.FromArgb(59, 130, 246);

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
        private Button _btnIssueCreditSale = null!;
        private TextBox _txtCreditCustomerSearch = null!;
        private ComboBox _cmbCreditCustomer = null!;
        private DataGridView _creditAccountsGrid = null!;
        private List<Customer> _allCreditCustomers = new();
        private Panel _pnlCreditManagement = null!;

        // ==========================================
        // Sales / POS Controls
        // ==========================================
        private Button _btnSales = null!;
        private Panel _pnlSales = null!;
        private TextBox _txtPosBarcode = null!;
        private NumericUpDown _nudPosQuantity = null!;
        private DataGridView _dgvPosCart = null!;
        private Label _lblPosSubtotalGct = null!;
        private Label _lblPosTotal = null!;
        private TextBox _txtPosCashReceived = null!;
        private Label _lblPosStatus = null!;
        private Button _btnPosPrintReceipt = null!;
        private readonly List<SaleRequestLine> _posCart = new();
        private string? _lastPosReceipt;

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
        private CheckBox chkShowBusinessContactDetails = null!;

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

        // ==========================================
        // Consignment Returns & Settlement Controls
        // ==========================================
        private TextBox _txtConsignmentRef = null!;
        private Button _btnFindConsignment = null!;
        private TextBox _txtConsignmentCustomerSearch = null!;
        private ComboBox _cmbConsignmentCustomer = null!;
        private ComboBox _cmbCustomerConsignments = null!;
        private DataGridView _dgvConsignmentLines = null!;
        private Label _lblConsignmentStatus = null!;
        private Label _lblConsignmentSummary = null!;
        private Button _btnProcessReturn = null!;
        private Button _btnViewConsignmentHistory = null!;
        private NumericUpDown _nudConsignmentPayment = null!;
        private ComboBox _cmbConsignmentPaymentMethod = null!;
        private Button _btnRecordConsignmentPayment = null!;
        private IReadOnlyList<Customer> _allConsignmentCustomers = Array.Empty<Customer>();
        private ConsignmentReturnView? _currentConsignmentView;

        // ==========================================
        // Inventory Controls
        // ==========================================
        private DataGridView _inventoryGrid = null!;
        private TextBox _inventorySearch = null!;
        private ComboBox _inventoryFilter = null!;
        private Label _inventoryTotal = null!;
        private Label _inventoryLow = null!;
        private Label _inventoryOut = null!;
        private Label _inventoryInvalid = null!;
        private Label _inventoryResultCount = null!;
        private Button _inventoryReceive = null!;
        private Button _inventoryAdjust = null!;
        private Button _inventoryMinimum = null!;
        private Button _inventoryHistory = null!;
        private Button _inventoryRefresh = null!;
        private Button _inventoryAddProduct = null!;
        private Button _inventoryEditProduct = null!;
        private List<InventoryRow> _inventoryRows = new();
        private bool _inventoryBusy;

        // ==========================================
        // Customer Management Controls
        // ==========================================
        private Button _btnCustomers = null!;
        private Panel _pnlCustomers = null!;
        private TextBox _txtCustomerSearch = null!;
        private CheckBox _chkIncludeInactiveCustomers = null!;
        private DataGridView _customersGrid = null!;
        private Button _btnAddCustomer = null!;
        private Button _btnEditCustomer = null!;
        private Button _btnRefreshCustomers = null!;
        private List<Customer> _customerRows = new();

        public MainForm(
            UserSessionService userSession,
            LoginService loginService,
            ICustomerRepository customerRepository,
            CustomerManagementService customerManagementService,
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
            ConsignmentReturnService consignmentReturnService,
            InventoryManagementService inventoryManagementService)
        {
            InitializeComponent();

            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _customerManagementService = customerManagementService ?? throw new ArgumentNullException(nameof(customerManagementService));
            _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
            _regularSaleService = regularSaleService ?? throw new ArgumentNullException(nameof(regularSaleService));
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _roleManagementService = roleManagementService ?? throw new ArgumentNullException(nameof(roleManagementService));
            _permissionManagementService = permissionManagementService ?? throw new ArgumentNullException(nameof(permissionManagementService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _businessSettingsService = businessSettingsService ?? throw new ArgumentNullException(nameof(businessSettingsService));
            _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
            _consignmentReturnService = consignmentReturnService ?? throw new ArgumentNullException(nameof(consignmentReturnService));
            _inventoryManagementService = inventoryManagementService ?? throw new ArgumentNullException(nameof(inventoryManagementService));

            this.FormClosing += MainForm_FormClosing;

            ConfigureDashboardLayout();
            ConfigureUserManagementLayout();
            ConfigureCreditManagementLayout();
            ConfigureCustomersLayout();
            ConfigureSettingsLayout();
            ConfigureConsignmentReturnsTab();
            ConfigureInventoryLayout();
            ConfigureSalesLayout();

            btnExportPdf.Click += btnExportPdf_Click;
            btnExportExcel.Click += btnExportExcel_Click;
            btnExportWord.Click += btnExportWord_Click;
            _btnSearchCreditAccounts.Click += btnSearchCreditAccounts_Click;
            _btnViewCreditPayments.Click += btnViewCreditPayments_Click;
            _btnRecordCreditPayment.Click += btnRecordCreditPayment_Click;

            _txtCreditCustomerSearch.TextChanged += (_, _) =>
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

            _cmbCreditCustomer.SelectedIndexChanged += (_, _) =>
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
            cmbCustomer.SelectedIndexChanged += (_, _) => DisableReportExports();

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
            _btnCustomers.Visible = true;
            _btnCustomers.Enabled = HasPermission("Customers.Manage");
            _btnCustomers.BackColor = _btnCustomers.Enabled
                ? _normalButtonColor
                : Color.Gray;
            btnSettings.Click += btnSettings_Click;

            UpdateSessionDisplay();

            ShowDefaultPage();
        }

        private void btnConsignments_Click(object? sender, EventArgs e)
        {
            ShowPage(pnlConsignments, "Consignments", btnConsignments);
        }

        private void btnProducts_Click(object? sender, EventArgs e)
        {
            if (!RequirePermission("Inventory.View"))
                return;

            ShowPage(pnlInventory, "Inventory Management", btnProducts);

            LoadInventory();
        }

        private void ConfigureInventoryLayout()
        {
            pnlInventory.Controls.Clear();
            pnlInventory.BackColor = Color.White;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12),
                BackColor = Color.White
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 72));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 28));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 44));

            // Inventory summary cards
            var summaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8)
            };

            for (int i = 0; i < 4; i++)
            {
                summaryLayout.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 25));
            }

            summaryLayout.Controls.Add(
                CreateInventoryCard(
                    "Total Products",
                    out _inventoryTotal,
                    Color.FromArgb(17, 24, 39)), 0, 0);

            summaryLayout.Controls.Add(
                CreateInventoryCard(
                    "Low Stock Products",
                    out _inventoryLow,
                    Color.DarkOrange), 1, 0);

            summaryLayout.Controls.Add(
                CreateInventoryCard(
                    "Out of Stock",
                    out _inventoryOut,
                    Color.Firebrick), 2, 0);

            summaryLayout.Controls.Add(
                CreateInventoryCard(
                    "Invalid Balances",
                    out _inventoryInvalid,
                    Color.Firebrick), 3, 0);

            layout.Controls.Add(summaryLayout, 0, 0);

            // Product search and stock status filters
            var filterPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(243, 244, 246),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 8)
            };

            var searchLabel = new Label
            {
                Text = "Search Product",
                AutoSize = true,
                Location = new Point(12, 6)
            };

            _inventorySearch = new TextBox
            {
                Location = new Point(12, 27),
                Width = 280,
                PlaceholderText = "Enter product name or barcode"
            };

            var filterLabel = new Label
            {
                Text = "Stock Status",
                AutoSize = true,
                Location = new Point(308, 6)
            };

            _inventoryFilter = new ComboBox
            {
                Location = new Point(308, 27),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _inventoryFilter.Items.AddRange(new object[]
            {
                "All Products",
                "In Stock",
                "Low Stock",
                "Out of Stock",
                "Invalid Balances"
            });

            _inventoryFilter.SelectedIndex = 0;
            _inventoryAddProduct = CreateInventoryButton("Add Product");
            _inventoryEditProduct = CreateInventoryButton("Edit Product");
            _inventoryRefresh = CreateInventoryButton("Refresh");

            _inventoryRefresh.Location = new Point(504, 25);

            filterPanel.Controls.Add(searchLabel);
            filterPanel.Controls.Add(_inventorySearch);
            filterPanel.Controls.Add(filterLabel);
            filterPanel.Controls.Add(_inventoryFilter);
            filterPanel.Controls.Add(_inventoryRefresh);

            layout.Controls.Add(filterPanel, 0, 1);

            // Inventory table
            _inventoryGrid = CreateInventoryGrid();

            AddInventoryColumn(
                _inventoryGrid, "ProductName", "Product", 150);

            AddInventoryColumn(
                _inventoryGrid, "Barcode", "Barcode", 100);

            AddInventoryColumn(
                _inventoryGrid, "SellingPrice", $"Price ({GetCurrencyPrefix()})", 85, "N2");

            AddInventoryColumn(
                _inventoryGrid, "UnitsPerCase", "Units/Case", 75);

            AddInventoryColumn(
                _inventoryGrid, "AvailableStock", "Available", 80);

            AddInventoryColumn(
                _inventoryGrid, "OutstandingConsignmentQuantity",
                "Outstanding", 95);

            AddInventoryColumn(
                _inventoryGrid, "MinimumStockLevel", "Minimum", 80);

            AddInventoryColumn(
                _inventoryGrid, "Status", "Status", 105);

            layout.Controls.Add(_inventoryGrid, 0, 2);

            // Display the number of products matching the current filters
            _inventoryResultCount = new Label
            {
                Text = "Showing 0 of 0 products",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            layout.Controls.Add(_inventoryResultCount, 0, 3);

            // Stock management actions
            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0)
            };

            _inventoryReceive = CreateInventoryButton(
                "Receive Stock", true);

            _inventoryAdjust = CreateInventoryButton(
                "Adjust Stock");

            _inventoryMinimum = CreateInventoryButton(
                "Set Minimum Level");

            _inventoryHistory = CreateInventoryButton(
                "Stock Movement History");

            actionPanel.Controls.Add(_inventoryReceive);
            actionPanel.Controls.Add(_inventoryAddProduct);
            actionPanel.Controls.Add(_inventoryEditProduct);
            actionPanel.Controls.Add(_inventoryAdjust);
            actionPanel.Controls.Add(_inventoryMinimum);
            actionPanel.Controls.Add(_inventoryHistory);

            layout.Controls.Add(actionPanel, 0, 4);

            pnlInventory.Controls.Add(layout);

            _inventorySearch.TextChanged += (_, _) => ApplyInventoryFilter();
            _inventoryFilter.SelectedIndexChanged += (_, _) => ApplyInventoryFilter();
            _inventoryRefresh.Click += (_, _) => LoadInventory();
            _inventoryGrid.SelectionChanged += (_, _) => ApplyInventoryPermissions();
            _inventoryGrid.CellFormatting += InventoryGrid_CellFormatting;

            _inventoryReceive.Click += async (_, _) => await ChangeInventoryStockAsync(false);
            _inventoryAddProduct.Click += (_, _) => ShowProductDialog(productToEdit: null);
            _inventoryEditProduct.Click += (_, _) =>
            {
                var selected = GetSelectedInventoryProduct();
                if (selected != null)
                {
                    ShowProductDialog(productToEdit: selected);
                }
            };
            _inventoryAdjust.Click += async (_, _) => await ChangeInventoryStockAsync(true);
            _inventoryMinimum.Click += (_, _) => UpdateInventoryMinimum();
            _inventoryHistory.Click += (_, _) => ShowInventoryHistory();

            btnProducts.Click += btnProducts_Click;
        }

        private Panel CreateInventoryCard(
            string title,
            out Label valueLabel,
            Color valueColor)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(246, 247, 249),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3)
            };

            var titleLabel = new Label
            {
                Text = title,
                AutoSize = true,
                Location = new Point(10, 8),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(107, 114, 128)
            };

            valueLabel = new Label
            {
                Text = "0",
                AutoSize = true,
                Location = new Point(10, 29),
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = valueColor
            };

            card.Controls.Add(titleLabel);
            card.Controls.Add(valueLabel);

            return card;
        }

        private Button CreateInventoryButton(
            string text,
            bool primary = false)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(100, 30),
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };

            if (primary)
            {
                button.BackColor = Color.FromArgb(37, 99, 235);
                button.ForeColor = Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.UseVisualStyleBackColor = false;
            }

            return button;
        }

        private DataGridView CreateInventoryGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 26 },
                Margin = new Padding(0),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9),
                    SelectionBackColor = Color.FromArgb(37, 99, 235),
                    SelectionForeColor = Color.White
                }
            };
        }

        private void AddInventoryColumn(
            DataGridView grid,
            string propertyName,
            string headerText,
            int fillWeight,
            string? format = null)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = propertyName,
                DataPropertyName = propertyName,
                HeaderText = headerText,
                FillWeight = fillWeight,
                MinimumWidth = 65,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            if (!string.IsNullOrWhiteSpace(format))
            {
                column.DefaultCellStyle.Format = format;
            }

            grid.Columns.Add(column);
        }

        private void LoadInventory()
        {
            if (_inventoryBusy)
                return;

            if (!HasPermission("Inventory.View"))
            {
                ClearInventory();
                ApplyInventoryPermissions();
                return;
            }

            try
            {
                _inventoryRows = _inventoryManagementService.GetInventory();

                UpdateInventorySummary();
                ApplyInventoryFilter();
            }
            catch (Exception ex)
            {
                ClearInventory();
                ShowInventoryError(ex.Message);
            }
            finally
            {
                ApplyInventoryPermissions();
            }
        }

        private void UpdateInventorySummary()
        {
            var summary = InventorySummary.FromRows(_inventoryRows);

            _inventoryTotal.Text = summary.TotalProducts.ToString("N0");

            _inventoryLow.Text = summary.LowStockProductCount.ToString("N0");
            _inventoryLow.ForeColor = summary.LowStockProductCount > 0
                ? Color.DarkOrange : Color.FromArgb(31, 41, 55);

            _inventoryOut.Text = summary.OutOfStockProductCount.ToString("N0");
            _inventoryOut.ForeColor = summary.OutOfStockProductCount > 0
                ? Color.Firebrick : Color.FromArgb(31, 41, 55);

            _inventoryInvalid.Text = summary.InvalidBalanceProductCount.ToString("N0");
            _inventoryInvalid.ForeColor = summary.InvalidBalanceProductCount > 0
                ? Color.Firebrick : Color.FromArgb(31, 41, 55);
        }

        private void ApplyInventoryFilter()
        {
            if (!HasPermission("Inventory.View"))
                return;

            string searchTerm = _inventorySearch.Text.Trim();
            string stockStatus = _inventoryFilter.SelectedItem?.ToString()
                ?? "All Products";

            IEnumerable<InventoryRow> products = _inventoryRows;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                products = products.Where(product =>
                    product.ProductName.Contains(
                        searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    product.Barcode.Contains(
                        searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            products = stockStatus switch
            {
                "In Stock" => products.Where(product =>
                    product.Status == "In Stock"),

                "Low Stock" => products.Where(product =>
                    product.IsLowStock),

                "Out of Stock" => products.Where(product =>
                    product.IsOutOfStock),

                "Invalid Balances" => products.Where(product =>
                    product.HasInvalidBalance),

                _ => products
            };

            var filteredProducts = products.ToList();

            _inventoryGrid.DataSource = filteredProducts;

            _inventoryResultCount.Text =
                $"Showing {filteredProducts.Count:N0} of {_inventoryRows.Count:N0} products";

            ApplyInventoryPermissions();
        }

        private void InventoryGrid_CellFormatting(
            object? sender,
            DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (_inventoryGrid.Columns[e.ColumnIndex].Name != "Status")
                return;

            if (_inventoryGrid.Rows[e.RowIndex].DataBoundItem
                is not InventoryRow product)
            {
                return;
            }

            if (product.HasInvalidBalance || product.IsOutOfStock)
            {
                e.CellStyle.ForeColor = Color.Firebrick;
            }
            else if (product.IsLowStock)
            {
                e.CellStyle.ForeColor = Color.DarkOrange;
            }
            else
            {
                e.CellStyle.ForeColor = Color.DarkGreen;
            }
        }

        private void ApplyInventoryPermissions()
        {
            bool canView = HasPermission("Inventory.View");
            bool canReceive = HasPermission("Inventory.Receive");
            bool canAdjust = HasPermission("Inventory.Adjust");

            bool hasSelection =
                _inventoryGrid.CurrentRow?.DataBoundItem is InventoryRow;

            bool canUseInventory = canView && !_inventoryBusy;

            _inventorySearch.Enabled = canUseInventory;
            _inventoryFilter.Enabled = canUseInventory;
            _inventoryRefresh.Enabled = canUseInventory;
            _inventoryGrid.Enabled = canUseInventory;

            _inventoryAddProduct.Enabled = canUseInventory && canAdjust;

            _inventoryEditProduct.Enabled = canUseInventory && canAdjust && hasSelection;

            _inventoryReceive.Enabled = canUseInventory && canReceive && hasSelection;

            _inventoryAdjust.Enabled = canUseInventory && canAdjust && hasSelection;

            _inventoryMinimum.Enabled = canUseInventory && canAdjust && hasSelection;

            _inventoryHistory.Enabled = canUseInventory && hasSelection;
        }

        private void ShowProductDialog(InventoryRow? productToEdit)
        {
            if (_inventoryBusy)
                return;

            if (!HasPermission("Inventory.View") ||
                !HasPermission("Inventory.Adjust"))
            {
                ShowInventoryError(
                    "You do not have permission to perform this action.");
                return;
            }

            bool isEdit = productToEdit != null;
            string title = isEdit ? "Edit Product" : "Add Product";

            using var dialog = new Form
            {
                Text = title,
                ClientSize = new Size(440, isEdit ? 345 : 395),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            var nameLabel = new Label
            {
                Text = "Product Name",
                Location = new Point(20, 16),
                AutoSize = true
            };

            var nameInput = new TextBox
            {
                Location = new Point(20, 38),
                Width = 400,
                MaxLength = 150,
                Text = isEdit ? productToEdit!.ProductName : string.Empty
            };

            var barcodeLabel = new Label
            {
                Text = "Barcode",
                Location = new Point(20, 72),
                AutoSize = true
            };

            var barcodeInput = new TextBox
            {
                Location = new Point(20, 94),
                Width = 400,
                MaxLength = 100,
                Text = isEdit ? productToEdit!.Barcode : string.Empty
            };

            var priceLabel = new Label
            {
                Text = $"Selling Price ({GetCurrencyPrefix()})",
                Location = new Point(20, 128),
                AutoSize = true
            };

            var priceInput = new NumericUpDown
            {
                Location = new Point(20, 150),
                Width = 190,
                DecimalPlaces = 2,
                Minimum = 0m,
                Maximum = 100000000m,
                ThousandsSeparator = true,
                Value = isEdit
                    ? Math.Clamp(productToEdit!.SellingPrice, 0m, 100000000m)
                    : 0m
            };

            var unitsPerCaseLabel = new Label
            {
                Text = "Units Per Case",
                Location = new Point(230, 128),
                AutoSize = true
            };

            var unitsPerCaseInput = new NumericUpDown
            {
                Location = new Point(230, 150),
                Width = 190,
                Minimum = 1,
                Maximum = 1000000,
                Value = isEdit
                    ? Math.Clamp(productToEdit!.UnitsPerCase, 1, 1000000)
                    : 1
            };

            var minStockLabel = new Label
            {
                Text = "Minimum Stock Level",
                Location = new Point(20, 186),
                AutoSize = true
            };

            int defaultMinStock = 0;
            try
            {
                defaultMinStock = Math.Max(0, _businessSettingsService.GetSettings().DefaultMinimumStockLevel);
            }
            catch { }

            var minStockInput = new NumericUpDown
            {
                Location = new Point(20, 208),
                Width = isEdit ? 400 : 190,
                Minimum = 0,
                Maximum = int.MaxValue,
                ThousandsSeparator = true,
                Value = isEdit ? Math.Max(0, productToEdit!.MinimumStockLevel) : defaultMinStock
            };

            NumericUpDown? initialStockInput = null;
            if (!isEdit)
            {
                var initialStockLabel = new Label
                {
                    Text = "Initial Stock (units)",
                    Location = new Point(230, 186),
                    AutoSize = true
                };

                initialStockInput = new NumericUpDown
                {
                    Location = new Point(230, 208),
                    Width = 190,
                    Minimum = 0,
                    Maximum = int.MaxValue,
                    ThousandsSeparator = true,
                    Value = 0
                };

                dialog.Controls.Add(initialStockLabel);
                dialog.Controls.Add(initialStockInput);
            }

            int buttonsY = isEdit ? 285 : 335;

            var saveButton = CreateInventoryButton("Save", true);
            saveButton.Location = new Point(210, buttonsY);

            var cancelButton = CreateInventoryButton("Cancel");
            cancelButton.Location = new Point(320, buttonsY);
            cancelButton.DialogResult = DialogResult.Cancel;

            saveButton.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(nameInput.Text))
                {
                    MessageBox.Show(
                        dialog,
                        "Product name is required.",
                        title,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    nameInput.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(barcodeInput.Text))
                {
                    MessageBox.Show(
                        dialog,
                        "Barcode is required.",
                        title,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    barcodeInput.Focus();
                    return;
                }

                try
                {
                    if (isEdit)
                    {
                        _inventoryManagementService.UpdateProduct(
                            productToEdit!.ProductId,
                            nameInput.Text,
                            barcodeInput.Text,
                            priceInput.Value,
                            (int)unitsPerCaseInput.Value,
                            (int)minStockInput.Value);
                    }
                    else
                    {
                        _inventoryManagementService.AddProduct(
                            nameInput.Text,
                            barcodeInput.Text,
                            priceInput.Value,
                            (int)unitsPerCaseInput.Value,
                            (int)minStockInput.Value,
                            (int)(initialStockInput?.Value ?? 0));
                    }

                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    ShowInventoryError(ex.Message);
                }
            };

            dialog.Controls.AddRange(new Control[]
            {
                nameLabel,
                nameInput,
                barcodeLabel,
                barcodeInput,
                priceLabel,
                priceInput,
                unitsPerCaseLabel,
                unitsPerCaseInput,
                minStockLabel,
                minStockInput,
                saveButton,
                cancelButton
            });

            dialog.AcceptButton = saveButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadInventory();
                MessageBox.Show(
                    this,
                    isEdit
                        ? "Product updated successfully."
                        : "Product added successfully.",
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private InventoryRow? GetSelectedInventoryProduct()
        {
            if (_inventoryGrid.CurrentRow?.DataBoundItem
                is InventoryRow product)
            {
                return product;
            }

            MessageBox.Show(
                this,
                "Please select a product first.",
                "Inventory",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return null;
        }

        private void ClearInventory()
        {
            _inventoryRows.Clear();
            _inventoryGrid.DataSource = null;

            UpdateInventorySummary();

            _inventoryResultCount.Text = "Showing 0 of 0 products";
        }

        private void ShowInventoryError(string message)
        {
            MessageBox.Show(
                this,
                message,
                "Inventory",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private async Task ChangeInventoryStockAsync(bool isAdjustment)
        {
            if (_inventoryBusy)
                return;

            string permission = isAdjustment
                ? "Inventory.Adjust"
                : "Inventory.Receive";

            string dialogTitle = isAdjustment
                ? "Adjust Stock"
                : "Receive Stock";

            if (!HasPermission("Inventory.View") ||
                !HasPermission(permission))
            {
                ShowInventoryError(
                    "You do not have permission to perform this action.");

                return;
            }

            var product = GetSelectedInventoryProduct();

            if (product == null)
                return;

            using var dialog = new Form
            {
                Text = dialogTitle,
                ClientSize = new Size(440, 320),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            var productLabel = new Label
            {
                Text = $"Product: {product.ProductName}",
                Location = new Point(20, 18),
                Size = new Size(400, 24),
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            var stockLabel = new Label
            {
                Text = $"Current available stock: {product.AvailableStock:N0}",
                Location = new Point(20, 48),
                AutoSize = true
            };

            var quantityLabel = new Label
            {
                Text = isAdjustment
                    ? "Adjustment Quantity (units)"
                    : "Received Quantity (units)",
                Location = new Point(20, 80),
                AutoSize = true
            };

            var quantityInput = new NumericUpDown
            {
                Location = new Point(20, 103),
                Width = 400,
                Minimum = isAdjustment ? int.MinValue : 1,
                Maximum = int.MaxValue,
                Value = isAdjustment ? 0 : 1,
                ThousandsSeparator = true
            };

            var instructionLabel = new Label
            {
                Text = isAdjustment
                    ? "Use a positive quantity to add stock or a negative quantity to remove stock."
                    : "Enter the number of individual units received.",
                Location = new Point(20, 133),
                Size = new Size(400, 40),
                ForeColor = Color.FromArgb(107, 114, 128)
            };

            var notesLabel = new Label
            {
                Text = isAdjustment
                    ? "Reason (required)"
                    : "Notes (optional)",
                Location = new Point(20, 177),
                AutoSize = true
            };

            var notesInput = new TextBox
            {
                Location = new Point(20, 200),
                Size = new Size(400, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var saveButton = CreateInventoryButton(
                isAdjustment ? "Adjust Stock" : "Receive Stock",
                true);

            saveButton.Location = new Point(190, 278);

            var cancelButton = CreateInventoryButton("Cancel");

            cancelButton.Location = new Point(320, 278);
            cancelButton.DialogResult = DialogResult.Cancel;

            saveButton.Click += (_, _) =>
            {
                if (isAdjustment && quantityInput.Value == 0)
                {
                    MessageBox.Show(
                        dialog,
                        "Adjustment quantity cannot be zero.",
                        dialogTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    quantityInput.Focus();
                    return;
                }

                if (isAdjustment &&
                    string.IsNullOrWhiteSpace(notesInput.Text))
                {
                    MessageBox.Show(
                        dialog,
                        "Please enter a reason for the adjustment.",
                        dialogTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    notesInput.Focus();
                    return;
                }

                dialog.DialogResult = DialogResult.OK;
            };

            dialog.Controls.AddRange(new Control[]
            {
                productLabel,
                stockLabel,
                quantityLabel,
                quantityInput,
                instructionLabel,
                notesLabel,
                notesInput,
                saveButton,
                cancelButton
            });

            dialog.AcceptButton = saveButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            int quantity = (int)quantityInput.Value;
            string notes = notesInput.Text.Trim();

            bool success = false;

            _inventoryBusy = true;
            ApplyInventoryPermissions();
            Enabled = false;

            try
            {
                if (isAdjustment)
                {
                    await _inventoryManagementService.AdjustStockAsync(
                        product.ProductId,
                        quantity,
                        notes);
                }
                else
                {
                    await _inventoryManagementService.ReceiveStockAsync(
                        product.ProductId,
                        quantity,
                        notes);
                }

                success = true;
            }
            catch (Exception ex)
            {
                ShowInventoryError(ex.Message);
            }
            finally
            {
                Enabled = true;
                _inventoryBusy = false;

                ApplyInventoryPermissions();
                LoadInventory();
            }

            if (success)
            {
                MessageBox.Show(
                    this,
                    isAdjustment
                        ? "Stock adjusted successfully."
                        : "Stock received successfully.",
                    dialogTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void UpdateInventoryMinimum()
        {
            if (_inventoryBusy)
                return;

            if (!HasPermission("Inventory.View") ||
                !HasPermission("Inventory.Adjust"))
            {
                ShowInventoryError(
                    "You do not have permission to perform this action.");

                return;
            }

            var product = GetSelectedInventoryProduct();

            if (product == null)
                return;

            using var dialog = new Form
            {
                Text = "Set Minimum Stock Level",
                ClientSize = new Size(400, 235),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            var productLabel = new Label
            {
                Text = $"Product: {product.ProductName}",
                Location = new Point(20, 18),
                Size = new Size(360, 24),
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            var currentMinimumLabel = new Label
            {
                Text = $"Current minimum level: {product.MinimumStockLevel:N0}",
                Location = new Point(20, 48),
                AutoSize = true
            };

            var minimumLabel = new Label
            {
                Text = "New Minimum Stock Level (units)",
                Location = new Point(20, 80),
                AutoSize = true
            };

            var minimumInput = new NumericUpDown
            {
                Location = new Point(20, 103),
                Width = 360,
                Minimum = 0,
                Maximum = int.MaxValue,
                Value = Math.Max(0, product.MinimumStockLevel),
                ThousandsSeparator = true
            };

            var instructionLabel = new Label
            {
                Text = "Products at or below this level will be identified as low stock.",
                Location = new Point(20, 135),
                Size = new Size(360, 40),
                ForeColor = Color.FromArgb(107, 114, 128)
            };

            var saveButton = CreateInventoryButton("Save", true);

            saveButton.Location = new Point(170, 190);
            saveButton.DialogResult = DialogResult.OK;

            var cancelButton = CreateInventoryButton("Cancel");

            cancelButton.Location = new Point(280, 190);
            cancelButton.DialogResult = DialogResult.Cancel;

            dialog.Controls.AddRange(new Control[]
            {
                productLabel,
                currentMinimumLabel,
                minimumLabel,
                minimumInput,
                instructionLabel,
                saveButton,
                cancelButton
            });

            dialog.AcceptButton = saveButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            int minimumStockLevel = (int)minimumInput.Value;

            if (minimumStockLevel == product.MinimumStockLevel)
                return;

            try
            {
                _inventoryManagementService.SetMinimumStockLevel(
                    product.ProductId,
                    minimumStockLevel);

                MessageBox.Show(
                    this,
                    "Minimum stock level updated successfully.",
                    "Inventory",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowInventoryError(ex.Message);
            }
            finally
            {
                LoadInventory();
            }
        }

        private void ShowInventoryHistory()
        {
            if (_inventoryBusy)
                return;

            if (!HasPermission("Inventory.View"))
            {
                ShowInventoryError(
                    "You do not have permission to view inventory history.");

                return;
            }

            var product = GetSelectedInventoryProduct();

            if (product == null)
                return;

            try
            {
                var movements =
                    _inventoryManagementService.GetStockMovementHistory(
                        product.ProductId);

                using var dialog = new Form
                {
                    Text = "Stock Movement History",
                    ClientSize = new Size(1000, 500),
                    MinimumSize = new Size(750, 350),
                    StartPosition = FormStartPosition.CenterParent,
                    ShowInTaskbar = false,
                    BackColor = Color.White,
                    Font = new Font("Segoe UI", 9)
                };

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 4,
                    Padding = new Padding(15)
                };

                layout.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 100));

                layout.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 38));

                layout.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 30));

                layout.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100));

                layout.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 45));

                var titleLabel = new Label
                {
                    Text = $"Stock Movement History - {product.ProductName}",
                    Dock = DockStyle.Fill,
                    AutoEllipsis = true,
                    Font = new Font("Segoe UI", 14, FontStyle.Bold)
                };

                var detailsLabel = new Label
                {
                    Text = $"Barcode: {product.Barcode}    |    " +
                           $"Available stock: {product.AvailableStock:N0}    |    " +
                           $"Total movements: {movements.Count:N0}",
                    Dock = DockStyle.Fill,
                    ForeColor = Color.FromArgb(107, 114, 128),
                    AutoEllipsis = true
                };

                var historyGrid = CreateInventoryGrid();

                AddInventoryColumn(
                    historyGrid, "RecordedAt", "Date and Time", 140);

                AddInventoryColumn(
                    historyGrid, "ProductName", "Product", 100);

                AddInventoryColumn(
                    historyGrid, "MovementType", "Movement Type", 120);

                AddInventoryColumn(
                    historyGrid, "QuantityChange", "Quantity", 65,
                    "+#,##0;-#,##0;0");

                AddInventoryColumn(
                    historyGrid, "Username", "Recorded By", 100);

                AddInventoryColumn(
                    historyGrid, "Notes", "Notes", 180);

                historyGrid.CellFormatting += (_, e) =>
                {
                    if (e.RowIndex < 0 || e.ColumnIndex < 0)
                        return;

                    string columnName =
                        historyGrid.Columns[e.ColumnIndex].Name;

                    if (columnName == "RecordedAt" &&
                        e.Value is DateTime recordedAt)
                    {
                        DateTime localTime = DateTime.SpecifyKind(
                            recordedAt, DateTimeKind.Utc).ToLocalTime();

                        e.Value = localTime.ToString(
                            "dd MMM yyyy, hh:mm tt");

                        e.FormattingApplied = true;
                    }

                    if (columnName == "QuantityChange" && e.Value is int quantity)
                    {
                        if (quantity > 0)
                        {
                            e.CellStyle.ForeColor = Color.DarkGreen;
                        }
                        else if (quantity < 0)
                        {
                            e.CellStyle.ForeColor = Color.Firebrick;
                        }
                        else
                        {
                            e.CellStyle.ForeColor = Color.Black;
                        }
                    }

                    if (columnName == "MovementType" &&
                        e.Value is string movementType)
                    {
                        e.Value = movementType switch
                        {
                            "StockReceived" => "Stock Received",
                            "StockAdjustment" => "Stock Adjustment",
                            "RegularSale" => "Regular Sale",
                            "CreditSale" => "Credit Sale",
                            "ConsignmentIssued" => "Consignment Issued",
                            "ConsignmentReturn" => "Consignment Return",
                            _ => movementType
                        };

                        e.FormattingApplied = true;
                    }
                };

                historyGrid.DataSource = movements;

                var footerPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false,
                    Padding = new Padding(0, 8, 0, 0)
                };

                var closeButton = CreateInventoryButton("Close");

                closeButton.DialogResult = DialogResult.OK;

                footerPanel.Controls.Add(closeButton);

                if (movements.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = "No stock movements recorded for this product.",
                        AutoSize = true,
                        Margin = new Padding(10, 8, 10, 0)
                    };

                    footerPanel.Controls.Add(emptyLabel);
                }

                layout.Controls.Add(titleLabel, 0, 0);
                layout.Controls.Add(detailsLabel, 0, 1);
                layout.Controls.Add(historyGrid, 0, 2);
                layout.Controls.Add(footerPanel, 0, 3);

                dialog.Controls.Add(layout);
                dialog.CancelButton = closeButton;

                dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ShowInventoryError(ex.Message);
            }
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
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(
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
                visibleCards.Add(pnlActiveCustomers);

            if (canSeeProducts)
                visibleCards.Add(pnlTotalProducts);

            if (canSeeCredit)
            {
                visibleCards.Add(_pnlOutstandingCreditCard);
                visibleCards.Add(_pnlOverdueCreditCard);
            }

            if (canSeeConsignment)
            {
                visibleCards.Add(pnlPendingConsignments);
                visibleCards.Add(_pnlOutstandingConsignmentCard);
            }

            if (canSeeInventory)
            {
                visibleCards.Add(_pnlLowStockCard);
                visibleCards.Add(_pnlOutOfStockCard);
            }

            tlpDashboardCards.SuspendLayout();
            tlpDashboardCards.Controls.Clear();
            tlpDashboardCards.ColumnStyles.Clear();
            tlpDashboardCards.RowStyles.Clear();

            if (visibleCards.Count == 0)
            {
                tlpDashboardCards.ColumnCount = 1;
                tlpDashboardCards.RowCount = 1;
                tlpDashboardCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                tlpDashboardCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                tlpDashboardCards.ResumeLayout();
                return;
            }

            int columns = Math.Min(3, visibleCards.Count);
            int rows = (int)Math.Ceiling(visibleCards.Count / (double)columns);

            tlpDashboardCards.ColumnCount = columns;
            tlpDashboardCards.RowCount = rows;

            for (int c = 0; c < columns; c++)
            {
                tlpDashboardCards.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 100F / columns));
            }

            for (int r = 0; r < rows; r++)
            {
                tlpDashboardCards.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100F / rows));
            }

            for (int i = 0; i < visibleCards.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                tlpDashboardCards.Controls.Add(visibleCards[i], col, row);
            }

            tlpDashboardCards.ResumeLayout();
        }

        private void ConfigureSettingsLayout()
        {
            pnlSettings.Controls.Clear();

            pnlSettings.AutoScroll = true;
            pnlSettings.BackColor = Color.FromArgb(248, 250, 252);
            pnlSettings.Padding = new Padding(20);

            var mainLayout = new TableLayoutPanel
            {
                Location = new Point(20, 20),
                Width = Math.Max(400, pnlSettings.ClientSize.Width - 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(4),
                BackColor = Color.Transparent
            };

            mainLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));

            // Keep width synced when pnlSettings resizes
            pnlSettings.SizeChanged += (_, _) =>
            {
                int scrollbarWidth = pnlSettings.VerticalScroll.Visible
                    ? SystemInformation.VerticalScrollBarWidth
                    : 0;
                mainLayout.Width = Math.Max(
                    400,
                    pnlSettings.ClientSize.Width - pnlSettings.Padding.Horizontal - scrollbarWidth - 8);
            };

            // Business Information
            var businessGroup = CreateSettingsGroup("Business Information");

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

            chkShowBusinessContactDetails = new CheckBox
            {
                Text = "Show business contact details on receipts",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 8, 3, 8)
            };

            var chkLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 1,
                Margin = new Padding(3, 5, 3, 5)
            };
            chkLayout.Controls.Add(chkShowBusinessContactDetails, 0, 0);

            // Add in reverse visual order because DockStyle.Top stacks bottom-up
            receiptGroup.Controls.Add(chkLayout);
            AddSettingsField(receiptGroup, "Receipt Footer", txtReceiptFooter);
            AddSettingsField(receiptGroup, "Receipt Header", txtReceiptHeader);

            // System Preferences
            var systemGroup = CreateSettingsGroup("System Preferences");

            cmbCurrencyCode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Width = 250
            };
            cmbCurrencyCode.Items.AddRange(new object[] { "JMD", "USD" });

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
            var inventoryGroup = CreateSettingsGroup("Inventory Preferences");

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

            // Add sections to rows explicitly
            mainLayout.Controls.Add(businessGroup, 0, 0);
            mainLayout.Controls.Add(receiptGroup, 0, 1);
            mainLayout.Controls.Add(systemGroup, 0, 2);
            mainLayout.Controls.Add(inventoryGroup, 0, 3);
            mainLayout.Controls.Add(btnSaveSettings, 0, 4);

            pnlSettings.Controls.Add(mainLayout);
        }

        private GroupBox CreateSettingsGroup(string title)
        {
            return new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Padding = new Padding(15),
                Margin = new Padding(0, 0, 0, 15)
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
            GroupBox group,
            string labelText,
            Control input)
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
                BackColor = Color.FromArgb(249, 250, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(8)
            };

            Label titleLabel = new()
            {
                AutoSize = true,
                Font = new Font(
                    "Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(15, 15),
                Text = title
            };

            valueLabel = new Label
            {
                AutoSize = true,
                Font = new Font(
                    "Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                Location = new Point(15, 55),
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
            cmbReportType.Items.Add("Credit Report");
            cmbReportType.Items.Add("Consignment Report");
            cmbReportType.Items.Add("Credit Statement");
            cmbReportType.Items.Add("Consignment Statement");
            cmbReportType.Items.Add("Inventory Report");

            cmbReportType.SelectedIndex = 0;

            dtpStartDate.Value = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

            dtpEndDate.Value = DateTime.Today;

            string dateFormat = GetConfiguredDateFormat();
            dtpStartDate.Format = DateTimePickerFormat.Custom;
            dtpStartDate.CustomFormat = dateFormat;
            dtpEndDate.Format = DateTimePickerFormat.Custom;
            dtpEndDate.CustomFormat = dateFormat;

            // Fit long report summaries cleanly across the width at a smaller font
            lblReportSummary.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblReportSummary.AutoSize = false;
            if (lblReportSummary.Parent != null)
            {
                lblReportSummary.Width = Math.Max(300, lblReportSummary.Parent.ClientSize.Width - lblReportSummary.Left - 20);
                lblReportSummary.Height = 24;
                lblReportSummary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }

            lblStatementReference = new Label
            {
                Text = "Reference:",
                AutoSize = true,
                Font = lblCustomer.Font,
                ForeColor = lblCustomer.ForeColor,
                Margin = new Padding(12, 8, 4, 0),
                Visible = false
            };

            cmbStatementReference = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = cmbCustomer.Font,
                Width = 190,
                Visible = false
            };

            if (cmbCustomer.Parent != null)
            {
                cmbCustomer.Parent.Controls.Add(lblStatementReference);
                cmbCustomer.Parent.Controls.Add(cmbStatementReference);

                if (cmbCustomer.Parent is not FlowLayoutPanel)
                {
                    lblStatementReference.Location = new Point(
                        cmbCustomer.Right + 15,
                        lblCustomer.Top);

                    cmbStatementReference.Location = new Point(
                        lblStatementReference.Right + 6,
                        cmbCustomer.Top);
                }
            }

            cmbCustomer.SelectedIndexChanged += (_, _) => PopulateStatementReferences();
            cmbStatementReference.SelectedIndexChanged += (_, _) => DisableReportExports();

            btnGenerateReport.Click += btnGenerateReport_Click;
        }

        private void DisableReportExports()
        {
            btnExportPdf.Enabled = false;
            btnExportExcel.Enabled = false;
            btnExportWord.Enabled = false;
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
            object? sender, EventArgs e)
        {
            DisableReportExports();

            bool requiresCustomer =
                cmbReportType.SelectedIndex == 3 ||
                cmbReportType.SelectedIndex == 4;

            bool usesDateRange = cmbReportType.SelectedIndex != 5;

            lblCustomer.Visible = requiresCustomer;
            cmbCustomer.Visible = requiresCustomer;
            lblStatementReference.Visible = requiresCustomer;
            cmbStatementReference.Visible = requiresCustomer;

            dtpStartDate.Enabled = usesDateRange;
            dtpEndDate.Enabled = usesDateRange;

            if (!requiresCustomer)
            {
                cmbCustomer.SelectedIndex = -1;
                cmbStatementReference.Items.Clear();
            }
            else
            {
                PopulateStatementReferences();
            }
        }

        private void PopulateStatementReferences()
        {
            if (cmbStatementReference == null)
                return;

            cmbStatementReference.Items.Clear();
            cmbStatementReference.Items.Add("All in Date Range");

            if (cmbCustomer.SelectedValue is not int customerId)
            {
                cmbStatementReference.SelectedIndex = 0;
                return;
            }

            DateTime startDate = dtpStartDate.Value.Date;
            DateTime endDate = dtpEndDate.Value.Date;
            if (startDate > endDate)
            {
                cmbStatementReference.SelectedIndex = 0;
                return;
            }

            try
            {
                if (cmbReportType.SelectedIndex == 3)
                {
                    var statement = _reportingService.GetCreditStatement(
                        customerId, startDate, endDate);

                    var refs = statement.Transactions
                        .Select(t => t.ReferenceNumber)
                        .Distinct()
                        .Reverse();

                    foreach (var reference in refs)
                        cmbStatementReference.Items.Add(reference);
                }
                else if (cmbReportType.SelectedIndex == 4)
                {
                    var statement = _reportingService.GetConsignmentStatement(
                        customerId, startDate, endDate);

                    var refs = statement.Transactions
                        .Select(t => t.ReferenceNumber)
                        .Distinct()
                        .Reverse();

                    foreach (var reference in refs)
                        cmbStatementReference.Items.Add(reference);
                }
            }
            catch
            {
                // Keep "All in Date Range" if lookup fails
            }

            cmbStatementReference.SelectedIndex =
                cmbStatementReference.Items.Count > 1 ? 1 : 0;
        }

        private void dtpStartDate_ValueChanged(
            object? sender, EventArgs e)
        {
            DisableReportExports();

            if (dtpStartDate.Value.Date > dtpEndDate.Value.Date)
            {
                dtpEndDate.Value = dtpStartDate.Value.Date;
            }

            PopulateStatementReferences();
        }

        private void dtpEndDate_ValueChanged(
            object? sender, EventArgs e)
        {
            DisableReportExports();

            if (dtpEndDate.Value.Date < dtpStartDate.Value.Date)
            {
                dtpStartDate.Value = dtpEndDate.Value.Date;
            }

            PopulateStatementReferences();
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

        private void ConfigureCreditManagementLayout()
        {
            // Navigation button
            _btnCreditManagement = new Button
            {
                Text = "Credit Management",
                Name = "btnCreditManagement",
                Size = new Size(190, 42),
                Margin = new Padding(0, 5, 0, 5),
                BackColor = _normalButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Visible = false
            };

            _btnCreditManagement.FlatAppearance.BorderSize = 1;
            _btnCreditManagement.FlatAppearance.BorderColor = Color.White;

            flpNavigation.Controls.Add(_btnCreditManagement);
            flpNavigation.Controls.SetChildIndex(_btnCreditManagement, 2);

            // Main Credit Management page
            _pnlCreditManagement = new Panel
            {
                Name = "pnlCreditManagement",
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Bottom |
                         AnchorStyles.Left |
                         AnchorStyles.Right,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(20, 110),
                Size = new Size(1315, 597),
                Visible = false
            };

            pnlContent.Controls.Add(_pnlCreditManagement);

            // Main layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20),
                BackColor = Color.White
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Heading
            var headingLabel = new Label
            {
                Text = "Customer Credit Accounts",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(5, 5)
            };

            var headingPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            headingPanel.Controls.Add(headingLabel);

            // Customer search panel
            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 8, 0, 0)
            };

            var searchLabel = new Label
            {
                Text = "Search Customer:",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 8, 8, 3)
            };

            _txtCreditCustomerSearch = new TextBox
            {
                Name = "txtCreditCustomerSearch",
                PlaceholderText = "Type name or phone...",
                Width = 180,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 3, 8, 3)
            };

            _cmbCreditCustomer = new ComboBox
            {
                Name = "cmbCreditCustomer",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 230,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 3, 10, 3)
            };

            _btnSearchCreditAccounts = new Button
            {
                Name = "btnSearchCreditAccounts",
                Text = "Refresh",
                Size = new Size(95, 30),
                Font = new Font("Segoe UI", 9F),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(3)
            };

            _btnSearchCreditAccounts.FlatAppearance.BorderSize = 0;

            _btnViewCreditPayments = new Button
            {
                Name = "btnViewCreditPayments",
                Text = "View Payment History",
                Size = new Size(160, 30),
                Font = new Font("Segoe UI", 9F),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(3)
            };

            _btnRecordCreditPayment = new Button
            {
                Name = "btnRecordCreditPayment",
                Text = "Record Payment",
                Size = new Size(130, 30),
                Font = new Font("Segoe UI", 9F),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(3)
            };

            _btnRecordCreditPayment.FlatAppearance.BorderSize = 0;
            _btnViewCreditPayments.FlatAppearance.BorderSize = 0;

            _btnIssueCreditSale = new Button
            {
                Name = "btnIssueCreditSale",
                Text = "Issue Credit Sale",
                Size = new Size(140, 30),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(3)
            };
            _btnIssueCreditSale.FlatAppearance.BorderSize = 0;
            _btnIssueCreditSale.Click += (_, _) => ShowIssueCreditSaleDialog();

            searchPanel.Controls.Add(searchLabel);
            searchPanel.Controls.Add(_txtCreditCustomerSearch);
            searchPanel.Controls.Add(_cmbCreditCustomer);
            searchPanel.Controls.Add(_btnSearchCreditAccounts);
            searchPanel.Controls.Add(_btnViewCreditPayments);
            searchPanel.Controls.Add(_btnRecordCreditPayment);
            searchPanel.Controls.Add(_btnIssueCreditSale);

            // Credit accounts grid
            _creditAccountsGrid = new DataGridView
            {
                Name = "dgvCreditAccounts",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                AutoGenerateColumns = false
            };

            _creditAccountsGrid.Columns.Add("Id", "ID");
            _creditAccountsGrid.Columns.Add("ReferenceNumber", "Reference");
            _creditAccountsGrid.Columns.Add("IssuedAt", "Date Issued");
            _creditAccountsGrid.Columns.Add("DueDate", "Due Date");
            _creditAccountsGrid.Columns.Add("PurchaseAmount", "Purchase Amount");
            _creditAccountsGrid.Columns.Add("CreditFee", "Credit Fee");
            _creditAccountsGrid.Columns.Add("TotalAmount", "Total Amount");
            _creditAccountsGrid.Columns.Add("AmountPaid", "Paid");
            _creditAccountsGrid.Columns.Add("OutstandingBalance", "Outstanding");
            _creditAccountsGrid.Columns.Add("Status", "Status");

            _creditAccountsGrid.Columns["Id"]!.Visible = false;

            _creditAccountsGrid.Columns["Id"]!.DataPropertyName = "Id";
            _creditAccountsGrid.Columns["ReferenceNumber"]!.DataPropertyName = "ReferenceNumber";
            _creditAccountsGrid.Columns["IssuedAt"]!.DataPropertyName = "IssuedAt";
            _creditAccountsGrid.Columns["DueDate"]!.DataPropertyName = "DueDate";
            _creditAccountsGrid.Columns["PurchaseAmount"]!.DataPropertyName = "PurchaseAmount";
            _creditAccountsGrid.Columns["CreditFee"]!.DataPropertyName = "CreditFee";
            _creditAccountsGrid.Columns["TotalAmount"]!.DataPropertyName = "TotalAmount";

            _creditAccountsGrid.Columns["IssuedAt"]!.DefaultCellStyle.Format = "dd MMM yyyy";
            _creditAccountsGrid.Columns["DueDate"]!.DefaultCellStyle.Format = "dd MMM yyyy";
            _creditAccountsGrid.Columns["PurchaseAmount"]!.DefaultCellStyle.Format = "N2";
            _creditAccountsGrid.Columns["CreditFee"]!.DefaultCellStyle.Format = "N2";
            _creditAccountsGrid.Columns["TotalAmount"]!.DefaultCellStyle.Format = "N2";
            _creditAccountsGrid.Columns["AmountPaid"]!.DefaultCellStyle.Format = "N2";
            _creditAccountsGrid.Columns["OutstandingBalance"]!.DefaultCellStyle.Format = "N2";

            foreach (DataGridViewColumn column in _creditAccountsGrid.Columns)
                column.SortMode = DataGridViewColumnSortMode.Automatic;

            layout.Controls.Add(headingPanel, 0, 0);
            layout.Controls.Add(searchPanel, 0, 1);
            layout.Controls.Add(_creditAccountsGrid, 0, 2);

            _pnlCreditManagement.Controls.Add(layout);
        }

        private void ShowIssueCreditSaleDialog()
        {
            if (!RequirePermission("Credit.Manage") || _userSession.CurrentUser == null)
                return;

            var customers = _customerRepository.GetAll().Where(c => c.IsActive).ToList();
            if (customers.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "No active customers found. Add a customer first.",
                    "Issue Credit Sale",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var products = _inventoryManagementService.GetInventory()
                .Where(p => p.AvailableStock > 0)
                .ToList();
            if (products.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "No products with available stock found.",
                    "Issue Credit Sale",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new Form
            {
                Text = "Issue Credit Sale",
                ClientSize = new Size(560, 440),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F)
            };

            var cmbCustomer = new ComboBox
            {
                Location = new Point(130, 18),
                Width = 405,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(Customer.Name),
                ValueMember = nameof(Customer.Id),
                DataSource = customers
            };

            var lblTerms = new Label
            {
                Text = $"Terms: {BusinessRules.CreditRepaymentDays} days ({BusinessRules.StandardFeeRate:P0} fee)",
                Location = new Point(130, 60),
                AutoSize = true,
                ForeColor = Color.FromArgb(75, 85, 99)
            };

            var cmbProduct = new ComboBox
            {
                Location = new Point(130, 98),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(InventoryRow.ProductName),
                ValueMember = nameof(InventoryRow.ProductId),
                DataSource = products
            };

            var nudQty = new NumericUpDown
            {
                Location = new Point(390, 98),
                Width = 70,
                Minimum = 1,
                Maximum = 100000,
                Value = 1
            };

            var btnAddLine = new Button
            {
                Text = "Add",
                Location = new Point(468, 96),
                Size = new Size(67, 30),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddLine.FlatAppearance.BorderSize = 0;

            var grid = new DataGridView
            {
                Location = new Point(20, 138),
                Size = new Size(515, 200),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("ProductId", "ID");
            grid.Columns["ProductId"]!.Visible = false;
            grid.Columns.Add("ProductName", "Product");
            grid.Columns.Add("Quantity", "Qty");
            grid.Columns.Add("UnitPrice", "Unit Price");
            grid.Columns.Add("LineTotal", "Line Total");

            var cartLines = new List<SaleRequestLine>();

            void RefreshLinesGrid()
            {
                grid.Rows.Clear();
                foreach (var group in cartLines.GroupBy(x => x.ProductId))
                {
                    var prod = products.FirstOrDefault(p => p.ProductId == group.Key);
                    if (prod == null) continue;
                    int qty = group.Sum(x => x.Quantity);
                    decimal lineTotal = Math.Round(prod.SellingPrice * qty, 2, MidpointRounding.AwayFromZero);
                    grid.Rows.Add(prod.ProductId, prod.ProductName, qty, prod.SellingPrice.ToString("N2"), lineTotal.ToString("N2"));
                }
            }

            btnAddLine.Click += (_, _) =>
            {
                if (cmbProduct.SelectedItem is not InventoryRow selectedProd)
                    return;

                int qty = (int)nudQty.Value;
                int existingQty = cartLines.Where(x => x.ProductId == selectedProd.ProductId).Sum(x => x.Quantity);
                if (existingQty + qty > selectedProd.AvailableStock)
                {
                    MessageBox.Show(
                        dialog,
                        $"Only {selectedProd.AvailableStock} units available for {selectedProd.ProductName}.",
                        "Stock Limit",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                cartLines.Add(new SaleRequestLine(selectedProd.ProductId, qty));
                RefreshLinesGrid();
                nudQty.Value = 1;
            };

            var btnSave = new Button
            {
                Text = "Create Credit Sale",
                Location = new Point(285, 360),
                Size = new Size(145, 36),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(438, 360),
                Size = new Size(97, 36),
                DialogResult = DialogResult.Cancel
            };

            btnSave.Click += (_, _) =>
            {
                if (cmbCustomer.SelectedValue is not int customerId || customerId <= 0)
                {
                    MessageBox.Show(dialog, "Please select a customer.", "Issue Credit Sale", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cartLines.Count == 0)
                {
                    MessageBox.Show(dialog, "Please add at least one product.", "Issue Credit Sale", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    _creditService.CreateCreditSale(customerId, cartLines);

                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(dialog, ex.Message, "Issue Credit Sale", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            dialog.Controls.AddRange(new Control[]
            {
                new Label { Text = "Customer:", Location = new Point(20, 22), AutoSize = true },
                cmbCustomer,
                new Label { Text = "Credit Terms:", Location = new Point(20, 60), AutoSize = true },
                lblTerms,
                new Label { Text = "Product:", Location = new Point(20, 102), AutoSize = true },
                cmbProduct,
                nudQty,
                btnAddLine,
                grid,
                btnSave,
                btnCancel
            });

            dialog.AcceptButton = btnSave;
            dialog.CancelButton = btnCancel;

            if (dialog.ShowDialog(this) == DialogResult.OK &&
                cmbCustomer.SelectedValue is int custId)
            {
                _cmbCreditCustomer.SelectedValue = custId;
                LoadCustomerCreditAccounts(custId);
                MessageBox.Show(
                    this,
                    "Credit sale issued successfully.",
                    "Issue Credit Sale",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private string GetConfiguredBusinessName()
        {
            try
            {
                string name = _businessSettingsService.GetSettings().BusinessName;
                return string.IsNullOrWhiteSpace(name) ? "K&K WHOLESALE" : name.Trim();
            }
            catch
            {
                return "K&K WHOLESALE";
            }
        }

        private void ConfigureConsignmentReturnsTab()
        {
            tabConsignmentReturns.Controls.Clear();

            string currency = GetCurrencyPrefix();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15),
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 185F));

            // Row 0: Search by Reference # OR Customer + Consignment dropdown
            var searchPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(249, 250, 251),
                Padding = new Padding(10)
            };

            var refRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                WrapContents = false
            };
            _txtConsignmentRef = new TextBox { Width = 190 };
            _btnFindConsignment = new Button
            {
                Text = "Find",
                Width = 90,
                Height = 32,
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnFindConsignment.FlatAppearance.BorderSize = 0;

            refRow.Controls.Add(new Label
            {
                Text = "Reference #:",
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            });
            refRow.Controls.Add(_txtConsignmentRef);
            refRow.Controls.Add(_btnFindConsignment);

            var custRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                WrapContents = false
            };
            _txtConsignmentCustomerSearch = new TextBox
            {
                Width = 165,
                PlaceholderText = "Type name or phone..."
            };
            _cmbConsignmentCustomer = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 210
            };
            _cmbCustomerConsignments = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 330
            };

            custRow.Controls.Add(new Label
            {
                Text = "Customer:",
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            });
            custRow.Controls.Add(_txtConsignmentCustomerSearch);
            custRow.Controls.Add(_cmbConsignmentCustomer);
            custRow.Controls.Add(new Label
            {
                Text = "Consignment:",
                AutoSize = true,
                Margin = new Padding(12, 6, 6, 0)
            });
            custRow.Controls.Add(_cmbCustomerConsignments);

            searchPanel.Controls.Add(custRow);
            searchPanel.Controls.Add(refRow);

            // Row 1: Consignment line items grid
            _dgvConsignmentLines = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            _dgvConsignmentLines.Columns.Add("LineId", "ID");
            _dgvConsignmentLines.Columns.Add("Product", "Product");
            _dgvConsignmentLines.Columns.Add("Issued", "Issued");
            _dgvConsignmentLines.Columns.Add("Returned", "Accepted So Far");
            _dgvConsignmentLines.Columns.Add("Outstanding", "Outstanding");
            _dgvConsignmentLines.Columns.Add("Price", $"Issue Price ({currency})");
            _dgvConsignmentLines.Columns.Add("ReturnQty", "Return Qty");
            _dgvConsignmentLines.Columns.Add("AcceptedQty", "Accepted Qty");

            _dgvConsignmentLines.Columns["LineId"]!.Visible = false;
            for (int i = 0; i < 6; i++)
                _dgvConsignmentLines.Columns[i].ReadOnly = true;

            var editableTint = Color.FromArgb(240, 249, 255);
            _dgvConsignmentLines.Columns["ReturnQty"]!.DefaultCellStyle.BackColor = editableTint;
            _dgvConsignmentLines.Columns["AcceptedQty"]!.DefaultCellStyle.BackColor = editableTint;

            // Row 2: Settlement summary + actions
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(249, 250, 251),
                Padding = new Padding(10)
            };

            _lblConsignmentStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            _lblConsignmentSummary = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                WrapContents = false
            };

            _btnProcessReturn = new Button
            {
                Text = "Process Return",
                Width = 150,
                Height = 36,
                BackColor = Color.FromArgb(34, 139, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnProcessReturn.FlatAppearance.BorderSize = 0;

            _btnViewConsignmentHistory = new Button
            {
                Text = "View History",
                Width = 135,
                Height = 36,
                BackColor = Color.FromArgb(55, 65, 81),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnViewConsignmentHistory.FlatAppearance.BorderSize = 0;

            _nudConsignmentPayment = new NumericUpDown
            {
                DecimalPlaces = 2,
                Maximum = 100000000m,
                Width = 130,
                ThousandsSeparator = true
            };

            _cmbConsignmentPaymentMethod = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 135
            };
            _cmbConsignmentPaymentMethod.Items.AddRange(new object[]
            {
                "Cash",
                "Card",
                "Bank Transfer"
            });
            _cmbConsignmentPaymentMethod.SelectedIndex = 0;

            _btnRecordConsignmentPayment = new Button
            {
                Text = "Record Payment",
                Width = 155,
                Height = 36,
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnRecordConsignmentPayment.FlatAppearance.BorderSize = 0;

            actionRow.Controls.Add(_btnProcessReturn);
            actionRow.Controls.Add(_btnViewConsignmentHistory);
            actionRow.Controls.Add(new Label
            {
                Text = $"Payment ({currency}):",
                AutoSize = true,
                Margin = new Padding(18, 8, 6, 0)
            });
            actionRow.Controls.Add(_nudConsignmentPayment);
            actionRow.Controls.Add(_cmbConsignmentPaymentMethod);
            actionRow.Controls.Add(_btnRecordConsignmentPayment);

            bottomPanel.Controls.Add(_lblConsignmentSummary);
            bottomPanel.Controls.Add(_lblConsignmentStatus);
            bottomPanel.Controls.Add(actionRow);

            layout.Controls.Add(searchPanel, 0, 0);
            layout.Controls.Add(_dgvConsignmentLines, 0, 1);
            layout.Controls.Add(bottomPanel, 0, 2);
            tabConsignmentReturns.Controls.Add(layout);

            WireConsignmentReturnsEvents();
            ResetConsignmentReturnsView("Enter a reference number or select a customer consignment to begin.");
        }

        private void WireConsignmentReturnsEvents()
        {
            _btnFindConsignment.Click += (_, _) => LoadConsignmentForReturn(_txtConsignmentRef.Text);
            _btnProcessReturn.Click += (_, _) => ProcessConsignmentReturn();
            _btnViewConsignmentHistory.Click += (_, _) => ShowConsignmentHistoryDialog();
            _btnRecordConsignmentPayment.Click += (_, _) => RecordConsignmentPayment();

            _txtConsignmentRef.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    LoadConsignmentForReturn(_txtConsignmentRef.Text);
                }
            };

            _txtConsignmentCustomerSearch.TextChanged += (_, _) =>
                FilterConsignmentCustomers(_txtConsignmentCustomerSearch.Text);

            _cmbConsignmentCustomer.SelectedIndexChanged += (_, _) =>
            {
                if (_cmbConsignmentCustomer.SelectedValue is int customerId && customerId > 0)
                {
                    LoadCustomerConsignmentOptions(customerId);
                }
            };

            _cmbCustomerConsignments.SelectedIndexChanged += (_, _) =>
            {
                if (_cmbCustomerConsignments.SelectedItem is ConsignmentSummaryItem item)
                {
                    _txtConsignmentRef.Text = item.ReferenceNumber;
                    LoadConsignmentForReturn(item.ReferenceNumber);
                }
            };

            _dgvConsignmentLines.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_dgvConsignmentLines.IsCurrentCellDirty)
                {
                    _dgvConsignmentLines.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            _dgvConsignmentLines.CellValueChanged += DgvConsignmentLines_CellValueChanged;
        }

        private void LoadConsignmentCustomers()
        {
            _allConsignmentCustomers = _customerRepository.GetAll();
            FilterConsignmentCustomers(_txtConsignmentCustomerSearch.Text);
        }

        private void FilterConsignmentCustomers(string searchText)
        {
            int? previousId = _cmbConsignmentCustomer.SelectedValue as int?;
            string term = searchText?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(term)
                ? _allConsignmentCustomers.ToList()
                : _allConsignmentCustomers
                    .Where(c =>
                        c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(c.PhoneNumber) &&
                         c.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        c.Id.ToString() == term)
                    .ToList();

            _cmbConsignmentCustomer.DataSource = null;
            _cmbConsignmentCustomer.DisplayMember = nameof(Customer.Name);
            _cmbConsignmentCustomer.ValueMember = nameof(Customer.Id);
            _cmbConsignmentCustomer.DataSource = filtered;

            if (filtered.Count == 0)
            {
                _cmbConsignmentCustomer.SelectedIndex = -1;
                _cmbCustomerConsignments.DataSource = null;
            }
            else if (previousId.HasValue && filtered.Any(c => c.Id == previousId.Value))
            {
                _cmbConsignmentCustomer.SelectedValue = previousId.Value;
            }
            else if (!string.IsNullOrEmpty(term))
            {
                _cmbConsignmentCustomer.SelectedIndex = 0;
            }
            else
            {
                _cmbConsignmentCustomer.SelectedIndex = -1;
                _cmbCustomerConsignments.DataSource = null;
            }
        }

        private void LoadCustomerConsignmentOptions(int customerId)
        {
            try
            {
                if (!RequirePermission("Consignments.Return"))
                    return;

                var items = _consignmentReturnService.GetCustomerConsignments(
                    customerId,
                    GetCurrencyPrefix(),
                    GetConfiguredDateFormat());

                _cmbCustomerConsignments.DataSource = null;
                _cmbCustomerConsignments.DisplayMember = nameof(ConsignmentSummaryItem.DisplayText);
                _cmbCustomerConsignments.ValueMember = nameof(ConsignmentSummaryItem.ReferenceNumber);
                _cmbCustomerConsignments.DataSource = items;

                if (items.Count == 0)
                {
                    _cmbCustomerConsignments.SelectedIndex = -1;
                    ResetConsignmentReturnsView("This customer has no consignments on record.");
                }
            }
            catch (Exception ex)
            {
                ShowConsignmentError(ex);
            }
        }

        private void DgvConsignmentLines_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex != _dgvConsignmentLines.Columns["ReturnQty"]!.Index) return;

            var row = _dgvConsignmentLines.Rows[e.RowIndex];
            var accepted = row.Cells["AcceptedQty"].Value?.ToString();
            if (accepted == "0" || string.IsNullOrWhiteSpace(accepted))
                row.Cells["AcceptedQty"].Value = row.Cells["ReturnQty"].Value;
        }

        private void LoadConsignmentForReturn(string reference)
        {
            try
            {
                if (!RequirePermission("Consignments.Return"))
                    return;

                var view = _consignmentReturnService.FindConsignment(reference);

                if (view == null)
                {
                    ResetConsignmentReturnsView("No consignment found with that reference.");
                    return;
                }

                _currentConsignmentView = view;
                string currency = GetCurrencyPrefix();
                string dateFormat = GetConfiguredDateFormat();

                _dgvConsignmentLines.Rows.Clear();

                foreach (var s in view.Lines)
                {
                    _dgvConsignmentLines.Rows.Add(
                        s.Line.Id,
                        s.Line.ProductName,
                        s.Line.Quantity,
                        s.AcceptedReturned,
                        s.Outstanding,
                        s.Line.UnitPrice.ToString("N2"),
                        0,
                        0);
                }

                var st = view.Settlement;
                bool hasOutstandingGoods = view.Lines.Any(l => l.Outstanding > 0);
                bool canPay = !st.IsSettled && st.Outstanding > 0m;

                _btnViewConsignmentHistory.Enabled = true;
                _btnProcessReturn.Enabled = !st.IsSettled && hasOutstandingGoods;
                _btnRecordConsignmentPayment.Enabled = canPay;
                _nudConsignmentPayment.Enabled = canPay;
                _nudConsignmentPayment.Value = 0m;
                _nudConsignmentPayment.Maximum = canPay ? st.Outstanding : 0.01m;

                _lblConsignmentStatus.Text = st.IsSettled
                    ? "STATUS: SETTLED"
                    : $"STATUS: NOT SETTLED — OUTSTANDING: {currency}{st.Outstanding:N2}";
                _lblConsignmentStatus.ForeColor = st.IsSettled ? Color.SeaGreen : Color.Crimson;

                _lblConsignmentSummary.Text =
                    $"Reference: {view.Consignment.ReferenceNumber}   |   " +
                    $"Issued: {view.Consignment.IssuedAt.ToLocalTime().ToString(dateFormat)}\r\n" +
                    $"Original Goods: {currency}{st.OriginalTotal:N2}   |   " +
                    $"Fee (5%): {currency}{st.Fee:N2}   |   " +
                    $"Accepted Returns: {currency}{st.AcceptedReturnsValue:N2}\r\n" +
                    $"Final Balance: {currency}{st.FinalBalance:N2}   |   " +
                    $"Total Paid: {currency}{st.TotalPaid:N2}";
            }
            catch (Exception ex)
            {
                ShowConsignmentError(ex);
            }
        }

        private void ResetConsignmentReturnsView(string message)
        {
            _dgvConsignmentLines.Rows.Clear();
            _currentConsignmentView = null;
            _lblConsignmentStatus.Text = string.Empty;
            _lblConsignmentSummary.Text = message;

            _btnProcessReturn.Enabled = false;
            _btnRecordConsignmentPayment.Enabled = false;
            _btnViewConsignmentHistory.Enabled = false;
            _nudConsignmentPayment.Enabled = false;
            _nudConsignmentPayment.Value = 0m;
        }

        private void ProcessConsignmentReturn()
        {
            if (!RequirePermission("Consignments.Return"))
                return;

            if (_currentConsignmentView == null)
            {
                MessageBox.Show("Find a consignment first.", "Returns",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _dgvConsignmentLines.EndEdit();

            var requested = new List<ReturnRequestLine>();

            foreach (DataGridViewRow row in _dgvConsignmentLines.Rows)
            {
                if (!int.TryParse(row.Cells["ReturnQty"].Value?.ToString(), out int returned) ||
                    !int.TryParse(row.Cells["AcceptedQty"].Value?.ToString(), out int accepted))
                {
                    MessageBox.Show("Return and accepted quantities must be whole numbers.",
                        "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (returned == 0 && accepted == 0) continue;

                int lineId = Convert.ToInt32(row.Cells["LineId"].Value);
                requested.Add(new ReturnRequestLine(lineId, returned, accepted));
            }

            if (requested.Count == 0)
            {
                MessageBox.Show("Enter a return quantity for at least one product.",
                    "Returns", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                "Only accepted quantities are added back to stock. Process this return?",
                "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                string reference = _currentConsignmentView.Consignment.ReferenceNumber;
                var saved = _consignmentReturnService.ProcessReturn(reference, requested);

                MessageBox.Show($"Return {saved.ReferenceNumber} saved.", "Return Processed",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadConsignmentForReturn(reference);
            }
            catch (Exception ex)
            {
                ShowConsignmentError(ex);
            }
        }

        private void RecordConsignmentPayment()
        {
            if (!RequirePermission("Consignments.Return"))
                return;

            if (_currentConsignmentView == null)
            {
                MessageBox.Show("Find a consignment first.", "Payment",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string reference = _currentConsignmentView.Consignment.ReferenceNumber;
                var refreshedView = _consignmentReturnService.FindConsignment(reference)
                    ?? throw new InvalidOperationException("Consignment not found.");
                _currentConsignmentView = refreshedView;

                decimal outstanding = Math.Round(
                    refreshedView.Settlement.Outstanding, 2, MidpointRounding.AwayFromZero);

                if (refreshedView.Settlement.IsSettled || outstanding <= 0m)
                {
                    MessageBox.Show("This consignment is already settled.", "Payment",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadConsignmentForReturn(reference);
                    return;
                }

                var innerTextBox = _nudConsignmentPayment.Controls.OfType<TextBox>().FirstOrDefault();
                decimal enteredAmount = _nudConsignmentPayment.Value;

                if (innerTextBox != null &&
                    decimal.TryParse(
                        innerTextBox.Text,
                        System.Globalization.NumberStyles.Currency,
                        System.Globalization.CultureInfo.CurrentCulture,
                        out decimal typedValue))
                {
                    enteredAmount = typedValue;
                }

                enteredAmount = Math.Round(enteredAmount, 2, MidpointRounding.AwayFromZero);

                if (enteredAmount <= 0m)
                {
                    MessageBox.Show("Enter a payment amount greater than zero.", "Payment",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (enteredAmount > outstanding)
                {
                    _nudConsignmentPayment.Value = Math.Min(outstanding, _nudConsignmentPayment.Maximum);
                    if (innerTextBox != null)
                        innerTextBox.Text = outstanding.ToString("N2");

                    MessageBox.Show(
                        $"Payment cannot exceed the outstanding balance ({GetCurrencyPrefix()}{outstanding:N2}).",
                        "Payment Exceeds Balance",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _nudConsignmentPayment.Value = enteredAmount;

                _consignmentReturnService.RecordPayment(
                    reference,
                    enteredAmount,
                    _cmbConsignmentPaymentMethod.SelectedItem?.ToString() ?? "Cash");

                MessageBox.Show("Payment recorded.", "Payment",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _nudConsignmentPayment.Value = 0m;
                LoadConsignmentForReturn(reference);

                if (_cmbConsignmentCustomer.SelectedValue is int customerId && customerId > 0)
                {
                    LoadCustomerConsignmentOptions(customerId);
                }
            }
            catch (Exception ex)
            {
                ShowConsignmentError(ex);
            }
        }

        private void ShowConsignmentHistoryDialog()
        {
            if (!RequirePermission("Consignments.Return"))
                return;

            if (_currentConsignmentView == null) return;

            try
            {
                var consignment = _currentConsignmentView.Consignment;
                var returns = _consignmentReturnService.GetReturnHistory(consignment.Id);
                var payments = _consignmentReturnService.GetPaymentHistory(consignment.Id);
                string currency = GetCurrencyPrefix();
                string dateFormat = GetConfiguredDateFormat();

                using Form historyForm = new()
                {
                    Text = $"Consignment History - {consignment.ReferenceNumber}",
                    Size = new Size(860, 500),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F)
                };

                var tabs = new TabControl { Dock = DockStyle.Fill };

                var returnsTab = new TabPage($"Returns ({returns.Count})") { BackColor = Color.White };
                var returnsGrid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White
                };
                returnsGrid.Columns.Add("Ref", "Return Reference");
                returnsGrid.Columns.Add("Date", "Date & Time");
                returnsGrid.Columns.Add("Returned", "Total Returned");
                returnsGrid.Columns.Add("Accepted", "Accepted (Restocked)");
                returnsGrid.Columns.Add("Rejected", "Rejected");
                returnsGrid.Columns.Add("Value", $"Accepted Value ({currency})");

                foreach (var r in returns)
                {
                    returnsGrid.Rows.Add(
                        r.ReferenceNumber,
                        r.ReturnDate.ToLocalTime().ToString($"{dateFormat} HH:mm"),
                        r.Lines.Sum(l => l.QuantityReturned),
                        r.Lines.Sum(l => l.QuantityAccepted),
                        r.Lines.Sum(l => l.QuantityRejected),
                        r.Lines.Sum(l => l.AcceptedValue).ToString("N2"));
                }
                returnsTab.Controls.Add(returnsGrid);

                var paymentsTab = new TabPage($"Payments ({payments.Count})") { BackColor = Color.White };
                var paymentsGrid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White
                };
                paymentsGrid.Columns.Add("Date", "Date & Time");
                paymentsGrid.Columns.Add("Method", "Payment Method");
                paymentsGrid.Columns.Add("Amount", $"Amount ({currency})");

                foreach (var p in payments)
                {
                    paymentsGrid.Rows.Add(
                        p.PaymentDate.ToLocalTime().ToString($"{dateFormat} HH:mm"),
                        p.PaymentMethod,
                        p.Amount.ToString("N2"));
                }
                paymentsTab.Controls.Add(paymentsGrid);

                tabs.TabPages.Add(returnsTab);
                tabs.TabPages.Add(paymentsTab);
                historyForm.Controls.Add(tabs);

                historyForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ShowConsignmentError(ex);
            }
        }

        private static void ShowConsignmentError(Exception ex)
        {
            var icon = ex is UnauthorizedAccessException or InvalidOperationException or ArgumentException
                ? MessageBoxIcon.Warning
                : MessageBoxIcon.Error;

            MessageBox.Show(ex.Message, "Consignment Returns", MessageBoxButtons.OK, icon);
        }

        private void btnRecordCreditPayment_Click(object? sender, EventArgs e)
        {
            if (!RequirePermission("Credit.Manage"))
                return;

            if (_creditAccountsGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Please select a credit account first.",
                    "No Account Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (_creditAccountsGrid.SelectedRows[0].DataBoundItem
                is not CreditSale selectedSale)
            {
                MessageBox.Show(
                    "Unable to retrieve the selected credit account. Please search for the customer again.",
                    "Credit Account Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            try
            {
                var payments = _creditService.GetPaymentHistory(selectedSale.Id);
                decimal paidAmount = payments.Sum(payment => payment.Amount);
                decimal outstandingBalance = selectedSale.TotalAmount - paidAmount;

                if (outstandingBalance <= 0m)
                {
                    MessageBox.Show(
                        "This credit account has already been fully paid.",
                        "Account Settled",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }

                if (outstandingBalance < 0.01m ||
                    outstandingBalance != decimal.Round(outstandingBalance, 2))
                {
                    MessageBox.Show(
                        "The remaining balance cannot be represented exactly to two decimal places. Please review the account before recording another payment.",
                        "Balance Requires Review",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                using Form paymentForm = new()
                {
                    Text = $"Record Payment - {selectedSale.ReferenceNumber}",
                    Size = new Size(470, 360),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White,
                    Font = new Font("Segoe UI", 9F)
                };

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20),
                    ColumnCount = 2,
                    RowCount = 5,
                    BackColor = Color.White
                };

                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                var lblOutstanding = new Label
                {
                    Text = $"Outstanding Balance: {GetCurrencyPrefix()}{outstandingBalance:N2}",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    AutoSize = true,
                    Anchor = AnchorStyles.Left
                };

                layout.Controls.Add(lblOutstanding, 0, 0);
                layout.SetColumnSpan(lblOutstanding, 2);

                var lblAmount = new Label
                {
                    Text = "Payment Amount:",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left
                };

                var nudAmount = new NumericUpDown
                {
                    Minimum = 0.01m,
                    Maximum = 10000000m,
                    DecimalPlaces = 2,
                    Increment = Math.Min(100m, outstandingBalance),
                    Width = 220,
                    Font = new Font("Segoe UI", 10F),
                    ThousandsSeparator = true
                };

                layout.Controls.Add(lblAmount, 0, 1);
                layout.Controls.Add(nudAmount, 1, 1);

                var lblMethod = new Label
                {
                    Text = "Payment Method:",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left
                };

                var cmbMethod = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 220,
                    Font = new Font("Segoe UI", 10F)
                };

                cmbMethod.Items.AddRange(new object[]
                {
                    "Cash",
                    "Card",
                    "Bank Transfer",
                    "Cheque",
                    "Other"
                });

                cmbMethod.SelectedIndex = 0;

                layout.Controls.Add(lblMethod, 0, 2);
                layout.Controls.Add(cmbMethod, 1, 2);

                var lblReference = new Label
                {
                    Text = "Reference Number:",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left
                };

                var txtReference = new TextBox
                {
                    Width = 220,
                    Font = new Font("Segoe UI", 10F),
                    MaxLength = 100
                };

                layout.Controls.Add(lblReference, 0, 3);
                layout.Controls.Add(txtReference, 1, 3);

                var buttonPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(0, 10, 0, 0)
                };

                var btnSave = new Button
                {
                    Text = "Record Payment",
                    Size = new Size(135, 32),
                    BackColor = _selectedButtonColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };

                btnSave.FlatAppearance.BorderSize = 0;

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Size = new Size(90, 32),
                    DialogResult = DialogResult.Cancel
                };

                buttonPanel.Controls.Add(btnSave);
                buttonPanel.Controls.Add(btnCancel);

                layout.Controls.Add(buttonPanel, 0, 4);
                layout.SetColumnSpan(buttonPanel, 2);

                paymentForm.Controls.Add(layout);
                paymentForm.AcceptButton = btnSave;
                paymentForm.CancelButton = btnCancel;

                btnSave.Click += (_, _) =>
                {
                    if (nudAmount.Value <= 0m ||
                        nudAmount.Value > outstandingBalance)
                    {
                        MessageBox.Show(
                            "Enter a payment amount greater than zero that does not exceed the outstanding balance.",
                            "Invalid Amount",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    if (cmbMethod.SelectedItem == null)
                    {
                        MessageBox.Show(
                            "Please select a payment method.",
                            "Payment Method Required",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    try
                    {
                        _creditService.RecordPayment(
                            selectedSale.Id,
                            nudAmount.Value,
                            cmbMethod.SelectedItem.ToString()!,
                            string.IsNullOrWhiteSpace(txtReference.Text)
                                ? null
                                : txtReference.Text.Trim());

                        MessageBox.Show(
                            "Payment recorded successfully.",
                            "Payment Recorded",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        paymentForm.DialogResult = DialogResult.OK;
                        paymentForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Unable to record payment: {ex.Message}",
                            "Payment Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                };

                if (paymentForm.ShowDialog(this) == DialogResult.OK)
                {
                    btnSearchCreditAccounts_Click(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to prepare payment: {ex.Message}",
                    "Credit Management Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ApplyNavigationPermissions()
        {
            btnDashboard.Enabled = HasPermission("Dashboard.View");
            btnProducts.Enabled = HasPermission("Inventory.View");
            btnReports.Enabled = HasPermission("Reports.View");
            btnConsignments.Enabled =
                HasPermission("Consignments.Issue") ||
                HasPermission("Consignments.Return");

            btnUserManagement.Enabled =
                HasPermission("Users.Manage") ||
                HasPermission("Roles.Manage") ||
                HasPermission("Permissions.Manage") ||
                HasPermission("AuditLog.View");

            btnSettings.Enabled = HasPermission("Settings.Manage");

            _btnIssueCreditSale.Enabled = HasPermission("Credit.Manage");
            _btnCreditManagement.Visible = true;
            _btnCreditManagement.Enabled = HasPermission("Credit.Manage");

            _btnCreditManagement.BackColor = _btnCreditManagement.Enabled
                ? _normalButtonColor
                : Color.Gray;

            _btnSales.Visible = true;
            _btnSales.Enabled = HasPermission("Sales.Process");
            _btnSales.BackColor = _btnSales.Enabled
                ? _normalButtonColor
                : Color.Gray;

            btnProducts.Enabled = HasPermission("Inventory.View");

            ApplyManagementTabPermissions();
            ApplyManagementActionPermissions();

            if (!HasPermission("Inventory.View"))
            {
                ClearInventory();
                pnlInventory.Visible = false;
            }

            ApplyInventoryPermissions();
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
            else if (pageTitle == "Credit Management")
            {
                LoadCreditCustomers();
            }
            else if (pageTitle == "Consignments")
            {
                LoadConsignmentCustomers();
            }

            // Hide all pages
            pnlDashboard.Visible = false;
            pnlReports.Visible = false;
            pnlConsignments.Visible = false;
            pnlInventory.Visible = false;
            pnlUserManagement.Visible = false;
            pnlSettings.Visible = false;
            _pnlCreditManagement.Visible = false;
            _pnlCustomers.Visible = false;
            _pnlSales.Visible = false;

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
                "Credit Management" => "Manage customer credit accounts and payments.",
                "Settings" => "Configure your business and system preferences.",
                "Inventory Management" => "View products, monitor stock levels and manage inventory.",
                "Customers" => "Manage customer profiles and contact details.",
                "Point of Sale" => "Process regular cash and card sales and print receipts.",
                _ => string.Empty
            };

            // Reset navigation button colours
            btnDashboard.BackColor = btnDashboard.Enabled ? _normalButtonColor : Color.Gray;
            btnProducts.BackColor = btnProducts.Enabled ? _normalButtonColor : Color.Gray;
            btnReports.BackColor = btnReports.Enabled ? _normalButtonColor : Color.Gray;
            btnConsignments.BackColor = btnConsignments.Enabled ? _normalButtonColor : Color.Gray;
            btnUserManagement.BackColor = btnUserManagement.Enabled ? _normalButtonColor : Color.Gray;
            _btnCreditManagement.BackColor = _btnCreditManagement.Enabled ? _normalButtonColor : Color.Gray;
            _btnCustomers.BackColor = _btnCustomers.Enabled ? _normalButtonColor : Color.Gray;
            btnSettings.BackColor = btnSettings.Enabled ? _normalButtonColor : Color.Gray;
            _btnSales.BackColor = _btnSales.Enabled ? _normalButtonColor : Color.Gray;

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
            else if (CanAccessPage("Point of Sale"))
            {
                OpenSalesPage();
            }
            else if (CanAccessPage("Inventory Management"))
            {
                ShowPage(pnlInventory, "Inventory Management", btnProducts);
                LoadInventory();
            }
            else if (CanAccessPage("Reports"))
            {
                ShowPage(pnlReports, "Reports", btnReports);
            }
            else if (CanAccessPage("Consignments"))
            {
                ShowPage(pnlConsignments, "Consignments", btnConsignments);
            }
            else if (CanAccessPage("User Management"))
            {
                ShowPage(pnlUserManagement, "User Management", btnUserManagement);
            }
            else if (CanAccessPage("Customers"))
            {
                ShowPage(_pnlCustomers, "Customers", _btnCustomers);
                LoadCustomersManagement();
            }
            else if (CanAccessPage("Settings"))
            {
                ShowPage(pnlSettings, "Settings", btnSettings);
                LoadBusinessSettings();
            }
            else if (CanAccessPage("Credit Management"))
            {
                ShowPage(_pnlCreditManagement, "Credit Management", _btnCreditManagement);
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

        private string GetCurrencyPrefix()
        {
            try
            {
                string code = _businessSettingsService.GetSettings().CurrencyCode;
                return code.Equals("JMD", StringComparison.OrdinalIgnoreCase)
                    ? "J$"
                    : $"{code} ";
            }
            catch
            {
                return "J$";
            }
        }

        private string FormatConfiguredCurrency(decimal amount) =>
            $"{GetCurrencyPrefix()}{amount:N2}";

        private string GetConfiguredDateFormat()
        {
            try
            {
                string format = _businessSettingsService.GetSettings().DateFormat;
                return string.IsNullOrWhiteSpace(format) ? "dd/MM/yyyy" : format;
            }
            catch
            {
                return "dd/MM/yyyy";
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
            ApplyDashboardPermissions();
            try
            {
                var summary = _dashboardService.GetDashboardSummary();

                lblTotalProductsValue.Text =
                    summary.TotalProducts.ToString("N0");

                lblActiveCustomersValue.Text =
                    summary.TotalCustomers.ToString("N0");

                lblSalesTodayValue.Text =
                    FormatConfiguredCurrency(summary.SalesToday);

                if (lblTransactionsToday != null)
                {
                    lblTransactionsToday.Text =
                        $"{summary.TransactionsToday} transactions";
                }

                lblPendingConsignmentsValue.Text =
                    summary.PendingConsignments.ToString("N0");

                lblOutstandingConsignmentValue.Text =
                    FormatConfiguredCurrency(summary.OutstandingConsignmentBalance);

                lblOutstandingCreditValue.Text =
                    FormatConfiguredCurrency(summary.OutstandingCreditBalance);

                lblOverdueCreditValue.Text =
                    summary.OverdueCreditAccounts.ToString("N0");

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

        private void btnCreditManagement_Click(object? sender, EventArgs e)
        {
            ShowPage(_pnlCreditManagement, "Credit Management", _btnCreditManagement);
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

            _posCart.Clear();
            _lastPosReceipt = null;
            _btnPosPrintReceipt.Enabled = false;

            Hide();

            using var loginForm = new LoginForm(_loginService);

            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                UpdateSessionDisplay();
                ApplyNavigationPermissions();
                ApplyDashboardPermissions();
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

                if (cmbReportType.SelectedIndex != 5 && startDate > endDate)
                {
                    MessageBox.Show(
                        "Start date cannot be after the end date.",
                        "Invalid Date Range",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                string? selectedReference =
                    cmbStatementReference.SelectedIndex > 0
                        ? cmbStatementReference.SelectedItem?.ToString()
                        : null;

                switch (cmbReportType.SelectedIndex)
                {
                    case 0:
                        DisplaySalesReport(
                            _reportingService.GetSalesReport(startDate, endDate));
                        break;

                    case 1:
                        DisplayCreditReport(
                            _reportingService.GetCreditReport(startDate, endDate));
                        break;

                    case 2:
                        DisplayConsignmentReport(
                            _reportingService.GetConsignmentReport(startDate, endDate));
                        break;

                    case 3:
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
                                creditCustomerId, startDate, endDate, selectedReference));
                        break;

                    case 4:
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
                                consignmentCustomerId, startDate, endDate, selectedReference));
                        break;

                    case 5:
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
                    "Feature Unavailable",
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

            using Font titleFont = new(
                "Arial", 16, FontStyle.Bold);

            using Font summaryFont = new(
                "Arial", 9, FontStyle.Regular);

            using Font headerFont = new(
                "Arial", 9, FontStyle.Bold);

            using Font rowFont = new(
                "Arial", 8, FontStyle.Regular);

            using Pen borderPen = new(Color.Black);

            int y = bounds.Top;

            // Report title
            graphics.DrawString(
                GetConfiguredBusinessName(),
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
                $"Total Revenue: {FormatConfiguredCurrency(report.TotalSalesRevenue)}";
            btnPrintReport.Enabled = report.TransactionCount > 0;

            btnExportPdf.Enabled = report.TransactionCount > 0;
            btnExportExcel.Enabled = report.TransactionCount > 0;
            btnExportWord.Enabled = report.TransactionCount > 0;
        }

        private void DisplayCreditReport(CreditReportResult report)
        {
            _currentReportName = "Credit Report";

            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Accounts.ToList();

            if (dgvReportResults.Columns[nameof(CreditReportRow.CreditSaleId)] != null)
                dgvReportResults.Columns[nameof(CreditReportRow.CreditSaleId)]!.Visible = false;

            if (dgvReportResults.Columns[nameof(CreditReportRow.IssuedDate)] != null)
            {
                dgvReportResults.Columns[nameof(CreditReportRow.IssuedDate)]!.HeaderText = "Date Issued";
                dgvReportResults.Columns[nameof(CreditReportRow.IssuedDate)]!.DefaultCellStyle.Format = "dd MMM yyyy";
            }

            if (dgvReportResults.Columns[nameof(CreditReportRow.DueDate)] != null)
            {
                dgvReportResults.Columns[nameof(CreditReportRow.DueDate)]!.HeaderText = "Due Date";
                dgvReportResults.Columns[nameof(CreditReportRow.DueDate)]!.DefaultCellStyle.Format = "dd MMM yyyy";
            }

            if (dgvReportResults.Columns[nameof(CreditReportRow.ReferenceNumber)] != null)
                dgvReportResults.Columns[nameof(CreditReportRow.ReferenceNumber)]!.HeaderText = "Reference";

            if (dgvReportResults.Columns[nameof(CreditReportRow.CustomerName)] != null)
                dgvReportResults.Columns[nameof(CreditReportRow.CustomerName)]!.HeaderText = "Customer";

            if (dgvReportResults.Columns[nameof(CreditReportRow.PurchaseAmount)] != null)
            {
                dgvReportResults.Columns[nameof(CreditReportRow.PurchaseAmount)]!.HeaderText = "Purchase Amount";
                dgvReportResults.Columns[nameof(CreditReportRow.PurchaseAmount)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(CreditReportRow.CreditFee)] != null)
            {
                dgvReportResults.Columns[nameof(CreditReportRow.CreditFee)]!.HeaderText = "Credit Fee";
                dgvReportResults.Columns[nameof(CreditReportRow.CreditFee)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(CreditReportRow.TotalAmount)] != null)
            {
                dgvReportResults.Columns[nameof(CreditReportRow.TotalAmount)]!.HeaderText = "Total Amount";
                dgvReportResults.Columns[nameof(CreditReportRow.TotalAmount)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(CreditReportRow.AmountPaid)] != null)
            {
                dgvReportResults.Columns[nameof(CreditReportRow.AmountPaid)]!.HeaderText = "Paid";
                dgvReportResults.Columns[nameof(CreditReportRow.AmountPaid)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(CreditReportRow.OutstandingBalance)] != null)
            {
                dgvReportResults.Columns[nameof(CreditReportRow.OutstandingBalance)]!.HeaderText = "Outstanding";
                dgvReportResults.Columns[nameof(CreditReportRow.OutstandingBalance)]!.DefaultCellStyle.Format = "N2";
            }

            lblReportSummary.Text =
                $"Credit Report | {report.StartDate:dd MMM yyyy} - {report.EndDate:dd MMM yyyy} | " +
                $"Accounts: {report.Accounts.Count} | " +
                $"Purchases: {FormatConfiguredCurrency(report.TotalPurchases)} | " +
                $"Fees: {FormatConfiguredCurrency(report.TotalFees)} | " +
                $"Paid: {FormatConfiguredCurrency(report.TotalPaid)} | " +
                $"Outstanding: {FormatConfiguredCurrency(report.TotalOutstanding)}";

            btnPrintReport.Enabled = report.Accounts.Count > 0;
            btnExportPdf.Enabled = true;
            btnExportExcel.Enabled = true;
            btnExportWord.Enabled = true;
        }

        private void DisplayConsignmentReport(ConsignmentReportResult report)
        {
            _currentReportName = "Consignment Report";

            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Consignments.ToList();

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.ConsignmentId)] != null)
                dgvReportResults.Columns[nameof(ConsignmentReportRow.ConsignmentId)]!.Visible = false;

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.IssueDate)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentReportRow.IssueDate)]!.HeaderText = "Issue Date";
                dgvReportResults.Columns[nameof(ConsignmentReportRow.IssueDate)]!.DefaultCellStyle.Format = "dd MMM yyyy";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.ReferenceNumber)] != null)
                dgvReportResults.Columns[nameof(ConsignmentReportRow.ReferenceNumber)]!.HeaderText = "Reference";

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.CustomerName)] != null)
                dgvReportResults.Columns[nameof(ConsignmentReportRow.CustomerName)]!.HeaderText = "Customer";

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.OriginalGoodsValue)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentReportRow.OriginalGoodsValue)]!.HeaderText = "Original Value";
                dgvReportResults.Columns[nameof(ConsignmentReportRow.OriginalGoodsValue)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.AcceptedReturnsValue)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentReportRow.AcceptedReturnsValue)]!.HeaderText = "Accepted Returns";
                dgvReportResults.Columns[nameof(ConsignmentReportRow.AcceptedReturnsValue)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.ConsignmentFee)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentReportRow.ConsignmentFee)]!.HeaderText = "Fee";
                dgvReportResults.Columns[nameof(ConsignmentReportRow.ConsignmentFee)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.Payments)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentReportRow.Payments)]!.HeaderText = "Payments";
                dgvReportResults.Columns[nameof(ConsignmentReportRow.Payments)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentReportRow.OutstandingBalance)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentReportRow.OutstandingBalance)]!.HeaderText = "Outstanding";
                dgvReportResults.Columns[nameof(ConsignmentReportRow.OutstandingBalance)]!.DefaultCellStyle.Format = "N2";
            }

            lblReportSummary.Text =
                $"Consignment Report | {report.StartDate:dd MMM yyyy} - {report.EndDate:dd MMM yyyy} | " +
                $"Consignments: {report.Consignments.Count} | " +
                $"Original Value: {FormatConfiguredCurrency(report.TotalOriginalValue)} | " +
                $"Returns: {FormatConfiguredCurrency(report.TotalAcceptedReturns)} | " +
                $"Fees: {FormatConfiguredCurrency(report.TotalFees)} | " +
                $"Payments: {FormatConfiguredCurrency(report.TotalPayments)} | " +
                $"Outstanding: {FormatConfiguredCurrency(report.TotalOutstanding)}";

            btnPrintReport.Enabled = report.Consignments.Count > 0;
            btnExportPdf.Enabled = true;
            btnExportExcel.Enabled = true;
            btnExportWord.Enabled = true;
        }

        private void DisplayCreditStatement(CreditStatementResult report)
        {
            _currentReportName = "Credit Statement";

            dgvReportResults.DataSource = null;
            dgvReportResults.DataSource = report.Transactions.ToList();

            if (dgvReportResults.Columns[nameof(CreditStatementRow.CreditSaleId)] != null)
                dgvReportResults.Columns[nameof(CreditStatementRow.CreditSaleId)]!.Visible = false;

            if (dgvReportResults.Columns[nameof(CreditStatementRow.CreditSaleLineId)] != null)
                dgvReportResults.Columns[nameof(CreditStatementRow.CreditSaleLineId)]!.Visible = false;

            if (dgvReportResults.Columns[nameof(CreditStatementRow.TransactionDate)] != null)
            {
                dgvReportResults.Columns[nameof(CreditStatementRow.TransactionDate)]!.HeaderText = "Date Issued";
                dgvReportResults.Columns[nameof(CreditStatementRow.TransactionDate)]!.DefaultCellStyle.Format = "dd MMM yyyy";
            }

            if (dgvReportResults.Columns[nameof(CreditStatementRow.DueDate)] != null)
            {
                dgvReportResults.Columns[nameof(CreditStatementRow.DueDate)]!.HeaderText = "Due Date";
                dgvReportResults.Columns[nameof(CreditStatementRow.DueDate)]!.DefaultCellStyle.Format = "dd MMM yyyy";
            }

            if (dgvReportResults.Columns[nameof(CreditStatementRow.ReferenceNumber)] != null)
                dgvReportResults.Columns[nameof(CreditStatementRow.ReferenceNumber)]!.HeaderText = "Reference";

            if (dgvReportResults.Columns[nameof(CreditStatementRow.ProductName)] != null)
                dgvReportResults.Columns[nameof(CreditStatementRow.ProductName)]!.HeaderText = "Product";

            if (dgvReportResults.Columns[nameof(CreditStatementRow.UnitPrice)] != null)
            {
                dgvReportResults.Columns[nameof(CreditStatementRow.UnitPrice)]!.HeaderText = "Unit Price";
                dgvReportResults.Columns[nameof(CreditStatementRow.UnitPrice)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(CreditStatementRow.Quantity)] != null)
                dgvReportResults.Columns[nameof(CreditStatementRow.Quantity)]!.HeaderText = "Qty";

            if (dgvReportResults.Columns[nameof(CreditStatementRow.LineTotal)] != null)
            {
                dgvReportResults.Columns[nameof(CreditStatementRow.LineTotal)]!.HeaderText = "Line Total";
                dgvReportResults.Columns[nameof(CreditStatementRow.LineTotal)]!.DefaultCellStyle.Format = "N2";
            }

            lblReportSummary.Text =
                $"Credit Statement: {report.CustomerName} | " +
                $"Purchases: {FormatConfiguredCurrency(report.TotalPurchases)} | " +
                $"Fees: {FormatConfiguredCurrency(report.TotalFees)} | " +
                $"Payments: {FormatConfiguredCurrency(report.TotalPayments)} | " +
                $"Outstanding: {FormatConfiguredCurrency(report.TotalOutstanding)}";

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

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.ConsignmentId)] != null)
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.ConsignmentId)]!.Visible = false;

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.ConsignmentLineId)] != null)
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.ConsignmentLineId)]!.Visible = false;

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.IssueDate)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.IssueDate)]!.HeaderText = "Issue Date";
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.IssueDate)]!.DefaultCellStyle.Format = "dd MMM yyyy";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.ReferenceNumber)] != null)
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.ReferenceNumber)]!.HeaderText = "Reference";

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.ProductName)] != null)
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.ProductName)]!.HeaderText = "Product";

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.UnitPrice)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.UnitPrice)]!.HeaderText = "Unit Price";
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.UnitPrice)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.QuantityIssued)] != null)
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.QuantityIssued)]!.HeaderText = "Qty Issued";

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.QuantityAcceptedReturned)] != null)
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.QuantityAcceptedReturned)]!.HeaderText = "Qty Returned";

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.RemainingQuantity)] != null)
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.RemainingQuantity)]!.HeaderText = "Qty Remaining";

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.OriginalLineValue)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.OriginalLineValue)]!.HeaderText = "Original Value";
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.OriginalLineValue)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.AcceptedReturnsValue)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.AcceptedReturnsValue)]!.HeaderText = "Accepted Returns";
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.AcceptedReturnsValue)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(ConsignmentStatementRow.NetGoodsValue)] != null)
            {
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.NetGoodsValue)]!.HeaderText = "Net Goods Value";
                dgvReportResults.Columns[nameof(ConsignmentStatementRow.NetGoodsValue)]!.DefaultCellStyle.Format = "N2";
            }

            lblReportSummary.Text =
                $"Consignment Statement: {report.CustomerName} | " +
                $"Original Value: {FormatConfiguredCurrency(report.TotalOriginalValue)} | " +
                $"Accepted Returns: {FormatConfiguredCurrency(report.TotalAcceptedReturns)} | " +
                $"Fees: {FormatConfiguredCurrency(report.TotalFees)} | " +
                $"Payments: {FormatConfiguredCurrency(report.TotalPayments)} | " +
                $"Outstanding: {FormatConfiguredCurrency(report.TotalOutstanding)}";

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

            if (dgvReportResults.Columns[nameof(InventoryReportRow.ProductId)] != null)
                dgvReportResults.Columns[nameof(InventoryReportRow.ProductId)]!.Visible = false;

            if (dgvReportResults.Columns[nameof(InventoryReportRow.IsLowStock)] != null)
                dgvReportResults.Columns[nameof(InventoryReportRow.IsLowStock)]!.Visible = false;

            if (dgvReportResults.Columns[nameof(InventoryReportRow.ProductName)] != null)
                dgvReportResults.Columns[nameof(InventoryReportRow.ProductName)]!.HeaderText = "Product";

            if (dgvReportResults.Columns[nameof(InventoryReportRow.UnitsPerCase)] != null)
                dgvReportResults.Columns[nameof(InventoryReportRow.UnitsPerCase)]!.HeaderText = "Units/Case";

            if (dgvReportResults.Columns[nameof(InventoryReportRow.AvailableStock)] != null)
                dgvReportResults.Columns[nameof(InventoryReportRow.AvailableStock)]!.HeaderText = "Available Stock";

            if (dgvReportResults.Columns[nameof(InventoryReportRow.OutstandingConsignmentQuantity)] != null)
                dgvReportResults.Columns[nameof(InventoryReportRow.OutstandingConsignmentQuantity)]!.HeaderText = "On Consignment";

            if (dgvReportResults.Columns[nameof(InventoryReportRow.MinimumStockLevel)] != null)
                dgvReportResults.Columns[nameof(InventoryReportRow.MinimumStockLevel)]!.HeaderText = "Reorder Level";

            if (dgvReportResults.Columns[nameof(InventoryReportRow.UnitSellingPrice)] != null)
            {
                dgvReportResults.Columns[nameof(InventoryReportRow.UnitSellingPrice)]!.HeaderText = "Unit Price";
                dgvReportResults.Columns[nameof(InventoryReportRow.UnitSellingPrice)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(InventoryReportRow.StockRetailValue)] != null)
            {
                dgvReportResults.Columns[nameof(InventoryReportRow.StockRetailValue)]!.HeaderText = "Stock Value";
                dgvReportResults.Columns[nameof(InventoryReportRow.StockRetailValue)]!.DefaultCellStyle.Format = "N2";
            }

            if (dgvReportResults.Columns[nameof(InventoryReportRow.StockStatus)] != null)
                dgvReportResults.Columns[nameof(InventoryReportRow.StockStatus)]!.HeaderText = "Status";

            lblReportSummary.Text =
                $"Inventory Report | Products: {report.TotalProducts} | " +
                $"Units In Stock: {report.TotalUnitsInStock} | " +
                $"On Consignment: {report.TotalUnitsOnConsignment} | " +
                $"Low/Out of Stock: {report.LowStockProductCount} | " +
                $"Total Stock Value: {FormatConfiguredCurrency(report.TotalInventoryValue)} | " +
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
                string businessName = GetConfiguredBusinessName();

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
                                .Text(businessName)
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
                                text.Span($"{businessName} | Page ");
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
                worksheet.Cell("A1").Value = GetConfiguredBusinessName();
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
                    GetConfiguredBusinessName(), true, "32"));

                body.Append(CreateWordParagraph(
                    _currentReportName, true, "24"));

                body.Append(CreateWordParagraph(
                    lblReportSummary.Text, false, "20"));

                W.Table table = new();

                table.AppendChild(new TableProperties(
                    new TableBorders(
                        new W.TopBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new W.BottomBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new W.LeftBorder
                        {
                            Val = BorderValues.Single,
                            Size = 6
                        },
                        new W.RightBorder
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
            if (_userManagementTabControl != null)
            {
                pnlUserManagement.Controls.Remove(_userManagementTabControl);
                _userManagementTabControl.Dispose();
                _userManagementTabControl = null!;
            }

            pnlUserManagement.Controls.Clear();

            _userManagementTabControl = new TabControl
            {
                Name = "tabControlUserManagement",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Padding = new Point(15, 5)
            };

            _usersTab = CreateManagementTab("tabUsers", "Users");
            _rolesTab = CreateManagementTab("tabRoles", "Roles");
            _permissionsTab = CreateManagementTab("tabPermissions", "Permissions");
            _auditLogTab = CreateManagementTab("tabAuditLog", "Audit Log");

            _userManagementTabControl.TabPages.AddRange(new[]
            {
                _usersTab,
                _rolesTab,
                _permissionsTab,
                _auditLogTab
            });

            pnlUserManagement.Controls.Add(_userManagementTabControl);

            ConfigureUsersTab(_usersTab);
            ConfigureRolesTab(_rolesTab);
            ConfigurePermissionsTab(_permissionsTab);
            ConfigureAuditLogTab(_auditLogTab);
        }

        private static TabPage CreateManagementTab(string name, string title) =>
            new(title)
            {
                Name = name,
                BackColor = Color.White
            };

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
                BackColor = Color.White
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
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var descriptionLabel = new Label
            {
                Text = "Create roles and manage their permissions.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
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
                BackgroundColor = Color.White
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
                Size = new Size(130, 38),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            _assignPermissionButton = new Button
            {
                Name = "btnAssignPermission",
                Text = "Assign Permission",
                Size = new Size(150, 38),
                BackColor = Color.FromArgb(34, 139, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            _removePermissionButton = new Button
            {
                Name = "btnRemovePermission",
                Text = "Remove Permission",
                Size = new Size(160, 38),
                BackColor = Color.FromArgb(220, 75, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            buttonPanel.Controls.Add(_createRoleButton);
            buttonPanel.Controls.Add(_assignPermissionButton);
            buttonPanel.Controls.Add(_removePermissionButton);

            _createRoleButton.Click += (_, _) => ShowCreateRoleDialog();
            _assignPermissionButton.Click += (_, _) => ShowAssignPermissionDialog();
            _removePermissionButton.Click += (_, _) => ShowRemovePermissionDialog();

            layout.Controls.Add(headingPanel, 0, 0);
            layout.Controls.Add(rolesGrid, 0, 1);
            layout.Controls.Add(buttonPanel, 0, 2);

            rolesTab.Controls.Add(layout);
        }

        private void LoadCreditCustomers()
        {
            _allCreditCustomers = _customerRepository.GetAll();
            FilterCreditCustomerDropdown(_txtCreditCustomerSearch.Text);
        }

        private void FilterCreditCustomerDropdown(string searchText)
        {
            int? previousCustomerId = _cmbCreditCustomer.SelectedValue as int?;
            string term = searchText?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(term)
                ? _allCreditCustomers
                : _allCreditCustomers
                    .Where(c =>
                        c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(c.PhoneNumber) &&
                         c.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        c.Id.ToString() == term)
                    .ToList();

            _cmbCreditCustomer.DataSource = null;
            _cmbCreditCustomer.DisplayMember = nameof(Customer.Name);
            _cmbCreditCustomer.ValueMember = nameof(Customer.Id);
            _cmbCreditCustomer.DataSource = filtered;

            if (filtered.Count == 0)
            {
                _cmbCreditCustomer.SelectedIndex = -1;
                _creditAccountsGrid.DataSource = null;
            }
            else if (previousCustomerId.HasValue &&
                     filtered.Any(c => c.Id == previousCustomerId.Value))
            {
                _cmbCreditCustomer.SelectedValue = previousCustomerId.Value;
            }
            else if (!string.IsNullOrEmpty(term))
            {
                _cmbCreditCustomer.SelectedIndex = 0;
            }
            else
            {
                _cmbCreditCustomer.SelectedIndex = -1;
                _creditAccountsGrid.DataSource = null;
            }
        }

        private void btnSearchCreditAccounts_Click(object? sender, EventArgs e)
        {
            if (!RequirePermission("Credit.Manage"))
                return;

            if (_cmbCreditCustomer.SelectedValue is not int customerId || customerId <= 0)
            {
                _creditAccountsGrid.DataSource = null;

                MessageBox.Show(
                    "Please select a valid customer from the dropdown.",
                    "Customer Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                _cmbCreditCustomer.Focus();
                return;
            }

            LoadCustomerCreditAccounts(customerId, showEmptyMessage: true);
        }

        private void LoadCustomerCreditAccounts(int customerId, bool showEmptyMessage = false)
        {
            try
            {
                var accounts = _creditService.GetCustomerAccount(customerId);

                _creditAccountsGrid.DataSource = null;
                _creditAccountsGrid.DataSource = accounts;

                string dateFormat = GetConfiguredDateFormat();
                _creditAccountsGrid.Columns["IssuedAt"]!.DefaultCellStyle.Format = dateFormat;
                _creditAccountsGrid.Columns["DueDate"]!.DefaultCellStyle.Format = dateFormat;

                for (int i = 0; i < accounts.Count && i < _creditAccountsGrid.Rows.Count; i++)
                {
                    var sale = accounts[i];
                    var payments = _creditService.GetPaymentHistory(sale.Id);
                    decimal paid = payments.Sum(p => p.Amount);
                    decimal outstanding = Math.Max(0m, sale.TotalAmount - paid);

                    string status = outstanding <= 0m
                        ? "Paid"
                        : (DateTime.UtcNow.Date > sale.DueDate.Date ? "Overdue" : "Open");

                    var row = _creditAccountsGrid.Rows[i];
                    row.Cells["AmountPaid"].Value = paid;
                    row.Cells["OutstandingBalance"].Value = outstanding;
                    row.Cells["Status"].Value = status;

                    if (status == "Overdue")
                    {
                        row.Cells["Status"].Style.ForeColor = Color.Crimson;
                        row.Cells["Status"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                    else if (status == "Paid")
                    {
                        row.Cells["Status"].Style.ForeColor = Color.SeaGreen;
                    }
                }

                if (accounts.Count == 0 && showEmptyMessage)
                {
                    MessageBox.Show(
                        "No credit accounts were found for this customer.",
                        "No Credit Accounts",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else if (_creditAccountsGrid.Rows.Count > 0)
                {
                    _creditAccountsGrid.ClearSelection();
                    _creditAccountsGrid.Rows[0].Selected = true;
                }
            }
            catch (Exception ex)
            {
                _creditAccountsGrid.DataSource = null;

                MessageBox.Show(
                    $"Unable to retrieve customer credit accounts: {ex.Message}",
                    "Credit Management Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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
                BackColor = Color.White
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
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var descriptionLabel = new Label
            {
                Text = "View all permissions available within the system.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
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
                BackgroundColor = Color.White
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
                Font = new Font("Segoe UI", 10F),
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
                BackColor = Color.White
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
                Size = new Size(120, 35),
                Location = new Point(0, 5),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            buttonPanel.Controls.Add(refreshButton);

            refreshButton.Click += (_, _) =>
            {
                if (!RequirePermission("AuditLog.View"))
                    return;

                LoadAuditLog();
            };

            var titleLabel = new Label
            {
                Text = "System Audit Log",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var descriptionLabel = new Label
            {
                Text = "Review recorded activities and changes made within the system.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
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
                BackgroundColor = Color.White,
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
                Size = new Size(400, 300),
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
                Size = new Size(85, 32)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(275, 165),
                Size = new Size(85, 32)
            };

            saveButton.Click += (_, _) =>
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

            dialog.Controls.AddRange(new Control[]
            {
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

        private void btnViewCreditPayments_Click(object? sender, EventArgs e)
        {
            if (!RequirePermission("Credit.Manage"))
                return;

            if (_creditAccountsGrid.SelectedRows.Count == 0 ||
                _creditAccountsGrid.SelectedRows[0].DataBoundItem is not CreditSale selectedSale)
            {
                MessageBox.Show(
                    "Please select a credit account first.",
                    "No Account Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var payments = _creditService.GetPaymentHistory(selectedSale.Id);
                decimal totalPaid = payments.Sum(payment => payment.Amount);
                decimal outstanding = selectedSale.TotalAmount - totalPaid;

                using Form paymentForm = new()
                {
                    Text = $"Payment History - {selectedSale.ReferenceNumber}",
                    Size = new Size(850, 480),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.White,
                    Font = new Font("Segoe UI", 9F)
                };

                var summaryLabel = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 48,
                    Padding = new Padding(12),
                    Text = $"Credit Total: {GetCurrencyPrefix()}{selectedSale.TotalAmount:N2}    " +
                           $"Paid: {GetCurrencyPrefix()}{totalPaid:N2}    " +
                           $"Outstanding: {GetCurrencyPrefix()}{Math.Max(0m, outstanding):N2}",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                DataGridView paymentGrid = new()
                {
                    Dock = DockStyle.Fill,
                    AutoGenerateColumns = true,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White,
                    RowHeadersVisible = false,
                    DataSource = payments
                };

                paymentForm.Controls.Add(paymentGrid);
                paymentForm.Controls.Add(summaryLabel);

                if (paymentGrid.Columns[nameof(CreditPayment.Amount)] != null)
                    paymentGrid.Columns[nameof(CreditPayment.Amount)].DefaultCellStyle.Format = "N2";

                if (paymentGrid.Columns[nameof(CreditPayment.PaymentDate)] != null)
                    paymentGrid.Columns[nameof(CreditPayment.PaymentDate)].DefaultCellStyle.Format = $"{GetConfiguredDateFormat()} HH:mm";

                if (payments.Count == 0)
                {
                    summaryLabel.Text += "    No payments have been recorded.";
                }

                paymentForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to retrieve payment history: {ex.Message}",
                    "Credit Management Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                Size = new Size(450, 350),
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
                Size = new Size(390, 30),
                DataSource = availablePermissions,
                DisplayMember = "Name",
                ValueMember = "Id"
            };

            var descriptionLabel = new Label
            {
                Location = new Point(20, 100),
                Size = new Size(390, 70),
                ForeColor = Color.Gray
            };

            permissionComboBox.SelectedIndexChanged += (_, _) =>
            {
                if (permissionComboBox.SelectedItem is Permission selected)
                {
                    descriptionLabel.Text = selected.Description;
                }
            };

            var assignButton = new Button
            {
                Text = "Assign",
                Size = new Size(120, 38),
                Location = new Point(160, 240),
                BackColor = Color.FromArgb(34, 139, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(120, 38),
                Location = new Point(290, 240),
                DialogResult = DialogResult.Cancel
            };

            assignButton.Click += (_, _) =>
            {
                if (permissionComboBox.SelectedItem is not Permission selected)
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

            dialog.Controls.AddRange(new Control[]
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
                Size = new Size(450, 350),
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
                Size = new Size(390, 30),
                DataSource = assignedPermissions,
                DisplayMember = "Name",
                ValueMember = "Id"
            };

            var descriptionLabel = new Label
            {
                Location = new Point(20, 100),
                Size = new Size(390, 70),
                ForeColor = Color.Gray
            };

            permissionComboBox.SelectedIndexChanged += (_, _) =>
            {
                if (permissionComboBox.SelectedItem is Permission selected)
                {
                    descriptionLabel.Text = selected.Description;
                }
            };

            var removeButton = new Button
            {
                Text = "Remove",
                Size = new Size(120, 38),
                Location = new Point(160, 240),
                BackColor = Color.FromArgb(220, 75, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(120, 38),
                Location = new Point(290, 240),
                DialogResult = DialogResult.Cancel
            };

            removeButton.Click += (_, _) =>
            {
                if (permissionComboBox.SelectedItem is not Permission selected)
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

            dialog.Controls.AddRange(new Control[]
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
                BackColor = Color.White
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
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var descriptionLabel = new Label
            {
                Text = "Create accounts and manage user access.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
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
                BackgroundColor = Color.White
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
                Size = new Size(130, 38),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            _activateUserButton = new Button
            {
                Name = "btnActivateUser",
                Text = "Activate",
                Size = new Size(130, 38)
            };

            _deactivateUserButton = new Button
            {
                Name = "btnDeactivateUser",
                Text = "Deactivate",
                Size = new Size(130, 38)
            };

            _assignRoleButton = new Button
            {
                Name = "btnAssignRole",
                Text = "Assign Role",
                Size = new Size(130, 38)
            };

            _removeRoleButton = new Button
            {
                Name = "btnRemoveRole",
                Text = "Remove Role",
                Size = new Size(150, 38)
            };

            _createUserButton.Click += (_, _) => ShowCreateUserDialog();
            _activateUserButton.Click += (_, _) => ActivateSelectedUser();
            _deactivateUserButton.Click += (_, _) => DeactivateSelectedUser();
            _assignRoleButton.Click += (_, _) => ShowAssignRoleDialog();
            _removeRoleButton.Click += (_, _) => ShowRemoveRoleDialog();

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
                Size = new Size(400, 200),
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
                Size = new Size(80, 35),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(280, 105),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.AddRange(new Control[]
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
                Size = new Size(400, 200),
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
                Size = new Size(90, 35),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(280, 105),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.AddRange(new Control[]
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

        private void ConfigureCustomersLayout()
        {
            var existingButton = flpNavigation.Controls
                .OfType<Button>()
                .FirstOrDefault(b => b.Name == "btnCustomers");

            if (existingButton != null)
            {
                flpNavigation.Controls.Remove(existingButton);
                existingButton.Dispose();
            }

            _btnCustomers = new Button
            {
                Text = "Customers",
                Name = "btnCustomers",
                Size = new Size(190, 42),
                Margin = new Padding(0, 5, 0, 5),
                BackColor = _normalButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnCustomers.FlatAppearance.BorderSize = 1;
            _btnCustomers.FlatAppearance.BorderColor = Color.White;
            _btnCustomers.Click += (_, _) =>
            {
                if (!RequirePermission("Customers.Manage"))
                    return;

                ShowPage(_pnlCustomers, "Customers", _btnCustomers);
                LoadCustomersManagement();
            };

            flpNavigation.Controls.Add(_btnCustomers);
            flpNavigation.Controls.SetChildIndex(_btnCustomers, 2);

            _pnlCustomers = new Panel
            {
                Name = "pnlCustomers",
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Bottom |
                         AnchorStyles.Left |
                         AnchorStyles.Right,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(20, 110),
                Size = new Size(1315, 597),
                Visible = false
            };
            pnlContent.Controls.Add(_pnlCustomers);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20),
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var headingLabel = new Label
            {
                Text = "Customers",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _txtCustomerSearch = new TextBox
            {
                Width = 240,
                PlaceholderText = "Search name, phone, or address...",
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 4, 10, 0)
            };

            _chkIncludeInactiveCustomers = new CheckBox
            {
                Text = "Include Inactive",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 7, 14, 0)
            };

            _btnAddCustomer = new Button
            {
                Text = "Add Customer",
                Size = new Size(125, 34),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAddCustomer.FlatAppearance.BorderSize = 0;

            _btnEditCustomer = new Button
            {
                Text = "Edit Customer",
                Size = new Size(125, 34),
                Margin = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            _btnRefreshCustomers = new Button
            {
                Text = "Refresh",
                Size = new Size(95, 34),
                Margin = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            toolbar.Controls.AddRange(new Control[]
            {
                _txtCustomerSearch,
                _chkIncludeInactiveCustomers,
                _btnAddCustomer,
                _btnEditCustomer,
                _btnRefreshCustomers
            });

            _customersGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };

            _customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = nameof(Customer.Id),
                Visible = false
            });
            _customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                DataPropertyName = nameof(Customer.Name),
                HeaderText = "Customer Name",
                FillWeight = 28
            });
            _customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PhoneNumber",
                DataPropertyName = nameof(Customer.PhoneNumber),
                HeaderText = "Phone",
                FillWeight = 20
            });
            _customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Address",
                DataPropertyName = nameof(Customer.Address),
                HeaderText = "Address",
                FillWeight = 36
            });
            _customersGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsActive",
                DataPropertyName = nameof(Customer.IsActive),
                HeaderText = "Active",
                FillWeight = 16
            });

            layout.Controls.Add(headingLabel, 0, 0);
            layout.Controls.Add(toolbar, 0, 1);
            layout.Controls.Add(_customersGrid, 0, 2);
            _pnlCustomers.Controls.Add(layout);

            _txtCustomerSearch.TextChanged += (_, _) => LoadCustomersManagement();
            _chkIncludeInactiveCustomers.CheckedChanged += (_, _) => LoadCustomersManagement();
            _btnRefreshCustomers.Click += (_, _) => LoadCustomersManagement();
            _btnAddCustomer.Click += (_, _) => ShowCustomerDialog(null);
            _btnEditCustomer.Click += (_, _) =>
            {
                var selected = GetSelectedCustomer();
                if (selected != null)
                    ShowCustomerDialog(selected);
            };
        }

        private void LoadCustomersManagement()
        {
            if (!HasPermission("Customers.Manage"))
                return;

            try
            {
                string? search = _txtCustomerSearch?.Text;
                bool includeInactive = _chkIncludeInactiveCustomers?.Checked ?? false;

                _customerRows = _customerManagementService.GetCustomers(search, includeInactive);
                _customersGrid.DataSource = null;
                _customersGrid.DataSource = _customerRows;

                if (_customersGrid.Columns["Id"] != null)
                    _customersGrid.Columns["Id"]!.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Unable to load customers: {ex.Message}",
                    "Customers",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private Customer? GetSelectedCustomer()
        {
            if (_customersGrid.CurrentRow?.DataBoundItem is Customer rowCustomer)
                return rowCustomer;

            if (_customersGrid.CurrentRow?.Cells["Id"]?.Value is int id)
                return _customerRows.FirstOrDefault(c => c.Id == id);

            MessageBox.Show(
                this,
                "Please select a customer first.",
                "Customers",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return null;
        }

        private void ShowCustomerDialog(Customer? customerToEdit)
        {
            if (!RequirePermission("Customers.Manage"))
                return;

            bool isEdit = customerToEdit != null;
            string title = isEdit ? "Edit Customer" : "Add Customer";

            using var dialog = new Form
            {
                Text = title,
                ClientSize = new Size(420, isEdit ? 310 : 275),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F)
            };

            var lblName = new Label { Text = "Customer Name *", Location = new Point(20, 18), AutoSize = true };
            var txtName = new TextBox
            {
                Location = new Point(20, 40),
                Width = 380,
                MaxLength = 150,
                Text = isEdit ? customerToEdit!.Name : string.Empty
            };

            var lblPhone = new Label { Text = "Phone Number", Location = new Point(20, 76), AutoSize = true };
            var txtPhone = new TextBox
            {
                Location = new Point(20, 98),
                Width = 380,
                MaxLength = 40,
                Text = isEdit ? (customerToEdit!.PhoneNumber ?? string.Empty) : string.Empty
            };

            var lblAddress = new Label { Text = "Address", Location = new Point(20, 134), AutoSize = true };
            var txtAddress = new TextBox
            {
                Location = new Point(20, 156),
                Width = 380,
                MaxLength = 250,
                Text = isEdit ? (customerToEdit!.Address ?? string.Empty) : string.Empty
            };

            CheckBox? chkActive = null;
            if (isEdit)
            {
                chkActive = new CheckBox
                {
                    Text = "Active Customer",
                    Location = new Point(20, 196),
                    AutoSize = true,
                    Checked = customerToEdit!.IsActive
                };
                dialog.Controls.Add(chkActive);
            }

            int btnY = isEdit ? 242 : 210;
            var btnSave = new Button
            {
                Text = "Save",
                Location = new Point(215, btnY),
                Size = new Size(90, 34),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(310, btnY),
                Size = new Size(90, 34),
                DialogResult = DialogResult.Cancel
            };

            btnSave.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show(
                        dialog,
                        "Customer name is required.",
                        title,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                try
                {
                    if (isEdit)
                    {
                        _customerManagementService.UpdateCustomer(
                            customerToEdit!.Id,
                            txtName.Text,
                            txtPhone.Text,
                            txtAddress.Text,
                            chkActive?.Checked ?? true);
                    }
                    else
                    {
                        _customerManagementService.AddCustomer(
                            txtName.Text,
                            txtPhone.Text,
                            txtAddress.Text);
                    }

                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        dialog,
                        ex.Message,
                        title,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

            dialog.Controls.AddRange(new Control[]
            {
                lblName, txtName,
                lblPhone, txtPhone,
                lblAddress, txtAddress,
                btnSave, btnCancel
            });

            dialog.AcceptButton = btnSave;
            dialog.CancelButton = btnCancel;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadCustomersManagement();
                LoadCustomers();
                MessageBox.Show(
                    this,
                    isEdit ? "Customer updated successfully." : "Customer added successfully.",
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void ConfigureSalesLayout()
        {
            var existingButton = flpNavigation.Controls
                .OfType<Button>()
                .FirstOrDefault(b => b.Name == "btnSales");

            if (existingButton != null)
            {
                flpNavigation.Controls.Remove(existingButton);
                existingButton.Dispose();
            }

            _btnSales = new Button
            {
                Text = "Sales / POS",
                Name = "btnSales",
                Size = new Size(190, 42),
                Margin = new Padding(0, 5, 0, 5),
                BackColor = _normalButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnSales.FlatAppearance.BorderSize = 1;
            _btnSales.FlatAppearance.BorderColor = Color.White;
            _btnSales.Click += (_, _) => OpenSalesPage();

            flpNavigation.Controls.Add(_btnSales);
            flpNavigation.Controls.SetChildIndex(_btnSales, 1);

            _pnlSales = new Panel
            {
                Name = "pnlSales",
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Bottom |
                         AnchorStyles.Left |
                         AnchorStyles.Right,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(20, 110),
                Size = new Size(1315, 597),
                Visible = false
            };
            pnlContent.Controls.Add(_pnlSales);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(16),
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            var entryRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _txtPosBarcode = new TextBox
            {
                Width = 230,
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Scan or enter barcode...",
                Margin = new Padding(0, 4, 10, 0)
            };
            _txtPosBarcode.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AddPosItemByBarcode();
                }
            };

            _nudPosQuantity = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100000,
                Value = 1,
                Width = 85,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 4, 10, 0)
            };

            var btnAddItem = new Button
            {
                Text = "Add Item",
                Size = new Size(110, 34),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddItem.FlatAppearance.BorderSize = 0;
            btnAddItem.Click += (_, _) => AddPosItemByBarcode();

            var btnRemoveItem = new Button
            {
                Text = "Remove Item",
                Size = new Size(120, 34),
                Margin = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnRemoveItem.Click += (_, _) => RemoveSelectedPosItem();

            var btnFindProduct = new Button
            {
                Text = "Find Product",
                Size = new Size(125, 34),
                Margin = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnFindProduct.Click += (_, _) => FindPosProduct();

            var btnNewProduct = new Button
            {
                Text = "New Product",
                Size = new Size(120, 34),
                Margin = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNewProduct.FlatAppearance.BorderSize = 0;
            btnNewProduct.Click += (_, _) => RegisterPosProduct();

            entryRow.Controls.AddRange(new Control[]
            {
                new Label { Text = "Barcode:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) },
                _txtPosBarcode,
                new Label { Text = "Qty:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) },
                _nudPosQuantity,
                btnAddItem,
                btnFindProduct,
                btnNewProduct,
                btnRemoveItem
            });

            _dgvPosCart = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            _dgvPosCart.Columns.Add("Barcode", "Barcode");
            _dgvPosCart.Columns.Add("Product", "Product");
            _dgvPosCart.Columns.Add("Quantity", "Qty");
            _dgvPosCart.Columns.Add("UnitPrice", "Unit Price");
            _dgvPosCart.Columns.Add("LineTotal", "Line Total");

            var paymentRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 8, 0, 0)
            };

            _lblPosSubtotalGct = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 8, 16, 0),
                Text = $"Subtotal: {FormatConfiguredCurrency(0m)}   GCT (15%): {FormatConfiguredCurrency(0m)}"
            };

            _lblPosTotal = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(0, 4, 20, 0),
                Text = $"Total: {FormatConfiguredCurrency(0m)}"
            };

            _txtPosCashReceived = new TextBox
            {
                Width = 110,
                Font = new Font("Segoe UI", 10F),
                Text = "0.00",
                Margin = new Padding(0, 4, 10, 0)
            };

            var btnCashSale = new Button
            {
                Text = "Cash Sale",
                Size = new Size(120, 35),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCashSale.FlatAppearance.BorderSize = 0;
            btnCashSale.Click += (_, _) => CompletePosCashSale();

            var btnCardSale = new Button
            {
                Text = "Card Sale",
                Size = new Size(120, 35),
                Margin = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCardSale.FlatAppearance.BorderSize = 0;
            btnCardSale.Click += (_, _) => CompletePosCardSale();

            var btnCancelSale = new Button
            {
                Text = "Cancel Sale",
                Size = new Size(115, 35),
                Margin = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnCancelSale.Click += (_, _) => CancelPosSale();

            _btnPosPrintReceipt = new Button
            {
                Text = "Last Receipt",
                Size = new Size(120, 35),
                Margin = new Padding(8, 0, 0, 0),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            _btnPosPrintReceipt.Click += (_, _) => ShowPosReceipt();

            paymentRow.Controls.AddRange(new Control[]
            {
                _lblPosSubtotalGct,
                _lblPosTotal,
                new Label { Text = "Cash:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) },
                _txtPosCashReceived,
                btnCashSale,
                btnCardSale,
                btnCancelSale,
                _btnPosPrintReceipt
            });

            _lblPosStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(31, 41, 55),
                Text = "Ready. Scan or enter a barcode."
            };

            layout.Controls.Add(entryRow, 0, 0);
            layout.Controls.Add(_dgvPosCart, 0, 1);
            layout.Controls.Add(paymentRow, 0, 2);
            layout.Controls.Add(_lblPosStatus, 0, 3);
            _pnlSales.Controls.Add(layout);

            RefreshPosCartDisplay();
        }

        private void OpenSalesPage()
        {
            if (!RequirePermission("Sales.Process"))
                return;

            ShowPage(_pnlSales, "Point of Sale", _btnSales);
            RefreshPosCartDisplay();
            _txtPosBarcode.Focus();
        }

        private void RemoveSelectedPosItem()
        {
            if (_dgvPosCart.CurrentRow == null)
            {
                ShowPosError("Please select a cart item to remove.");
                return;
            }

            string? barcode = _dgvPosCart.CurrentRow.Cells["Barcode"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(barcode))
                return;

            var product = _regularSaleService.FindProduct(barcode);
            if (product == null)
                return;

            int removed = _posCart.RemoveAll(line => line.ProductId == product.Id);
            if (removed > 0)
            {
                RefreshPosCartDisplay();
                _lblPosStatus.Text = $"Removed {product.Name} from cart.";
                _txtPosBarcode.Focus();
            }
        }

        private void AddPosItemByBarcode()
        {
            try
            {
                var product = _regularSaleService.FindProduct(_txtPosBarcode.Text)
                    ?? throw new InvalidOperationException("No product found for that barcode.");

                AddPosProductToCart(product.Id);
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is InvalidOperationException ||
                ex is OverflowException)
            {
                ShowPosError(ex.Message);
            }
        }

        private void AddPosProductToCart(int productId)
        {
            try
            {
                var product = _regularSaleService.FindProductById(productId)
                    ?? throw new InvalidOperationException("Product not found.");

                int quantity = (int)_nudPosQuantity.Value;
                int alreadyInCart = _posCart
                    .Where(i => i.ProductId == product.Id)
                    .Sum(i => i.Quantity);

                _regularSaleService.CheckQuantity(product.Id, quantity, alreadyInCart);
                _posCart.Add(new SaleRequestLine(product.Id, quantity));

                RefreshPosCartDisplay();
                _lblPosStatus.Text = $"Added {quantity} x {product.Name}";
                _txtPosBarcode.Clear();
                _nudPosQuantity.Value = 1;
                _txtPosBarcode.Focus();
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is InvalidOperationException ||
                ex is OverflowException)
            {
                ShowPosError(ex.Message);
            }
        }

        private void RefreshPosCartDisplay()
        {
            _dgvPosCart.Rows.Clear();

            decimal total = 0m;

            foreach (var group in _posCart.GroupBy(i => i.ProductId))
            {
                var product = _regularSaleService.FindProductById(group.Key);
                if (product == null)
                    continue;

                int quantity = group.Sum(i => i.Quantity);
                decimal lineTotal = Math.Round(
                    product.SellingPrice * quantity,
                    2,
                    MidpointRounding.AwayFromZero);

                total += lineTotal;

                _dgvPosCart.Rows.Add(
                    product.Barcode,
                    product.Name,
                    quantity,
                    product.SellingPrice.ToString("N2"),
                    lineTotal.ToString("N2"));
            }

            total = Math.Round(total, 2, MidpointRounding.AwayFromZero);

            _lblPosSubtotalGct.Text = $"Items: {_posCart.Sum(i => i.Quantity)}";
            _lblPosTotal.Text = $"Total: {FormatConfiguredCurrency(total)}";
        }

        private void FindPosProduct()
        {
            var matches = _inventoryManagementService.GetInventory();
            if (matches.Count == 0)
            {
                ShowPosError("No products available in inventory.");
                return;
            }

            using var picker = new Form
            {
                Text = "Find Product",
                ClientSize = new Size(680, 440),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F)
            };

            var txtFilter = new TextBox
            {
                Location = new Point(16, 16),
                Width = 648,
                PlaceholderText = "Type product name or barcode..."
            };

            var grid = new DataGridView
            {
                Location = new Point(16, 56),
                Size = new Size(648, 315),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InventoryRow.Barcode), HeaderText = "Barcode", FillWeight = 22 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InventoryRow.ProductName), HeaderText = "Product", FillWeight = 42 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InventoryRow.SellingPrice), HeaderText = "Price", FillWeight = 18, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InventoryRow.AvailableStock), HeaderText = "In Stock", FillWeight = 18 });

            int? selectedProductId = null;

            void BindRows()
            {
                string term = txtFilter.Text.Trim();
                grid.DataSource = string.IsNullOrWhiteSpace(term)
                    ? matches.ToList()
                    : matches.Where(p =>
                            p.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            txtFilter.TextChanged += (_, _) => BindRows();
            BindRows();

            var btnSelect = new Button
            {
                Text = "Add Selected",
                Location = new Point(450, 384),
                Size = new Size(110, 36),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSelect.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(568, 384),
                Size = new Size(96, 36),
                DialogResult = DialogResult.Cancel
            };

            void ConfirmSelection()
            {
                if (grid.CurrentRow?.DataBoundItem is InventoryRow row)
                {
                    selectedProductId = row.ProductId;
                    picker.DialogResult = DialogResult.OK;
                    picker.Close();
                }
            }

            btnSelect.Click += (_, _) => ConfirmSelection();
            grid.CellDoubleClick += (_, _) => ConfirmSelection();

            picker.Controls.AddRange(new Control[] { txtFilter, grid, btnSelect, btnCancel });
            picker.AcceptButton = btnSelect;
            picker.CancelButton = btnCancel;

            if (picker.ShowDialog(this) == DialogResult.OK && selectedProductId.HasValue)
            {
                AddPosProductToCart(selectedProductId.Value);
            }

            _txtPosBarcode.Focus();
        }

        private void RegisterPosProduct()
        {
            ShowProductDialog(productToEdit: null);
            _txtPosBarcode.Focus();
        }

        private void CompletePosCardSale()
        {
            var cashier = _userSession.CurrentUser;
            if (cashier == null)
            {
                ShowPosError("No active cashier session.");
                return;
            }

            if (_posCart.Count == 0)
            {
                ShowPosError("Please add at least one item.");
                return;
            }

            decimal total;
            try
            {
                total = _regularSaleService.CalculateTotal(_posCart);
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is InvalidOperationException ||
                ex is OverflowException)
            {
                ShowPosError(ex.Message);
                return;
            }

            using var cardDialog = new Form
            {
                Text = "Card Payment",
                ClientSize = new Size(410, 265),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F)
            };

            var lblAmount = new Label
            {
                Text = $"Amount to charge: {FormatConfiguredCurrency(total)}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(20, 16),
                AutoSize = true
            };

            var cmbCardType = new ComboBox
            {
                Location = new Point(150, 58),
                Width = 235,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCardType.Items.AddRange(RegularSaleService.CardTypes.Cast<object>().ToArray());
            cmbCardType.SelectedIndex = 0;

            var txtLastFour = new TextBox
            {
                Location = new Point(150, 98),
                Width = 235,
                MaxLength = 4
            };
            txtLastFour.KeyPress += (_, e) =>
            {
                e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
            };

            var txtApproval = new TextBox
            {
                Location = new Point(150, 138),
                Width = 235,
                MaxLength = 12,
                CharacterCasing = CharacterCasing.Upper
            };

            var btnConfirm = new Button
            {
                Text = "Complete Sale",
                Location = new Point(175, 196),
                Size = new Size(115, 36),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnConfirm.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(298, 196),
                Size = new Size(87, 36),
                DialogResult = DialogResult.Cancel
            };

            btnConfirm.Click += (_, _) =>
            {
                if (txtLastFour.Text.Trim().Length != 4)
                {
                    MessageBox.Show(cardDialog, "Enter the last 4 digits of the card.", "Card Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastFour.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtApproval.Text))
                {
                    MessageBox.Show(cardDialog, "Enter the approval code from the terminal.", "Card Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtApproval.Focus();
                    return;
                }

                cardDialog.DialogResult = DialogResult.OK;
                cardDialog.Close();
            };

            cardDialog.Controls.AddRange(new Control[]
            {
                lblAmount,
                new Label { Text = "Card Type:", Location = new Point(20, 62), AutoSize = true },
                cmbCardType,
                new Label { Text = "Last 4 Digits:", Location = new Point(20, 102), AutoSize = true },
                txtLastFour,
                new Label { Text = "Approval Code:", Location = new Point(20, 142), AutoSize = true },
                txtApproval,
                btnConfirm,
                btnCancel
            });

            cardDialog.AcceptButton = btnConfirm;
            cardDialog.CancelButton = btnCancel;

            if (cardDialog.ShowDialog(this) != DialogResult.OK)
            {
                _lblPosStatus.Text = "Card payment cancelled. The sale is still open.";
                return;
            }

            try
            {
                var sale = _regularSaleService.CompleteCardSale(
                    _posCart,
                    cmbCardType.SelectedItem?.ToString() ?? RegularSaleService.CardTypes[0],
                    txtLastFour.Text.Trim(),
                    txtApproval.Text.Trim(),
                    cashier.Id,
                    cashier.Username);

                FinishPosSale(
                    sale,
                    $"Sale #{sale.Id} complete. Card approved: {sale.ApprovalCode}");
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is InvalidOperationException ||
                ex is OverflowException)
            {
                ShowPosError(ex.Message);
            }
        }

        private void ShowPosReceipt()
        {
            if (string.IsNullOrWhiteSpace(_lastPosReceipt))
                return;

            using var receiptDialog = new Form
            {
                Text = "Receipt",
                ClientSize = new Size(420, 520),
                StartPosition = FormStartPosition.CenterParent,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F)
            };

            var txtReceiptBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font(DrawingFontFamily.GenericMonospace, 10.5F),
                Text = _lastPosReceipt
            };

            var buttonBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8, 6, 8, 6),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(90, 32),
                DialogResult = DialogResult.OK
            };

            var btnPrint = new Button
            {
                Text = "Print",
                Size = new Size(90, 32),
                BackColor = _selectedButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPrint.FlatAppearance.BorderSize = 0;

            btnPrint.Click += (_, _) =>
            {
                try
                {
                    using var printDoc = new PrintDocument();
                    using var printFont = new Font(DrawingFontFamily.GenericMonospace, 10F);

                    printDoc.PrintPage += (_, args) =>
                    {
                        if (args.Graphics == null)
                            return;

                        args.Graphics.DrawString(
                            _lastPosReceipt,
                            printFont,
                            Brushes.Black,
                            args.MarginBounds);
                        args.HasMorePages = false;
                    };

                    using var printDialog = new PrintDialog
                    {
                        Document = printDoc,
                        UseEXDialog = true
                    };

                    if (printDialog.ShowDialog(receiptDialog) == DialogResult.OK)
                    {
                        printDoc.Print();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        receiptDialog,
                        $"Unable to print receipt: {ex.Message}",
                        "Print Receipt",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            buttonBar.Controls.Add(btnClose);
            buttonBar.Controls.Add(btnPrint);

            receiptDialog.Controls.Add(txtReceiptBox);
            receiptDialog.Controls.Add(buttonBar);
            receiptDialog.CancelButton = btnClose;
            receiptDialog.ShowDialog(this);
        }

        private string BuildConfiguredReceipt(RegularSale sale)
        {
            BusinessSettings? settings = null;
            try
            {
                settings = _businessSettingsService.GetSettings();
            }
            catch
            {
                // Fall back to defaults if settings aren't readable
            }

            string businessName = string.IsNullOrWhiteSpace(settings?.BusinessName)
                ? "K&K WHOLESALE"
                : settings!.BusinessName.Trim();

            string dateFormat = GetConfiguredDateFormat();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine(businessName);

            if (settings?.ShowBusinessContactDetails == true)
            {
                if (!string.IsNullOrWhiteSpace(settings.BusinessAddress))
                    sb.AppendLine(settings.BusinessAddress.Trim());

                if (!string.IsNullOrWhiteSpace(settings.PhoneNumber))
                    sb.AppendLine($"Tel: {settings.PhoneNumber.Trim()}");

                if (!string.IsNullOrWhiteSpace(settings.EmailAddress))
                    sb.AppendLine($"Email: {settings.EmailAddress.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(settings?.ReceiptHeader))
            {
                sb.AppendLine("--------------------------------");
                sb.AppendLine(settings!.ReceiptHeader.Trim());
            }

            sb.AppendLine("--------------------------------");
            sb.AppendLine("REGULAR SALE RECEIPT");
            sb.AppendLine($"Sale #: {sale.Id}");
            sb.AppendLine($"Date: {sale.CompletedAt.ToLocalTime().ToString($"{dateFormat} HH:mm")}");
            sb.AppendLine($"Cashier: {sale.CashierName}");
            sb.AppendLine("--------------------------------");

            foreach (var line in sale.Lines)
            {
                sb.AppendLine(line.ProductName);
                sb.AppendLine(
                    $"  {line.Quantity} x {FormatConfiguredCurrency(line.UnitPrice)} = {FormatConfiguredCurrency(line.LineTotal)}");
            }

            sb.AppendLine("--------------------------------");
            sb.AppendLine($"TOTAL: {FormatConfiguredCurrency(sale.Total)}");

            if (sale.PaymentMethod == "Card")
            {
                sb.AppendLine($"CARD ({sale.CardType}): {FormatConfiguredCurrency(sale.CashReceived)}");
                sb.AppendLine($"Card No: **** **** **** {sale.CardLastFour}");
                sb.AppendLine($"Approval Code: {sale.ApprovalCode}");
            }
            else
            {
                sb.AppendLine($"CASH: {FormatConfiguredCurrency(sale.CashReceived)}");
                sb.AppendLine($"CHANGE: {FormatConfiguredCurrency(sale.ChangeDue)}");
            }

            sb.AppendLine("--------------------------------");

            string footer = string.IsNullOrWhiteSpace(settings?.ReceiptFooter)
                ? "Thank you for shopping with us!"
                : settings!.ReceiptFooter.Trim();
            sb.AppendLine(footer);

            return sb.ToString();
        }

        private void CompletePosCashSale()
        {
            var cashier = _userSession.CurrentUser;
            if (cashier == null)
            {
                ShowPosError("No active cashier session.");
                return;
            }

            if (!decimal.TryParse(
                    _txtPosCashReceived.Text,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.CurrentCulture,
                    out decimal cashReceived) &&
                !decimal.TryParse(
                    _txtPosCashReceived.Text,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out cashReceived))
            {
                ShowPosError("Please enter a valid cash amount.");
                return;
            }

            try
            {
                var sale = _regularSaleService.CompleteCashSale(
                    _posCart,
                    cashReceived,
                    cashier.Id,
                    cashier.Username);

                FinishPosSale(
                    sale,
                    $"Sale #{sale.Id} complete. Change: {FormatConfiguredCurrency(sale.ChangeDue)}");
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is InvalidOperationException ||
                ex is OverflowException)
            {
                ShowPosError(ex.Message);
            }
        }

        private void FinishPosSale(RegularSale sale, string statusMessage)
        {
            _lastPosReceipt = BuildConfiguredReceipt(sale);
            _posCart.Clear();
            _txtPosCashReceived.Text = "0.00";
            _btnPosPrintReceipt.Enabled = true;

            RefreshPosCartDisplay();
            _lblPosStatus.Text = statusMessage;
            ShowPosReceipt();
            _txtPosBarcode.Focus();
        }

        private void CancelPosSale()
        {
            _posCart.Clear();
            _txtPosCashReceived.Text = "0.00";
            RefreshPosCartDisplay();
            _lblPosStatus.Text = "Sale cancelled. Nothing was saved.";
            _txtPosBarcode.Focus();
        }

        private void ShowPosError(string message)
        {
            _lblPosStatus.Text = message;
            MessageBox.Show(
                this,
                message,
                "Regular Sale",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private bool HasPermission(string permissionName)
        {
            var currentUser = _userSession?.CurrentUser;

            return currentUser != null &&
                   _authorizationService != null &&
                   _authorizationService.HasPermission(
                       currentUser,
                       permissionName);
        }

        private bool CanAccessPage(string pageTitle)
        {
            return pageTitle switch
            {
                "Dashboard" => HasPermission("Dashboard.View"),
                "Inventory Management" => HasPermission("Inventory.View"),
                "Reports" => HasPermission("Reports.View"),
                "Consignments" =>
                    HasPermission("Consignments.Issue") ||
                    HasPermission("Consignments.Return"),
                "User Management" =>
                    HasPermission("Users.Manage") ||
                    HasPermission("Roles.Manage") ||
                    HasPermission("Permissions.Manage") ||
                    HasPermission("AuditLog.View"),
                "Credit Management" => HasPermission("Credit.Manage"),
                "Settings" => HasPermission("Settings.Manage"),
                "Customers" => HasPermission("Customers.Manage"),
                "Point of Sale" => HasPermission("Sales.Process"),
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
                Size = new Size(450, 250),
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
                Size = new Size(85, 35),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(285, 165),
                Size = new Size(85, 35),
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.AddRange(new Control[]
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

        private static W.Paragraph CreateWordParagraph(
    string text,
    bool bold,
    string fontSize)
        {
            W.RunProperties runProperties = new();

            if (bold)
                runProperties.Append(new W.Bold());

            runProperties.Append(new W.FontSize
            {
                Val = fontSize
            });

            W.Run run = new(
                runProperties,
                new W.Text(text)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });

            return new W.Paragraph(run);
        }

        private static TableCell CreateWordCell(
            string text,
            bool bold = false)
        {
            W.RunProperties runProperties = new();

            if (bold)
                runProperties.Append(new W.Bold());

            W.Run run = new(
                runProperties,
                new W.Text(text)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });

            W.Paragraph paragraph = new(run);

            return new W.TableCell(
                paragraph,
                new W.TableCellProperties(
                    new W.TableCellVerticalAlignment
                    {
                        Val = W.TableVerticalAlignmentValues.Center
                    }));
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container
                .Background(PdfColors.Grey.Lighten2)
                .Border(1)
                .Padding(5);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(PdfColors.Grey.Lighten2)
                .Padding(5);
        }
    }
}