using System;
using System.Windows.Forms;

namespace ParentalControlApp
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var login = new LoginForm();
            Application.Run(login);

            if (login.IsAuthenticated)
            {
                Application.Run(new MainForm());
            }
        }
    }
}
