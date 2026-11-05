using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CarParkManager
{
    public partial class VehicleWindow : Window
    {
        private readonly Vehicles form;
        private readonly bool isEdit;

        public VehicleWindow(Vehicles vehicle)
        {
            InitializeComponent();

            isEdit = vehicle != null;

            form = new Vehicles();

            if (isEdit)
            {
                form.Id = vehicle.Id;
                form.LicensePlate = vehicle.LicensePlate;
                form.Manufacturer = vehicle.Manufacturer;
                form.Model = vehicle.Model;
                form.YearOfManufacture = vehicle.YearOfManufacture;
                form.Mileage = vehicle.Mileage;
                form.LastMaintenanceMileage = vehicle.LastMaintenanceMileage;
                form.LastMaintenanceDate = vehicle.LastMaintenanceDate;

                form.Status = vehicle.Status == VehicleStatus.Repair ? VehicleStatus.Repair : VehicleStatus.Available;

                Title = "Редактирование транспортного средства";
            }
            else
            {
                form.YearOfManufacture = DateTime.Today.Year;
                form.Status = VehicleStatus.Available;

                Title = "Новое транспортное средство";
            }

            HeaderText.Text = Title;
            DataContext = form;
        }

        private void ClearErrors()
        {
            PlateError.Text = "";
            ManufacturerError.Text = "";
            ModelError.Text = "";
            YearError.Text = "";
            MileageError.Text = "";
            StatusError.Text = "";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors();

            string plate = Validator.NormalizePlate(form.LicensePlate);
            string manufacturer = (form.Manufacturer ?? "").Trim();
            string model = (form.Model ?? "").Trim();

            string plateError = "";
            string manufacturerError = "";
            string modelError = "";
            string yearError = "";
            string mileageError = "";

            if (!Validator.IsPlate(plate))
            {
                plateError = "Формат А123ВС77 (буквы А В Е К М Н О Р С Т У Х)";
            }

            if (manufacturer == "" || manufacturer.Length > 50)
            {
                manufacturerError = "Обязательное поле, до 50 символов";
            }

            if (model == "" || model.Length > 50)
            {
                modelError = "Обязательное поле, до 50 символов";
            }

            if (Validation.GetHasError(YearBox))
            {
                yearError = "Введите целое число";
            }
            else if (form.YearOfManufacture < 1980 || form.YearOfManufacture > DateTime.Today.Year + 1)
            {
                yearError = "От 1980 до " + (DateTime.Today.Year + 1);
            }

            if (Validation.GetHasError(MileageBox))
            {
                mileageError = "Введите целое число";
            }
            else if (form.Mileage < 0 || form.Mileage > 5000000)
            {
                mileageError = "От 0 до 5 000 000 км";
            }
            else if (form.Mileage < form.LastMaintenanceMileage)
            {
                mileageError = "Пробег не может быть меньше пробега при последнем ТО ("
                    + form.LastMaintenanceMileage.ToString("N0") + " км)";
            }

            try
            {
                using (var db = new FleetDBEntities())
                {
                    int id = form.Id;

                    if (plateError == "" && db.Vehicles.Any(v => v.LicensePlate == plate && v.Id != id))
                    {
                        plateError = "Такой номер уже есть в базе";
                    }

                    PlateError.Text = plateError;
                    ManufacturerError.Text = manufacturerError;
                    ModelError.Text = modelError;
                    YearError.Text = yearError;
                    MileageError.Text = mileageError;

                    if (plateError != "" || manufacturerError != "" || modelError != ""
                        || yearError != "" || mileageError != "")
                    {
                        return;
                    }

                    Vehicles target;

                    if (isEdit)
                    {
                        target = db.Vehicles.Find(id);

                        if (target == null)
                        {
                            MessageBox.Show("Запись не найдена, возможно, она уже удалена.");
                            DialogResult = false;
                            return;
                        }
                    }
                    else
                    {
                        target = new Vehicles();
                        db.Vehicles.Add(target);
                    }

                    target.LicensePlate = plate;
                    target.Manufacturer = manufacturer;
                    target.Model = model;
                    target.YearOfManufacture = form.YearOfManufacture;
                    target.Mileage = form.Mileage;
                    target.LastMaintenanceMileage = form.LastMaintenanceMileage;
                    target.LastMaintenanceDate = form.LastMaintenanceDate;
                    target.Status = form.Status ?? VehicleStatus.Available;

                    db.SaveChanges();
                }
            }
            catch (DbUpdateException)
            {
                MessageBox.Show("Не удалось сохранить транспортное средство. Возможно, такой номер уже есть.");
                return;
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка работы с базой данных. Проверьте, что SQL Server запущен.");
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
