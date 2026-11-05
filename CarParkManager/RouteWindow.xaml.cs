using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CarParkManager
{
    public partial class RouteWindow : Window
    {
        private readonly Routes form;
        private readonly int? signedInDriverId;
        private readonly bool isAdmin;

        public RouteWindow(int? signedInDriverId, bool isAdmin)
        {
            InitializeComponent();

            this.signedInDriverId = signedInDriverId;
            this.isAdmin = isAdmin;

            if (!isAdmin)
            {
                HeaderText.Text = "Взять транспорт и оформить рейс";
            }

            form = new Routes
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today,
                DriverId = isAdmin ? 0 : signedInDriverId.GetValueOrDefault()
            };

            try
            {
                using (var db = new FleetDBEntities())
                {
                    FleetService.RefreshVehicleStatuses(db);

                    var allVehicles = db.Vehicles
                        .OrderBy(v => v.LicensePlate)
                        .ToList();

                    var selectableVehicles = isAdmin
                        ? allVehicles.Where(v => v.Status != VehicleStatus.Repair)
                        : allVehicles.Where(v => v.Status == VehicleStatus.Available);

                    var vehicleOptions = selectableVehicles
                        .Select(v => new Option
                        {
                            Id = v.Id,
                            Title = v.LicensePlate + " - " + v.Title + " (" + v.Status + ")"
                        })
                        .ToList();

                    VehicleBox.ItemsSource = vehicleOptions;
                    VehicleBox.IsEnabled = vehicleOptions.Count > 0;

                    if (!isAdmin)
                    {
                        VehicleHint.Text = vehicleOptions.Count == 0
                            ? "Сейчас нет свободных машин. Транспорт в ремонте или в рейсе взять нельзя."
                            : "Можно выбрать только свободный транспорт. Машины в ремонте или в рейсе недоступны.";
                    }

                    var drivers = db.Drivers.AsQueryable();
                    if (!isAdmin)
                    {
                        int ownId = signedInDriverId.GetValueOrDefault();
                        drivers = drivers.Where(d => d.Id == ownId);
                    }

                    DriverBox.ItemsSource = drivers
                        .OrderBy(d => d.FullName)
                        .ToList()
                        .Select(d => new Option { Id = d.Id, Title = d.FullName })
                        .ToList();

                    if (!isAdmin)
                    {
                        DriverBox.SelectedValue = signedInDriverId.GetValueOrDefault();
                        DriverBox.IsEnabled = false;
                        DriverBox.ToolTip = "Водитель определяется по текущей учётной записи";
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось загрузить списки из базы данных.");
            }

            DataContext = form;
            if (!isAdmin)
            {
                DriverBox.SelectedValue = signedInDriverId.GetValueOrDefault();
                DriverBox.IsEnabled = false;
            }
        }

        private void ClearErrors()
        {
            VehicleError.Text = "";
            DriverError.Text = "";
            StartError.Text = "";
            EndError.Text = "";
            FromError.Text = "";
            ToError.Text = "";
            DistanceError.Text = "";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors();

            string startLocation = (form.StartLocation ?? "").Trim();
            string endLocation = (form.EndLocation ?? "").Trim();

            string vehicleError = "";
            string driverError = "";
            string startError = "";
            string endError = "";
            string fromError = "";
            string toError = "";
            string distanceError = "";

            if (form.VehicleId == 0)
            {
                vehicleError = "Выберите транспортное средство";
            }

            int selectedDriverId = isAdmin ? form.DriverId : signedInDriverId.GetValueOrDefault();
            if (selectedDriverId == 0)
            {
                driverError = "Не удалось определить водителя";
            }

            if (Validation.GetHasError(StartPicker) || StartPicker.SelectedDate == null)
            {
                startError = "Укажите дату начала";
            }

            if (Validation.GetHasError(EndPicker) || EndPicker.SelectedDate == null)
            {
                endError = "Укажите дату окончания";
            }
            else if (startError == "" && form.EndDate.Date < form.StartDate.Date)
            {
                endError = "Окончание раньше начала";
            }

            if (startLocation == "" || startLocation.Length > 100)
            {
                fromError = "Обязательное поле, до 100 символов";
            }

            if (endLocation == "" || endLocation.Length > 100)
            {
                toError = "Обязательное поле, до 100 символов";
            }

            if (Validation.GetHasError(DistanceBox))
            {
                distanceError = "Введите целое число";
            }
            else if (form.Distance < 1 || form.Distance > 20000)
            {
                distanceError = "От 1 до 20 000 км";
            }

            try
            {
                using (var db = new FleetDBEntities())
                {
                    int vehicleId = form.VehicleId;
                    FleetService.RefreshVehicleStatuses(db);

                    int driverId = isAdmin ? form.DriverId : signedInDriverId.GetValueOrDefault();

                    if (driverError == "" && !db.Drivers.Any(d => d.Id == driverId))
                    {
                        driverError = "Учётная запись водителя не найдена";
                    }

                    Vehicles vehicle = null;

                    if (vehicleError == "")
                    {
                        vehicle = db.Vehicles.Find(vehicleId);

                        if (vehicle == null)
                        {
                            vehicleError = "Транспортное средство не найдено";
                        }
                        else if (vehicle.Status == VehicleStatus.Repair)
                        {
                            vehicleError = "Машина в ремонте, взять её нельзя";
                        }
                        else if (!isAdmin && vehicle.Status != VehicleStatus.Available)
                        {
                            vehicleError = "Машина уже занята или находится в рейсе. Выберите свободный транспорт.";
                        }
                    }

                    if (startError == "" && endError == "")
                    {
                        DateTime start = form.StartDate.Date;
                        DateTime end = form.EndDate.Date;

                        if (vehicleError == "" && db.Routes.Any(r => r.VehicleId == vehicleId && r.StartDate <= end && r.EndDate >= start))
                        {
                            vehicleError = "На эти даты у машины уже есть рейс";
                        }

                        if (driverError == "" && db.Routes.Any(r => r.DriverId == driverId && r.StartDate <= end && r.EndDate >= start))
                        {
                            driverError = "На эти даты у водителя уже есть рейс";
                        }
                    }

                    VehicleError.Text = vehicleError;
                    DriverError.Text = driverError;
                    StartError.Text = startError;
                    EndError.Text = endError;
                    FromError.Text = fromError;
                    ToError.Text = toError;
                    DistanceError.Text = distanceError;

                    if (vehicleError != "" || driverError != "" || startError != "" || endError != ""
                        || fromError != "" || toError != "" || distanceError != "")
                    {
                        return;
                    }

                    var route = new Routes
                    {
                        VehicleId = vehicleId,
                        DriverId = driverId,
                        StartDate = form.StartDate.Date,
                        EndDate = form.EndDate.Date,
                        StartLocation = startLocation,
                        EndLocation = endLocation,
                        Distance = form.Distance
                    };

                    db.Routes.Add(route);

                    vehicle.Mileage += form.Distance;

                    db.SaveChanges();

                    FleetService.RefreshVehicleStatuses(db);
                }
            }
            catch (DbUpdateException)
            {
                MessageBox.Show("Не удалось сохранить рейс из-за ошибки базы данных. Попробуйте ещё раз.");
                return;
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка работы с базой данных. Проверьте, что SQL Server запущен.");
                return;
            }

            MessageBox.Show("Рейс зарегистрирован.");

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
