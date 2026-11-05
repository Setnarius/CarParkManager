using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CarParkManager
{
    public partial class DriverWindow : Window
    {
        private readonly Drivers form;
        private readonly bool isEdit;

        public string SavedEmail { get; private set; }

        public DriverWindow(Drivers driver, bool registration = false)
        {
            InitializeComponent();

            isEdit = driver != null;

            form = new Drivers();

            if (isEdit)
            {
                form.Id = driver.Id;
                form.FullName = driver.FullName;
                form.LicenseNumber = driver.LicenseNumber;
                form.PhoneNumber = driver.PhoneNumber;
                form.Experience = driver.Experience;
                form.Email = driver.Email;

                Title = "Редактирование водителя";
                PasswordLabel.Text = "Новый пароль (необязательно)";
            }
            else
            {
                Title = registration ? "Регистрация" : "Новый водитель";
            }

            HeaderText.Text = Title;
            DataContext = form;
        }

        private void ClearErrors()
        {
            FullNameError.Text = "";
            LicenseError.Text = "";
            PhoneError.Text = "";
            ExperienceError.Text = "";
            EmailError.Text = "";
            PasswordError.Text = "";
            RepeatError.Text = "";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors();

            string fullName = (form.FullName ?? "").Trim();
            string license = (form.LicenseNumber ?? "").Replace(" ", "");
            string phone = Validator.NormalizePhone(form.PhoneNumber);
            string email = (form.Email ?? "").Trim();
            string password = PasswordInput.Password;
            string repeat = RepeatInput.Password;

            string fullNameError = "";
            string licenseError = "";
            string phoneError = "";
            string experienceError = "";
            string emailError = "";
            string passwordError = "";
            string repeatError = "";

            if (fullName.Length < 5 || fullName.Length > 100)
            {
                fullNameError = "От 5 до 100 символов";
            }

            if (!Validator.IsLicenseNumber(license))
            {
                licenseError = "Нужно ровно 10 цифр";
            }

            if (!Validator.IsPhone(phone))
            {
                phoneError = "Формат: +79991234567";
            }

            if (Validation.GetHasError(ExperienceBox))
            {
                experienceError = "Введите целое число";
            }
            else if (form.Experience < 0 || form.Experience > 60)
            {
                experienceError = "От 0 до 60 лет";
            }

            if (email.Any(char.IsWhiteSpace))
            {
                emailError = "Почта не должна содержать пробелов";
            }
            else if (!Validator.IsEmail(email))
            {
                emailError = "Неверный формат почты";
            }

            bool passwordRequired = !isEdit || password != "" || repeat != "";

            if (passwordRequired)
            {
                passwordError = Validator.CheckPassword(password);

                if (password != repeat)
                {
                    repeatError = "Пароли не совпадают";
                }
            }

            try
            {
                if (emailError == "" && AdminAccountService.EmailExists(email))
                {
                    emailError = "Эта почта уже используется учётной записью администратора";
                }

                using (var db = new FleetDBEntities())
                {
                    int id = form.Id;

                    if (licenseError == "" && db.Drivers.Any(d => d.LicenseNumber == license && d.Id != id))
                    {
                        licenseError = "Такое удостоверение уже зарегистрировано";
                    }

                    if (emailError == "" && db.Drivers.Any(d => d.Email == email && d.Id != id))
                    {
                        emailError = "Такая почта уже зарегистрирована";
                    }

                    FullNameError.Text = fullNameError;
                    LicenseError.Text = licenseError;
                    PhoneError.Text = phoneError;
                    ExperienceError.Text = experienceError;
                    EmailError.Text = emailError;
                    PasswordError.Text = passwordError;
                    RepeatError.Text = repeatError;

                    if (fullNameError != "" || licenseError != "" || phoneError != "" || experienceError != ""
                        || emailError != "" || passwordError != "" || repeatError != "")
                    {
                        return;
                    }

                    Drivers target;

                    if (isEdit)
                    {
                        target = db.Drivers.Find(id);

                        if (target == null)
                        {
                            MessageBox.Show("Запись не найдена, возможно, она уже удалена.");
                            DialogResult = false;
                            return;
                        }
                    }
                    else
                    {
                        target = new Drivers();
                        db.Drivers.Add(target);
                    }

                    target.FullName = fullName;
                    target.LicenseNumber = license;
                    target.PhoneNumber = phone;
                    target.Experience = form.Experience;
                    target.Email = email;

                    if (passwordRequired)
                    {
                        target.Password = PasswordHelper.getHashPassword(password);
                    }

                    db.SaveChanges();
                }
            }
            catch (DbUpdateException)
            {
                MessageBox.Show("Не удалось сохранить водителя. Возможно, почта или удостоверение уже заняты.");
                return;
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка работы с базой данных. Проверьте, что SQL Server запущен.");
                return;
            }

            SavedEmail = email;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
