namespace KKWholesalePOS.UI
{
    partial class LoginForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            lblTitle = new Label();
            lblUsername = new Label();
            lblPassword = new Label();
            lblError = new Label();
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            btnLogin = new Button();
            colorDialog1 = new ColorDialog();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.Location = new Point(50, 35);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(300, 50);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "K && K Wholesale POS";
            // 
            // lblUsername
            // 
            lblUsername.Location = new Point(12, 105);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(174, 20);
            lblUsername.TabIndex = 1;
            lblUsername.Text = "Username";
            // 
            // lblPassword
            // 
            lblPassword.Location = new Point(12, 180);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(174, 20);
            lblPassword.TabIndex = 2;
            lblPassword.Text = "Password";
            // 
            // lblError
            // 
            lblError.ForeColor = Color.Red;
            lblError.Location = new Point(12, 310);
            lblError.Name = "lblError";
            lblError.Size = new Size(374, 25);
            lblError.TabIndex = 3;
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(12, 130);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(374, 31);
            txtUsername.TabIndex = 4;
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(12, 205);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(374, 31);
            txtPassword.TabIndex = 5;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(12, 260);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(374, 40);
            btnLogin.TabIndex = 6;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += btnLogin_Click;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(398, 344);
            Controls.Add(btnLogin);
            Controls.Add(txtPassword);
            Controls.Add(txtUsername);
            Controls.Add(lblError);
            Controls.Add(lblPassword);
            Controls.Add(lblUsername);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "K&K Wholesale POS - Login";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblUsername;
        private Label lblPassword;
        private Label lblError;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private ColorDialog colorDialog1;
    }
}