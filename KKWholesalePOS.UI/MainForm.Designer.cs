namespace KKWholesalePOS.UI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            pnlHeader = new Panel();
            lblAppName = new Label();
            lblCurrentUser = new Label();
            btnLogout = new Button();
            pnlSidebar = new Panel();
            flpNavigation = new FlowLayoutPanel();
            btnDashboard = new Button();
            btnProducts = new Button();
            btnCustomers = new Button();
            btnSales = new Button();
            btnConsignments = new Button();
            btnReports = new Button();
            btnUserManagement = new Button();
            btnSettings = new Button();
            lblNavigationTitle = new Label();
            pnlContent = new Panel();
            lblPageTitle = new Label();
            lblWelcome = new Label();
            pnlReports = new Panel();
            pnlReportFilters = new Panel();
            cmbCustomer = new ComboBox();
            lblCustomer = new Label();
            lblReportType = new Label();
            cmbReportType = new ComboBox();
            lblStartDate = new Label();
            dtpStartDate = new DateTimePicker();
            lblEndDate = new Label();
            dtpEndDate = new DateTimePicker();
            btnGenerateReport = new Button();
            lblReportsPlaceholder = new Label();
            dgvReportResults = new DataGridView();
            lblReportSummary = new Label();
            btnPrintReport = new Button();
            btnExportPdf = new Button();
            btnExportExcel = new Button();
            btnExportWord = new Button();
            pnlUserManagement = new Panel();
            lblUserManagementPlaceholder = new Label();
            pnlDashboard = new Panel();
            tlpDashboardCards = new TableLayoutPanel();
            pnlTotalProducts = new Panel();
            lblTotalProductsValue = new Label();
            lblTotalProductsTitle = new Label();
            pnlActiveCustomers = new Panel();
            lblActiveCustomersValue = new Label();
            lblActiveCustomersTitle = new Label();
            pnlSalesToday = new Panel();
            lblSalesTodayValue = new Label();
            lblSalesTodayTitle = new Label();
            pnlPendingConsignments = new Panel();
            lblPendingConsignmentsValue = new Label();
            lblPendingConsignmentsTitle = new Label();
            pnlSettings = new Panel();
            lblSettingsPlaceholder = new Label();
            statusStripMain = new StatusStrip();
            tsslSessionStatus = new ToolStripStatusLabel();
            tsslDateTime = new ToolStripStatusLabel();
            pnlHeader.SuspendLayout();
            pnlSidebar.SuspendLayout();
            flpNavigation.SuspendLayout();
            pnlContent.SuspendLayout();
            pnlReports.SuspendLayout();
            pnlReportFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReportResults).BeginInit();
            pnlUserManagement.SuspendLayout();
            pnlDashboard.SuspendLayout();
            tlpDashboardCards.SuspendLayout();
            pnlTotalProducts.SuspendLayout();
            pnlActiveCustomers.SuspendLayout();
            pnlSalesToday.SuspendLayout();
            pnlPendingConsignments.SuspendLayout();
            pnlSettings.SuspendLayout();
            statusStripMain.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(31, 41, 55);
            pnlHeader.Controls.Add(lblAppName);
            pnlHeader.Controls.Add(lblCurrentUser);
            pnlHeader.Controls.Add(btnLogout);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1578, 65);
            pnlHeader.TabIndex = 0;
            // 
            // lblAppName
            // 
            lblAppName.AutoSize = true;
            lblAppName.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAppName.ForeColor = Color.White;
            lblAppName.Location = new Point(20, 17);
            lblAppName.Name = "lblAppName";
            lblAppName.Size = new Size(323, 45);
            lblAppName.TabIndex = 0;
            lblAppName.Text = "K&&K Wholesale POS";
            // 
            // lblCurrentUser
            // 
            lblCurrentUser.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblCurrentUser.AutoSize = true;
            lblCurrentUser.ForeColor = Color.White;
            lblCurrentUser.Location = new Point(1232, 16);
            lblCurrentUser.Name = "lblCurrentUser";
            lblCurrentUser.Size = new Size(226, 25);
            lblCurrentUser.TabIndex = 1;
            lblCurrentUser.Text = "Signed in as: Administrator";
            // 
            // btnLogout
            // 
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.BackColor = Color.FromArgb(55, 65, 81);
            btnLogout.Cursor = Cursors.Hand;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.ForeColor = Color.White;
            btnLogout.Location = new Point(1464, 12);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(90, 32);
            btnLogout.TabIndex = 2;
            btnLogout.Text = "Logout";
            btnLogout.UseVisualStyleBackColor = false;
            // 
            // pnlSidebar
            // 
            pnlSidebar.BackColor = Color.FromArgb(17, 24, 39);
            pnlSidebar.Controls.Add(flpNavigation);
            pnlSidebar.Controls.Add(lblNavigationTitle);
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Location = new Point(0, 65);
            pnlSidebar.Name = "pnlSidebar";
            pnlSidebar.Size = new Size(220, 779);
            pnlSidebar.TabIndex = 1;
            // 
            // flpNavigation
            // 
            flpNavigation.AutoScroll = true;
            flpNavigation.Controls.Add(btnDashboard);
            flpNavigation.Controls.Add(btnProducts);
            flpNavigation.Controls.Add(btnCustomers);
            flpNavigation.Controls.Add(btnSales);
            flpNavigation.Controls.Add(btnConsignments);
            flpNavigation.Controls.Add(btnReports);
            flpNavigation.Controls.Add(btnUserManagement);
            flpNavigation.Controls.Add(btnSettings);
            flpNavigation.Dock = DockStyle.Fill;
            flpNavigation.FlowDirection = FlowDirection.TopDown;
            flpNavigation.Location = new Point(0, 45);
            flpNavigation.Name = "flpNavigation";
            flpNavigation.Padding = new Padding(10, 15, 10, 10);
            flpNavigation.Size = new Size(220, 734);
            flpNavigation.TabIndex = 1;
            flpNavigation.WrapContents = false;
            // 
            // btnDashboard
            // 
            btnDashboard.BackColor = Color.FromArgb(31, 41, 55);
            btnDashboard.Cursor = Cursors.Hand;
            btnDashboard.FlatStyle = FlatStyle.Flat;
            btnDashboard.ForeColor = Color.White;
            btnDashboard.Location = new Point(10, 20);
            btnDashboard.Margin = new Padding(0, 5, 0, 5);
            btnDashboard.Name = "btnDashboard";
            btnDashboard.Size = new Size(190, 42);
            btnDashboard.TabIndex = 0;
            btnDashboard.Text = "Dashboard";
            btnDashboard.TextAlign = ContentAlignment.MiddleLeft;
            btnDashboard.UseVisualStyleBackColor = false;
            // 
            // btnProducts
            // 
            btnProducts.BackColor = Color.FromArgb(31, 41, 55);
            btnProducts.Cursor = Cursors.Hand;
            btnProducts.FlatStyle = FlatStyle.Flat;
            btnProducts.ForeColor = Color.White;
            btnProducts.Location = new Point(10, 72);
            btnProducts.Margin = new Padding(0, 5, 0, 5);
            btnProducts.Name = "btnProducts";
            btnProducts.Size = new Size(190, 42);
            btnProducts.TabIndex = 1;
            btnProducts.Text = "Products";
            btnProducts.TextAlign = ContentAlignment.MiddleLeft;
            btnProducts.UseVisualStyleBackColor = false;
            // 
            // btnCustomers
            // 
            btnCustomers.BackColor = Color.FromArgb(31, 41, 55);
            btnCustomers.Cursor = Cursors.Hand;
            btnCustomers.FlatStyle = FlatStyle.Flat;
            btnCustomers.ForeColor = Color.White;
            btnCustomers.Location = new Point(10, 124);
            btnCustomers.Margin = new Padding(0, 5, 0, 5);
            btnCustomers.Name = "btnCustomers";
            btnCustomers.Size = new Size(190, 42);
            btnCustomers.TabIndex = 2;
            btnCustomers.Text = "Customers";
            btnCustomers.TextAlign = ContentAlignment.MiddleLeft;
            btnCustomers.UseVisualStyleBackColor = false;
            // 
            // btnSales
            // 
            btnSales.BackColor = Color.FromArgb(31, 41, 55);
            btnSales.Cursor = Cursors.Hand;
            btnSales.FlatStyle = FlatStyle.Flat;
            btnSales.ForeColor = Color.White;
            btnSales.Location = new Point(10, 176);
            btnSales.Margin = new Padding(0, 5, 0, 5);
            btnSales.Name = "btnSales";
            btnSales.Size = new Size(190, 42);
            btnSales.TabIndex = 3;
            btnSales.Text = "Sales";
            btnSales.TextAlign = ContentAlignment.MiddleLeft;
            btnSales.UseVisualStyleBackColor = false;
            // 
            // btnConsignments
            // 
            btnConsignments.BackColor = Color.FromArgb(31, 41, 55);
            btnConsignments.Cursor = Cursors.Hand;
            btnConsignments.FlatStyle = FlatStyle.Flat;
            btnConsignments.ForeColor = Color.White;
            btnConsignments.Location = new Point(10, 228);
            btnConsignments.Margin = new Padding(0, 5, 0, 5);
            btnConsignments.Name = "btnConsignments";
            btnConsignments.Size = new Size(190, 42);
            btnConsignments.TabIndex = 4;
            btnConsignments.Text = "Consigments";
            btnConsignments.TextAlign = ContentAlignment.MiddleLeft;
            btnConsignments.UseVisualStyleBackColor = false;
            // 
            // btnReports
            // 
            btnReports.BackColor = Color.FromArgb(31, 41, 55);
            btnReports.Cursor = Cursors.Hand;
            btnReports.FlatStyle = FlatStyle.Flat;
            btnReports.ForeColor = Color.White;
            btnReports.Location = new Point(10, 280);
            btnReports.Margin = new Padding(0, 5, 0, 5);
            btnReports.Name = "btnReports";
            btnReports.Size = new Size(190, 42);
            btnReports.TabIndex = 5;
            btnReports.Text = "Reports";
            btnReports.TextAlign = ContentAlignment.MiddleLeft;
            btnReports.UseVisualStyleBackColor = false;
            // 
            // btnUserManagement
            // 
            btnUserManagement.BackColor = Color.FromArgb(31, 41, 55);
            btnUserManagement.Cursor = Cursors.Hand;
            btnUserManagement.FlatStyle = FlatStyle.Flat;
            btnUserManagement.ForeColor = Color.White;
            btnUserManagement.Location = new Point(10, 332);
            btnUserManagement.Margin = new Padding(0, 5, 0, 5);
            btnUserManagement.Name = "btnUserManagement";
            btnUserManagement.Size = new Size(190, 42);
            btnUserManagement.TabIndex = 6;
            btnUserManagement.Text = "User Management";
            btnUserManagement.TextAlign = ContentAlignment.MiddleLeft;
            btnUserManagement.UseVisualStyleBackColor = false;
            // 
            // btnSettings
            // 
            btnSettings.BackColor = Color.FromArgb(31, 41, 55);
            btnSettings.Cursor = Cursors.Hand;
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.ForeColor = Color.White;
            btnSettings.Location = new Point(10, 384);
            btnSettings.Margin = new Padding(0, 5, 0, 5);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(190, 42);
            btnSettings.TabIndex = 7;
            btnSettings.Text = "Settings";
            btnSettings.TextAlign = ContentAlignment.MiddleLeft;
            btnSettings.UseVisualStyleBackColor = false;
            // 
            // lblNavigationTitle
            // 
            lblNavigationTitle.Dock = DockStyle.Top;
            lblNavigationTitle.Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblNavigationTitle.ForeColor = Color.FromArgb(156, 163, 175);
            lblNavigationTitle.Location = new Point(0, 0);
            lblNavigationTitle.Name = "lblNavigationTitle";
            lblNavigationTitle.Padding = new Padding(10, 0, 0, 0);
            lblNavigationTitle.Size = new Size(220, 45);
            lblNavigationTitle.TabIndex = 0;
            lblNavigationTitle.Text = "MAIN MENU";
            lblNavigationTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pnlContent
            // 
            pnlContent.BackColor = Color.FromArgb(243, 244, 246);
            pnlContent.Controls.Add(lblPageTitle);
            pnlContent.Controls.Add(lblWelcome);
            pnlContent.Controls.Add(pnlReports);
            pnlContent.Controls.Add(pnlUserManagement);
            pnlContent.Controls.Add(pnlDashboard);
            pnlContent.Controls.Add(pnlSettings);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(220, 65);
            pnlContent.Name = "pnlContent";
            pnlContent.Padding = new Padding(20);
            pnlContent.Size = new Size(1358, 779);
            pnlContent.TabIndex = 2;
            // 
            // lblPageTitle
            // 
            lblPageTitle.AutoSize = true;
            lblPageTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPageTitle.ForeColor = Color.FromArgb(17, 24, 39);
            lblPageTitle.Location = new Point(20, 20);
            lblPageTitle.Name = "lblPageTitle";
            lblPageTitle.Size = new Size(229, 54);
            lblPageTitle.TabIndex = 8;
            lblPageTitle.Text = "Dashboard";
            // 
            // lblWelcome
            // 
            lblWelcome.AutoSize = true;
            lblWelcome.BackColor = Color.FromArgb(243, 244, 246);
            lblWelcome.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblWelcome.ForeColor = Color.FromArgb(107, 114, 128);
            lblWelcome.Location = new Point(20, 75);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Size = new Size(299, 28);
            lblWelcome.TabIndex = 9;
            lblWelcome.Text = "Welcome to K&&K Wholesale POS";
            // 
            // pnlReports
            // 
            pnlReports.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlReports.BackColor = Color.White;
            pnlReports.BorderStyle = BorderStyle.FixedSingle;
            pnlReports.Controls.Add(pnlReportFilters);
            pnlReports.Controls.Add(lblReportsPlaceholder);
            pnlReports.Controls.Add(dgvReportResults);
            pnlReports.Controls.Add(lblReportSummary);
            pnlReports.Controls.Add(btnPrintReport);
            pnlReports.Controls.Add(btnExportPdf);
            pnlReports.Controls.Add(btnExportExcel);
            pnlReports.Controls.Add(btnExportWord);
            pnlReports.Location = new Point(20, 110);
            pnlReports.Name = "pnlReports";
            pnlReports.Size = new Size(1315, 597);
            pnlReports.TabIndex = 11;
            pnlReports.Visible = false;
            // 
            // pnlReportFilters
            // 
            pnlReportFilters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlReportFilters.BackColor = Color.FromArgb(249, 250, 251);
            pnlReportFilters.BorderStyle = BorderStyle.FixedSingle;
            pnlReportFilters.Controls.Add(cmbCustomer);
            pnlReportFilters.Controls.Add(lblCustomer);
            pnlReportFilters.Controls.Add(lblReportType);
            pnlReportFilters.Controls.Add(cmbReportType);
            pnlReportFilters.Controls.Add(lblStartDate);
            pnlReportFilters.Controls.Add(dtpStartDate);
            pnlReportFilters.Controls.Add(lblEndDate);
            pnlReportFilters.Controls.Add(dtpEndDate);
            pnlReportFilters.Controls.Add(btnGenerateReport);
            pnlReportFilters.Location = new Point(20, 65);
            pnlReportFilters.Name = "pnlReportFilters";
            pnlReportFilters.Size = new Size(1276, 145);
            pnlReportFilters.TabIndex = 1;
            // 
            // cmbCustomer
            // 
            cmbCustomer.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCustomer.FormattingEnabled = true;
            cmbCustomer.Location = new Point(20, 100);
            cmbCustomer.Name = "cmbCustomer";
            cmbCustomer.Size = new Size(220, 33);
            cmbCustomer.TabIndex = 8;
            cmbCustomer.Visible = false;
            // 
            // lblCustomer
            // 
            lblCustomer.AutoSize = true;
            lblCustomer.Location = new Point(20, 75);
            lblCustomer.Name = "lblCustomer";
            lblCustomer.Size = new Size(89, 25);
            lblCustomer.TabIndex = 7;
            lblCustomer.Text = "Customer";
            lblCustomer.Visible = false;
            // 
            // lblReportType
            // 
            lblReportType.AutoSize = true;
            lblReportType.Location = new Point(20, 10);
            lblReportType.Name = "lblReportType";
            lblReportType.Size = new Size(107, 25);
            lblReportType.TabIndex = 0;
            lblReportType.Text = "Report Type";
            // 
            // cmbReportType
            // 
            cmbReportType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbReportType.FormattingEnabled = true;
            cmbReportType.Location = new Point(20, 35);
            cmbReportType.Name = "cmbReportType";
            cmbReportType.Size = new Size(220, 33);
            cmbReportType.TabIndex = 1;
            // 
            // lblStartDate
            // 
            lblStartDate.AutoSize = true;
            lblStartDate.Location = new Point(277, 10);
            lblStartDate.Name = "lblStartDate";
            lblStartDate.Size = new Size(90, 25);
            lblStartDate.TabIndex = 2;
            lblStartDate.Text = "Start Date";
            // 
            // dtpStartDate
            // 
            dtpStartDate.Format = DateTimePickerFormat.Short;
            dtpStartDate.Location = new Point(277, 35);
            dtpStartDate.Name = "dtpStartDate";
            dtpStartDate.Size = new Size(130, 31);
            dtpStartDate.TabIndex = 3;
            // 
            // lblEndDate
            // 
            lblEndDate.AutoSize = true;
            lblEndDate.Location = new Point(437, 10);
            lblEndDate.Name = "lblEndDate";
            lblEndDate.Size = new Size(84, 25);
            lblEndDate.TabIndex = 5;
            lblEndDate.Text = "End Date";
            // 
            // dtpEndDate
            // 
            dtpEndDate.Format = DateTimePickerFormat.Short;
            dtpEndDate.Location = new Point(437, 35);
            dtpEndDate.Name = "dtpEndDate";
            dtpEndDate.Size = new Size(130, 31);
            dtpEndDate.TabIndex = 4;
            // 
            // btnGenerateReport
            // 
            btnGenerateReport.BackColor = Color.FromArgb(37, 99, 235);
            btnGenerateReport.FlatStyle = FlatStyle.Flat;
            btnGenerateReport.ForeColor = Color.White;
            btnGenerateReport.Location = new Point(597, 33);
            btnGenerateReport.Name = "btnGenerateReport";
            btnGenerateReport.Size = new Size(140, 35);
            btnGenerateReport.TabIndex = 6;
            btnGenerateReport.Text = "Generate Report";
            btnGenerateReport.UseVisualStyleBackColor = false;
            // 
            // lblReportsPlaceholder
            // 
            lblReportsPlaceholder.AutoSize = true;
            lblReportsPlaceholder.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblReportsPlaceholder.ForeColor = Color.FromArgb(17, 24, 39);
            lblReportsPlaceholder.Location = new Point(20, 20);
            lblReportsPlaceholder.Name = "lblReportsPlaceholder";
            lblReportsPlaceholder.Size = new Size(545, 48);
            lblReportsPlaceholder.TabIndex = 0;
            lblReportsPlaceholder.Text = "Business Reports && Statements";
            // 
            // dgvReportResults
            // 
            dgvReportResults.AllowUserToAddRows = false;
            dgvReportResults.AllowUserToDeleteRows = false;
            dgvReportResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvReportResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReportResults.BackgroundColor = Color.White;
            dgvReportResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvReportResults.Location = new Point(20, 224);
            dgvReportResults.Name = "dgvReportResults";
            dgvReportResults.ReadOnly = true;
            dgvReportResults.RowHeadersVisible = false;
            dgvReportResults.RowHeadersWidth = 62;
            dgvReportResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReportResults.Size = new Size(1276, 269);
            dgvReportResults.TabIndex = 2;
            // 
            // lblReportSummary
            // 
            lblReportSummary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblReportSummary.AutoSize = true;
            lblReportSummary.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblReportSummary.Location = new Point(20, 497);
            lblReportSummary.Name = "lblReportSummary";
            lblReportSummary.Size = new Size(235, 30);
            lblReportSummary.TabIndex = 3;
            lblReportSummary.Text = "No report generated.";
            // 
            // btnPrintReport
            // 
            btnPrintReport.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnPrintReport.Enabled = false;
            btnPrintReport.Location = new Point(1158, 547);
            btnPrintReport.Name = "btnPrintReport";
            btnPrintReport.Size = new Size(130, 38);
            btnPrintReport.TabIndex = 4;
            btnPrintReport.Text = "Print Report";
            btnPrintReport.UseVisualStyleBackColor = true;
            // 
            // btnExportPdf
            // 
            btnExportPdf.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnExportPdf.Location = new Point(1022, 547);
            btnExportPdf.Name = "btnExportPdf";
            btnExportPdf.Size = new Size(130, 38);
            btnExportPdf.TabIndex = 5;
            btnExportPdf.Text = "Export PDF";
            btnExportPdf.UseVisualStyleBackColor = true;
            // 
            // btnExportExcel
            // 
            btnExportExcel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnExportExcel.Location = new Point(886, 547);
            btnExportExcel.Name = "btnExportExcel";
            btnExportExcel.Size = new Size(130, 38);
            btnExportExcel.TabIndex = 6;
            btnExportExcel.Text = "Export Excel";
            btnExportExcel.UseVisualStyleBackColor = true;
            // 
            // btnExportWord
            // 
            btnExportWord.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnExportWord.Location = new Point(750, 547);
            btnExportWord.Name = "btnExportWord";
            btnExportWord.Size = new Size(130, 38);
            btnExportWord.TabIndex = 7;
            btnExportWord.Text = "Export Word";
            btnExportWord.UseVisualStyleBackColor = true;
            // 
            // pnlUserManagement
            // 
            pnlUserManagement.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlUserManagement.BackColor = Color.White;
            pnlUserManagement.BorderStyle = BorderStyle.FixedSingle;
            pnlUserManagement.Controls.Add(lblUserManagementPlaceholder);
            pnlUserManagement.Location = new Point(20, 110);
            pnlUserManagement.Name = "pnlUserManagement";
            pnlUserManagement.Size = new Size(1315, 597);
            pnlUserManagement.TabIndex = 12;
            pnlUserManagement.Visible = false;
            // 
            // lblUserManagementPlaceholder
            // 
            lblUserManagementPlaceholder.AutoSize = true;
            lblUserManagementPlaceholder.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUserManagementPlaceholder.Location = new Point(25, 25);
            lblUserManagementPlaceholder.Name = "lblUserManagementPlaceholder";
            lblUserManagementPlaceholder.Size = new Size(358, 38);
            lblUserManagementPlaceholder.TabIndex = 0;
            lblUserManagementPlaceholder.Text = "User management section";
            // 
            // pnlDashboard
            // 
            pnlDashboard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlDashboard.BackColor = Color.White;
            pnlDashboard.BorderStyle = BorderStyle.FixedSingle;
            pnlDashboard.Controls.Add(tlpDashboardCards);
            pnlDashboard.Location = new Point(20, 110);
            pnlDashboard.Name = "pnlDashboard";
            pnlDashboard.Size = new Size(1315, 597);
            pnlDashboard.TabIndex = 10;
            // 
            // tlpDashboardCards
            // 
            tlpDashboardCards.ColumnCount = 2;
            tlpDashboardCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpDashboardCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpDashboardCards.Controls.Add(pnlTotalProducts, 0, 0);
            tlpDashboardCards.Controls.Add(pnlActiveCustomers, 1, 0);
            tlpDashboardCards.Controls.Add(pnlSalesToday, 0, 1);
            tlpDashboardCards.Controls.Add(pnlPendingConsignments, 1, 1);
            tlpDashboardCards.Dock = DockStyle.Fill;
            tlpDashboardCards.Location = new Point(0, 0);
            tlpDashboardCards.Margin = new Padding(0);
            tlpDashboardCards.Name = "tlpDashboardCards";
            tlpDashboardCards.Padding = new Padding(15);
            tlpDashboardCards.RowCount = 2;
            tlpDashboardCards.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpDashboardCards.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpDashboardCards.Size = new Size(1313, 595);
            tlpDashboardCards.TabIndex = 0;
            // 
            // pnlTotalProducts
            // 
            pnlTotalProducts.BackColor = Color.FromArgb(249, 250, 251);
            pnlTotalProducts.BorderStyle = BorderStyle.FixedSingle;
            pnlTotalProducts.Controls.Add(lblTotalProductsValue);
            pnlTotalProducts.Controls.Add(lblTotalProductsTitle);
            pnlTotalProducts.Dock = DockStyle.Fill;
            pnlTotalProducts.Location = new Point(25, 25);
            pnlTotalProducts.Margin = new Padding(10);
            pnlTotalProducts.Name = "pnlTotalProducts";
            pnlTotalProducts.Size = new Size(621, 262);
            pnlTotalProducts.TabIndex = 0;
            // 
            // lblTotalProductsValue
            // 
            lblTotalProductsValue.AutoSize = true;
            lblTotalProductsValue.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            lblTotalProductsValue.ForeColor = Color.FromArgb(17, 24, 39);
            lblTotalProductsValue.Location = new Point(20, 55);
            lblTotalProductsValue.Name = "lblTotalProductsValue";
            lblTotalProductsValue.Size = new Size(47, 65);
            lblTotalProductsValue.TabIndex = 1;
            lblTotalProductsValue.Text = "-";
            // 
            // lblTotalProductsTitle
            // 
            lblTotalProductsTitle.AutoSize = true;
            lblTotalProductsTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblTotalProductsTitle.ForeColor = Color.FromArgb(107, 114, 128);
            lblTotalProductsTitle.Location = new Point(20, 20);
            lblTotalProductsTitle.Name = "lblTotalProductsTitle";
            lblTotalProductsTitle.Size = new Size(148, 28);
            lblTotalProductsTitle.TabIndex = 0;
            lblTotalProductsTitle.Text = "Total Products";
            // 
            // pnlActiveCustomers
            // 
            pnlActiveCustomers.BackColor = Color.FromArgb(249, 250, 251);
            pnlActiveCustomers.BorderStyle = BorderStyle.FixedSingle;
            pnlActiveCustomers.Controls.Add(lblActiveCustomersValue);
            pnlActiveCustomers.Controls.Add(lblActiveCustomersTitle);
            pnlActiveCustomers.Dock = DockStyle.Fill;
            pnlActiveCustomers.Location = new Point(666, 25);
            pnlActiveCustomers.Margin = new Padding(10);
            pnlActiveCustomers.Name = "pnlActiveCustomers";
            pnlActiveCustomers.Size = new Size(622, 262);
            pnlActiveCustomers.TabIndex = 1;
            // 
            // lblActiveCustomersValue
            // 
            lblActiveCustomersValue.AutoSize = true;
            lblActiveCustomersValue.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            lblActiveCustomersValue.ForeColor = Color.FromArgb(17, 24, 39);
            lblActiveCustomersValue.Location = new Point(20, 55);
            lblActiveCustomersValue.Name = "lblActiveCustomersValue";
            lblActiveCustomersValue.Size = new Size(47, 65);
            lblActiveCustomersValue.TabIndex = 1;
            lblActiveCustomersValue.Text = "-";
            // 
            // lblActiveCustomersTitle
            // 
            lblActiveCustomersTitle.AutoSize = true;
            lblActiveCustomersTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblActiveCustomersTitle.ForeColor = Color.FromArgb(107, 114, 128);
            lblActiveCustomersTitle.Location = new Point(20, 20);
            lblActiveCustomersTitle.Name = "lblActiveCustomersTitle";
            lblActiveCustomersTitle.Size = new Size(177, 28);
            lblActiveCustomersTitle.TabIndex = 0;
            lblActiveCustomersTitle.Text = "Active Customers";
            // 
            // pnlSalesToday
            // 
            pnlSalesToday.BackColor = Color.FromArgb(249, 250, 251);
            pnlSalesToday.BorderStyle = BorderStyle.FixedSingle;
            pnlSalesToday.Controls.Add(lblSalesTodayValue);
            pnlSalesToday.Controls.Add(lblSalesTodayTitle);
            pnlSalesToday.Dock = DockStyle.Fill;
            pnlSalesToday.Location = new Point(25, 307);
            pnlSalesToday.Margin = new Padding(10);
            pnlSalesToday.Name = "pnlSalesToday";
            pnlSalesToday.Size = new Size(621, 263);
            pnlSalesToday.TabIndex = 2;
            // 
            // lblSalesTodayValue
            // 
            lblSalesTodayValue.AutoSize = true;
            lblSalesTodayValue.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            lblSalesTodayValue.ForeColor = Color.FromArgb(17, 24, 39);
            lblSalesTodayValue.Location = new Point(20, 55);
            lblSalesTodayValue.Name = "lblSalesTodayValue";
            lblSalesTodayValue.Size = new Size(47, 65);
            lblSalesTodayValue.TabIndex = 1;
            lblSalesTodayValue.Text = "-";
            // 
            // lblSalesTodayTitle
            // 
            lblSalesTodayTitle.AutoSize = true;
            lblSalesTodayTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSalesTodayTitle.ForeColor = Color.FromArgb(107, 114, 128);
            lblSalesTodayTitle.Location = new Point(20, 20);
            lblSalesTodayTitle.Name = "lblSalesTodayTitle";
            lblSalesTodayTitle.Size = new Size(122, 28);
            lblSalesTodayTitle.TabIndex = 0;
            lblSalesTodayTitle.Text = "Sales Today";
            // 
            // pnlPendingConsignments
            // 
            pnlPendingConsignments.BackColor = Color.FromArgb(249, 250, 251);
            pnlPendingConsignments.BorderStyle = BorderStyle.FixedSingle;
            pnlPendingConsignments.Controls.Add(lblPendingConsignmentsValue);
            pnlPendingConsignments.Controls.Add(lblPendingConsignmentsTitle);
            pnlPendingConsignments.Dock = DockStyle.Fill;
            pnlPendingConsignments.Location = new Point(666, 307);
            pnlPendingConsignments.Margin = new Padding(10);
            pnlPendingConsignments.Name = "pnlPendingConsignments";
            pnlPendingConsignments.Size = new Size(622, 263);
            pnlPendingConsignments.TabIndex = 3;
            // 
            // lblPendingConsignmentsValue
            // 
            lblPendingConsignmentsValue.AutoSize = true;
            lblPendingConsignmentsValue.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            lblPendingConsignmentsValue.ForeColor = Color.FromArgb(17, 24, 39);
            lblPendingConsignmentsValue.Location = new Point(20, 55);
            lblPendingConsignmentsValue.Name = "lblPendingConsignmentsValue";
            lblPendingConsignmentsValue.Size = new Size(47, 65);
            lblPendingConsignmentsValue.TabIndex = 1;
            lblPendingConsignmentsValue.Text = "-";
            // 
            // lblPendingConsignmentsTitle
            // 
            lblPendingConsignmentsTitle.AutoSize = true;
            lblPendingConsignmentsTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblPendingConsignmentsTitle.ForeColor = Color.FromArgb(107, 114, 128);
            lblPendingConsignmentsTitle.Location = new Point(20, 20);
            lblPendingConsignmentsTitle.Name = "lblPendingConsignmentsTitle";
            lblPendingConsignmentsTitle.Size = new Size(227, 28);
            lblPendingConsignmentsTitle.TabIndex = 0;
            lblPendingConsignmentsTitle.Text = "Pending Consignments";
            // 
            // pnlSettings
            // 
            pnlSettings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlSettings.BackColor = Color.White;
            pnlSettings.BorderStyle = BorderStyle.FixedSingle;
            pnlSettings.Controls.Add(lblSettingsPlaceholder);
            pnlSettings.Location = new Point(20, 110);
            pnlSettings.Name = "pnlSettings";
            pnlSettings.Size = new Size(1315, 597);
            pnlSettings.TabIndex = 13;
            pnlSettings.Visible = false;
            // 
            // lblSettingsPlaceholder
            // 
            lblSettingsPlaceholder.AutoSize = true;
            lblSettingsPlaceholder.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSettingsPlaceholder.Location = new Point(25, 25);
            lblSettingsPlaceholder.Name = "lblSettingsPlaceholder";
            lblSettingsPlaceholder.Size = new Size(225, 38);
            lblSettingsPlaceholder.TabIndex = 0;
            lblSettingsPlaceholder.Text = "Settings section";
            // 
            // statusStripMain
            // 
            statusStripMain.BackColor = Color.White;
            statusStripMain.ImageScalingSize = new Size(0, 0);
            statusStripMain.Items.AddRange(new ToolStripItem[] { tsslSessionStatus, tsslDateTime });
            statusStripMain.Location = new Point(220, 812);
            statusStripMain.Name = "statusStripMain";
            statusStripMain.Size = new Size(1358, 32);
            statusStripMain.TabIndex = 3;
            // 
            // tsslSessionStatus
            // 
            tsslSessionStatus.AutoSize = false;
            tsslSessionStatus.Name = "tsslSessionStatus";
            tsslSessionStatus.Size = new Size(626, 25);
            tsslSessionStatus.Text = "Session ative";
            tsslSessionStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tsslDateTime
            // 
            tsslDateTime.Name = "tsslDateTime";
            tsslDateTime.Size = new Size(171, 25);
            tsslDateTime.Text = "K&&K Wholesale POS";
            tsslDateTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1578, 844);
            Controls.Add(statusStripMain);
            Controls.Add(pnlContent);
            Controls.Add(pnlSidebar);
            Controls.Add(pnlHeader);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "K&K Wholesale POS";
            WindowState = FormWindowState.Maximized;
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlSidebar.ResumeLayout(false);
            flpNavigation.ResumeLayout(false);
            pnlContent.ResumeLayout(false);
            pnlContent.PerformLayout();
            pnlReports.ResumeLayout(false);
            pnlReports.PerformLayout();
            pnlReportFilters.ResumeLayout(false);
            pnlReportFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReportResults).EndInit();
            pnlUserManagement.ResumeLayout(false);
            pnlUserManagement.PerformLayout();
            pnlDashboard.ResumeLayout(false);
            tlpDashboardCards.ResumeLayout(false);
            pnlTotalProducts.ResumeLayout(false);
            pnlTotalProducts.PerformLayout();
            pnlActiveCustomers.ResumeLayout(false);
            pnlActiveCustomers.PerformLayout();
            pnlSalesToday.ResumeLayout(false);
            pnlSalesToday.PerformLayout();
            pnlPendingConsignments.ResumeLayout(false);
            pnlPendingConsignments.PerformLayout();
            pnlSettings.ResumeLayout(false);
            pnlSettings.PerformLayout();
            statusStripMain.ResumeLayout(false);
            statusStripMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlHeader;
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Label lblCurrentUser;
        private Label lblAppName;
        private FlowLayoutPanel flpNavigation;
        private Label lblNavigationTitle;
        private Button btnDashboard;
        private Button btnProducts;
        private Button btnCustomers;
        private Button btnSales;
        private Button btnReports;
        private Button btnUserManagement;
        private Button btnSettings;
        private Button btnConsignments;
        private Label lblWelcome;
        private Label lblPageTitle;
        private Panel pnlDashboard;
        private TableLayoutPanel tlpDashboardCards;
        private Panel pnlTotalProducts;
        private Panel pnlActiveCustomers;
        private Panel pnlSalesToday;
        private Panel pnlPendingConsignments;
        private Label lblTotalProductsValue;
        private Label lblTotalProductsTitle;
        private Label lblActiveCustomersValue;
        private Label lblActiveCustomersTitle;
        private Label lblSalesTodayValue;
        private Label lblSalesTodayTitle;
        private Label lblPendingConsignmentsValue;
        private Label lblPendingConsignmentsTitle;
        private Button btnLogout;
        private StatusStrip statusStripMain;
        private ToolStripStatusLabel tsslSessionStatus;
        private ToolStripStatusLabel tsslDateTime;
        private Panel pnlSettings;
        private Panel pnlUserManagement;
        private Panel pnlReports;
        private Label lblUserManagementPlaceholder;
        private Label lblSettingsPlaceholder;
        private Label lblReportsPlaceholder;
        private Panel pnlReportFilters;
        private Button btnGenerateReport;
        private Label lblEndDate;
        private DateTimePicker dtpEndDate;
        private DateTimePicker dtpStartDate;
        private Label lblStartDate;
        private ComboBox cmbReportType;
        private Label lblReportType;
        private DataGridView dgvReportResults;
        private Button btnPrintReport;
        private Label lblReportSummary;
        private ComboBox cmbCustomer;
        private Label lblCustomer;
        private Button btnExportWord;
        private Button btnExportExcel;
        private Button btnExportPdf;
    }
}