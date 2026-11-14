namespace KKWholesalePOS.UI
{
    partial class SetupForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupForm));
            lblTitle = new Label();
            lblUsername = new Label();
            lblPassword = new Label();
            lblConfirmPassword = new Label();
            lblError = new Label();
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            txtConfirmPassword = new TextBox();
            btnCreateAccount = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.Location = new Point(12, 34);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(369, 38);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Initial Administrator Setup";
            // 
            // lblUsername
            // 
            lblUsername.Location = new Point(12, 105);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(174, 20);
            lblUsername.TabIndex = 1;
            lblUsername.Text = "Administrator Username";
            // 
            // lblPassword
            // 
            lblPassword.Location = new Point(12, 180);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(174, 20);
            lblPassword.TabIndex = 2;
            lblPassword.Text = "Password";
            // 
            // lblConfirmPassword
            // 
            lblConfirmPassword.Location = new Point(12, 255);
            lblConfirmPassword.Name = "lblConfirmPassword";
            lblConfirmPassword.Size = new Size(174, 20);
            lblConfirmPassword.TabIndex = 3;
            lblConfirmPassword.Text = "Confirm Password";
            // 
            // lblError
            // 
            lblError.ForeColor = Color.Red;
            lblError.Location = new Point(12, 385);
            lblError.Name = "lblError";
            lblError.Size = new Size(374, 25);
            lblError.TabIndex = 4;
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(12, 130);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(374, 31);
            txtUsername.TabIndex = 5;
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(12, 280);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(374, 31);
            txtPassword.TabIndex = 6;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // txtConfirmPassword
            // 
            txtConfirmPassword.Location = new Point(12, 205);
            txtConfirmPassword.Name = "txtConfirmPassword";
            txtConfirmPassword.Size = new Size(374, 31);
            txtConfirmPassword.TabIndex = 7;
            txtConfirmPassword.UseSystemPasswordChar = true;
            // 
            // btnCreateAccount
            // 
            btnCreateAccount.Location = new Point(12, 335);
            btnCreateAccount.Name = "btnCreateAccount";
            btnCreateAccount.Size = new Size(374, 31);
            btnCreateAccount.TabIndex = 8;
            btnCreateAccount.Text = "Create Administrator";
            btnCreateAccount.UseVisualStyleBackColor = true;
            btnCreateAccount.Click += btnCreateAccount_Click;
            // 
            // SetupForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(398, 424);
            Controls.Add(btnCreateAccount);
            Controls.Add(txtConfirmPassword);
            Controls.Add(txtPassword);
            Controls.Add(txtUsername);
            Controls.Add(lblError);
            Controls.Add(lblConfirmPassword);
            Controls.Add(lblPassword);
            Controls.Add(lblUsername);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "SetupForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "K&K Wholesale POS - Initial Setup";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblUsername;
        private Label lblPassword;
        private Label lblConfirmPassword;
        private Label lblError;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtConfirmPassword;
        private Button btnCreateAccount;
    }
}