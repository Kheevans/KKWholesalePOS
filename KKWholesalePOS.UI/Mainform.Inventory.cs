using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.UI;

public partial class MainForm
{
    private InventoryManagementService _inventoryManagementService = null!;

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

    private List<InventoryRow> _inventoryRows = new();
    private bool _inventoryBusy;

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
            _inventoryGrid, "SellingPrice", "Price (J$)", 85, "N2");

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

    // Retrieve the latest inventory records
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

    // Update the inventory overview cards
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

    // Filter inventory records by product name, barcode and stock status
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

    // Highlight products that require attention
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

    // Enable inventory controls according to the user's permissions
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

        _inventoryReceive.Enabled =
            canUseInventory && canReceive && hasSelection;

        _inventoryAdjust.Enabled =
            canUseInventory && canAdjust && hasSelection;

        _inventoryMinimum.Enabled =
            canUseInventory && canAdjust && hasSelection;

        _inventoryHistory.Enabled =
            canUseInventory && hasSelection;
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

    // Clear inventory records and summary values
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

    // Receive stock or adjust the available quantity of a selected product
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

    // Configure the minimum stock level for a selected product
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

    // Display the stock movement history of a selected product
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

    private void btnProducts_Click(object? sender, EventArgs e)
    {
        if (!RequirePermission("Inventory.View"))
            return;

        ShowPage(pnlInventory, "Inventory Management", btnProducts);

        LoadInventory();
    }
}