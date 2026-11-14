using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.UI
{
    public partial class SetupForm : Form
    {
        private readonly InitialSetupService _initialSetupService;

        public SetupForm(InitialSetupService initialSetupService)
        {
            InitializeComponent();
            _initialSetupService = initialSetupService;
        }

        private void btnCreateAccount_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                lblError.Text = "Please complete all fields.";
                return;
            }

            if (password != confirmPassword)
            {
                lblError.Text = "Passwords do not match.";
                txtConfirmPassword.Clear();
                txtConfirmPassword.Focus();
                return;
            }

            try
            {
                _initialSetupService.CreateInitialAdministrator(
                    username,
                    password);

                MessageBox.Show(
                    "Administrator account created successfully. You can now log in.",
                    "Setup Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }
    }
}