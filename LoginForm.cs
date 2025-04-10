using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ParentalControlApp
{
    public class LoginForm : Form
    {
        private TextBox txtPassword;
        private Button btnLogin;
        private readonly string configPath = "config.txt";
        private readonly string recoveryPath = "recovery.txt";
        public bool IsAuthenticated { get; private set; } = false;

        public LoginForm()
        {
            Text = "Enter Admin Password";
            Size = new System.Drawing.Size(300, 150);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lbl = new Label { Text = "Password:", Top = 20, Left = 10, Width = 80 };
            txtPassword = new TextBox { Top = 20, Left = 100, Width = 160, UseSystemPasswordChar = true };
            btnLogin = new Button { Text = "Login", Top = 60, Left = 100, Width = 80 };
            Button btnForgot = new Button { Text = "Forgot Password", Top = 60, Left = 190, Width = 80 };

            btnLogin.Click += (s, e) => CheckPassword();
            btnForgot.Click += (s, e) => HandlePasswordReset();
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) CheckPassword(); };

            Controls.AddRange(new Control[] { lbl, txtPassword, btnLogin, btnForgot });
        }

        private void CheckPassword()
        {
            string inputHash = HashPassword(txtPassword.Text);

            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, inputHash);
                string recoveryCode = Guid.NewGuid().ToString().Substring(0, 8);
                File.WriteAllText(recoveryPath, recoveryCode);
                MessageBox.Show($"Password set. Your recovery code is: {recoveryCode}\nStore it somewhere safe!", "Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                IsAuthenticated = true;
                Close();
                return;
            }

            string storedHash = File.ReadAllText(configPath);
            if (inputHash == storedHash)
            {
                IsAuthenticated = true;
                Close();
            }
            else
            {
                MessageBox.Show("Incorrect password.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Clear();
            }
        }

        private void HandlePasswordReset()
        {
            if (!File.Exists(recoveryPath))
            {
                MessageBox.Show("No recovery code was set. You can't reset the password this way.", "Error");
                return;
            }

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter your recovery code to reset the password:",
                "Reset Password"
            );

            string stored = File.ReadAllText(recoveryPath).Trim();

            if (input == stored)
            {
                string newPassword = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter your new password:",
                    "Set New Password"
                );

                string newHash = HashPassword(newPassword);
                File.WriteAllText(configPath, newHash);
                MessageBox.Show("Password has been reset successfully.", "Done");

                IsAuthenticated = true;
                Close();
            }
            else
            {
                MessageBox.Show("Incorrect recovery code.", "Access Denied");
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}