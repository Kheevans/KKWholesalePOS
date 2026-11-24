using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.UI;

public class ConsignmentReturnsControl : UserControl
{
    private readonly ConsignmentReturnService _service;

    private readonly TextBox txtReference = new() { Width = 220 };
    private readonly Button btnFind = new() { Text = "Find", Width = 90, Height = 36 };
    private readonly DataGridView dgvLines = new();
    private readonly Label lblSummary = new();
    private readonly Button btnProcessReturn = new() { Text = "Process Return", Width = 150, Height = 36 };
    private readonly NumericUpDown nudPayment = new()
    {
        DecimalPlaces = 2,
        Maximum = 100000000,
        Width = 130
    };
    private readonly ComboBox cmbMethod = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 130
    };
    private readonly Button btnRecordPayment = new() { Text = "Record Payment", Width = 300, Height = 36 };

    private string? _currentReference;

    public ConsignmentReturnsControl(ConsignmentReturnService service)
    {
        _service = service;

        BackColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        // Grid (added first so it docks last and fills the space left over)
        dgvLines.Dock = DockStyle.Fill;
        dgvLines.AllowUserToAddRows = false;
        dgvLines.AllowUserToDeleteRows = false;
        dgvLines.RowHeadersVisible = false;
        dgvLines.BackgroundColor = Color.White;
        dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvLines.Columns.Add("LineId", "ID");
        dgvLines.Columns.Add("Product", "Product");
        dgvLines.Columns.Add("Issued", "Issued");
        dgvLines.Columns.Add("Returned", "Accepted Returns So Far");
        dgvLines.Columns.Add("Outstanding", "Outstanding");
        dgvLines.Columns.Add("Price", "Issue Price (J$)");
        dgvLines.Columns.Add("ReturnQty", "Return Qty");
        dgvLines.Columns.Add("AcceptedQty", "Accepted Qty");
        dgvLines.Columns["LineId"]!.Visible = false;
        for (int i = 0; i < 6; i++)
            dgvLines.Columns[i].ReadOnly = true;
        dgvLines.CellValueChanged += DgvLines_CellValueChanged;

        // Bottom: summary and actions
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 280, Padding = new Padding(15) };

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 90, Padding = new Padding(0, 10, 0, 0) };
        cmbMethod.Items.AddRange(new object[] { "Cash", "Card", "Bank Transfer" });
        cmbMethod.SelectedIndex = 0;
        actions.Controls.Add(btnProcessReturn);
        actions.Controls.Add(new Label { Text = "   Payment (J$):", AutoSize = true, Margin = new Padding(20, 9, 0, 0) });
        actions.Controls.Add(nudPayment);
        actions.Controls.Add(cmbMethod);
        actions.Controls.Add(btnRecordPayment);

        lblSummary.Dock = DockStyle.Fill;
        lblSummary.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblSummary.Text = "Enter a consignment reference and click Find.";

        bottom.Controls.Add(lblSummary);
        bottom.Controls.Add(actions);

        // Top: search
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(15, 15, 15, 0) };
        top.Controls.Add(new Label { Text = "Consignment Reference:", AutoSize = true, Margin = new Padding(0, 6, 5, 0) });
        top.Controls.Add(txtReference);
        top.Controls.Add(btnFind);

        Controls.Add(dgvLines);
        Controls.Add(bottom);
        Controls.Add(top);

        btnFind.Click += (s, e) => LoadConsignment(txtReference.Text);
        btnProcessReturn.Click += (s, e) => ProcessReturn();
        btnRecordPayment.Click += (s, e) => RecordPayment();
        txtReference.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadConsignment(txtReference.Text);
            }
        };
    }

    // When a return qty is typed, default accepted qty to the same number
    private void DgvLines_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (e.ColumnIndex != dgvLines.Columns["ReturnQty"]!.Index) return;

        var row = dgvLines.Rows[e.RowIndex];
        var accepted = row.Cells["AcceptedQty"].Value?.ToString();
        if (accepted == "0" || string.IsNullOrWhiteSpace(accepted))
            row.Cells["AcceptedQty"].Value = row.Cells["ReturnQty"].Value;
    }

    private void LoadConsignment(string reference)
    {
        try
        {
            var view = _service.FindConsignment(reference);

            if (view == null)
            {
                dgvLines.Rows.Clear();
                _currentReference = null;
                lblSummary.Text = "No consignment found with that reference.";
                return;
            }

            _currentReference = view.Consignment.ReferenceNumber;
            dgvLines.Rows.Clear();

            foreach (var s in view.Lines)
            {
                dgvLines.Rows.Add(
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
            lblSummary.Text =
                $"Reference: {_currentReference}\r\n" +
                $"Original goods: J${st.OriginalTotal:N2}   |   Fee (5%): J${st.Fee:N2}   |   Accepted returns: J${st.AcceptedReturnsValue:N2}\r\n" +
                $"Final balance: J${st.FinalBalance:N2}   |   Paid: J${st.TotalPaid:N2}   |   Outstanding: J${st.Outstanding:N2}\r\n" +
                (st.IsSettled ? "STATUS: SETTLED" : "STATUS: NOT SETTLED");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void ProcessReturn()
    {
        if (_currentReference == null)
        {
            MessageBox.Show("Find a consignment first.", "Returns",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        dgvLines.EndEdit();

        var requested = new List<ReturnRequestLine>();

        foreach (DataGridViewRow row in dgvLines.Rows)
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
            var saved = _service.ProcessReturn(_currentReference, requested);
            MessageBox.Show($"Return {saved.ReferenceNumber} saved.", "Return Processed",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadConsignment(_currentReference);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RecordPayment()
    {
        if (_currentReference == null)
        {
            MessageBox.Show("Find a consignment first.", "Payment",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            _service.RecordPayment(
                _currentReference,
                nudPayment.Value,
                cmbMethod.SelectedItem?.ToString() ?? "Cash");

            MessageBox.Show("Payment recorded.", "Payment",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            nudPayment.Value = 0;
            LoadConsignment(_currentReference);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private static void ShowError(Exception ex)
    {
        var icon = ex is UnauthorizedAccessException or InvalidOperationException or ArgumentException
            ? MessageBoxIcon.Warning
            : MessageBoxIcon.Error;

        MessageBox.Show(ex.Message, "Consignment Returns", MessageBoxButtons.OK, icon);
    }
}