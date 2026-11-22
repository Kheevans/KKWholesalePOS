using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.UI
{
    public partial class LoginForm : Form
    {
        private readonly LoginService _loginService;

        public LoginForm(LoginService loginService)
        {
            InitializeComponent();
            this.AcceptButton = btnLogin;

            _loginService = loginService;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Please enter your username and password.";
                return;
            }

            bool loginSuccessful = _loginService.Login(username, password);

            if (!loginSuccessful)
            {
                lblError.Text = "Invalid username or password.";
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            lblError.Text = string.Empty;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
