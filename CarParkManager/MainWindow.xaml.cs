using System;
using System.Linq;
using System.Windows;

namespace CarParkManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginClick(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            string email = EmailBox.Text.Trim();
            string password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Введите почту и пароль";
                return;
            }

            try
            {
                var admin = AdminAccountService.FindByEmail(email);
                if (admin != null)
                {
                    if (!PasswordHelper.Verify(password, admin.PasswordHash))
                    {
                        ErrorText.Text = "Неверная почта или пароль";
                        return;
                    }

                    OpenHomeWindow(admin.Id, admin.FullName, true);
                    return;
                }

                Drivers driver;
                using (var db = new FleetDBEntities())
                {
                    driver = db.Drivers.FirstOrDefault(d => d.Email == email);
                }

                if (driver == null || !VerifyDriverPasswordAndUpgradeLegacy(driver, password))
                {
                    ErrorText.Text = "Неверная почта или пароль";
                    return;
                }

                OpenHomeWindow(driver.Id, driver.FullName, false);
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось подключиться к базе данных. Проверьте SQL Server и строку подключения.");
            }
        }

        private bool VerifyDriverPasswordAndUpgradeLegacy(Drivers driver, string password)
        {
            if (PasswordHelper.Verify(password, driver.Password))
            {
                return true;
            }

            if (!string.Equals(password, driver.Password, StringComparison.Ordinal))
            {
                return false;
            }

            using (var db = new FleetDBEntities())
            {
                var storedDriver = db.Drivers.Find(driver.Id);
                if (storedDriver == null || !string.Equals(storedDriver.Password, password, StringComparison.Ordinal))
                {
                    return false;
                }

                storedDriver.Password = PasswordHelper.getHashPassword(password);
                db.SaveChanges();
                return true;
            }
        }

        private void OpenHomeWindow(int userId, string fullName, bool isAdmin)
        {
            var homeWindow = new HomeWindow(userId, fullName, isAdmin);
            Application.Current.MainWindow = homeWindow;
            homeWindow.Show();
            Close();
        }

        private void OpenRegisterWindow_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new DriverWindow(null, true);
            registerWindow.Owner = this;

            if (registerWindow.ShowDialog() == true)
            {
                EmailBox.Text = registerWindow.SavedEmail;
                PasswordInput.Clear();
                PasswordInput.Focus();
                ErrorText.Text = "";
            }
        }
    }
}
