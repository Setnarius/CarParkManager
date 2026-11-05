using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CarParkManager
{
    public partial class HomeWindow : Window
    {
        private readonly int currentDriverId;
        private readonly bool isAdmin;

        public HomeWindow(int userId, string fullName, bool isAdmin)
        {
            InitializeComponent();

            currentDriverId = userId;
            this.isAdmin = isAdmin;
            UserName.Text = (isAdmin ? "Администратор: " : "Водитель: ") + fullName;

            ApplyRoleAccess();
            ReloadAll();
        }

        private void ApplyRoleAccess()
        {
            if (isAdmin)
            {
                return;
            }

            DriversTab.Visibility = Visibility.Collapsed;
            MileageReportTab.Visibility = Visibility.Collapsed;
            AddVehicleButton.Visibility = Visibility.Collapsed;
            DriverFilterPanel.Visibility = Visibility.Collapsed;
            RoutesTab.Header = "Мои рейсы";
            AddRouteButton.Content = "Взять транспорт и оформить рейс";

            var vehicleActions = VehiclesGrid.Columns.FirstOrDefault(c => Convert.ToString(c.Header) == "Действия");
            if (vehicleActions != null)
            {
                vehicleActions.Visibility = Visibility.Collapsed;
            }
        }

        private bool RequireAdministrator()
        {
            if (isAdmin)
            {
                return true;
            }

            MessageBox.Show("Эта операция доступна только администратору.", "Недостаточно прав",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }


        private void ShowDbError()
        {
            MessageBox.Show("Ошибка работы с базой данных. Проверьте подключение к SQL Server!",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void ReloadAll()
        {
            RefreshStatuses();
            LoadFilterOptions();
            LoadVehicles();
            if (isAdmin)
            {
                LoadDrivers();
                LoadMileageReport();
            }
            LoadRoutes();
        }

        private void RefreshStatuses()
        {
            try
            {
                using (var db = new FleetDBEntities())
                {
                    FleetService.RefreshVehicleStatuses(db);
                }
            }
            catch (Exception)
            {
                ShowDbError();
            }
        }

        private static int SelectedId(ComboBox box)
        {
            return box.SelectedValue is int ? (int)box.SelectedValue : 0;
        }

        private void LoadFilterOptions()
        {
            int vehicleId = SelectedId(VehicleFilterBox);
            int driverId = SelectedId(DriverFilterBox);

            try
            {
                using (var db = new FleetDBEntities())
                {
                    var vehicles = db.Vehicles
                        .OrderBy(v => v.LicensePlate)
                        .ToList()
                        .Select(v => new Option { Id = v.Id, Title = v.LicensePlate + " - " + v.Title })
                        .ToList();
                    vehicles.Insert(0, new Option { Id = 0, Title = "Все транспортные средства" });

                    VehicleFilterBox.ItemsSource = vehicles;
                    VehicleFilterBox.SelectedValue = vehicles.Any(x => x.Id == vehicleId) ? vehicleId : 0;

                    if (isAdmin)
                    {
                        var drivers = db.Drivers
                            .OrderBy(d => d.FullName)
                            .Select(d => new Option { Id = d.Id, Title = d.FullName })
                            .ToList();
                        drivers.Insert(0, new Option { Id = 0, Title = "Все водители" });
                        DriverFilterBox.ItemsSource = drivers;
                        DriverFilterBox.SelectedValue = drivers.Any(x => x.Id == driverId) ? driverId : 0;
                    }
                }
            }
            catch (Exception)
            {
                ShowDbError();
            }
        }

        private void LogoutClick(object sender, RoutedEventArgs e)
        {
            var authWindow = new MainWindow();

            Application.Current.MainWindow = authWindow;
            authWindow.Show();

            Close();
        }


        private void LoadVehicles()
        {
            try
            {
                using (var db = new FleetDBEntities())
                {
                    var vehicles = db.Vehicles.AsQueryable();
                    var selectedItem = VehicleSortBox.SelectedItem as ComboBoxItem;
                    string sort = selectedItem == null ? "Plate" : Convert.ToString(selectedItem.Tag);

                    switch (sort)
                    {
                        case "MileageAsc":
                            vehicles = vehicles.OrderBy(v => v.Mileage).ThenBy(v => v.LicensePlate);
                            break;
                        case "MileageDesc":
                            vehicles = vehicles.OrderByDescending(v => v.Mileage).ThenBy(v => v.LicensePlate);
                            break;
                        case "YearAsc":
                            vehicles = vehicles.OrderBy(v => v.YearOfManufacture).ThenBy(v => v.LicensePlate);
                            break;
                        case "YearDesc":
                            vehicles = vehicles.OrderByDescending(v => v.YearOfManufacture).ThenBy(v => v.LicensePlate);
                            break;
                        case "Manufacturer":
                            vehicles = vehicles.OrderBy(v => v.Manufacturer).ThenBy(v => v.Model);
                            break;
                        default:
                            vehicles = vehicles.OrderBy(v => v.LicensePlate);
                            break;
                    }

                    VehiclesGrid.ItemsSource = vehicles.ToList();
                }
            }
            catch (Exception)
            {
                ShowDbError();
            }
        }

        private void SortVehicles_Click(object sender, RoutedEventArgs e)
        {
            LoadVehicles();
        }

        private void LoadMileageReport()
        {
            if (!isAdmin) return;

            try
            {
                using (var db = new FleetDBEntities())
                {
                    var vehicles = db.Vehicles
                        .Include(v => v.Routes)
                        .OrderBy(v => v.LicensePlate)
                        .ToList();

                    var rows = vehicles.Select(v => new
                    {
                        v.Id,
                        v.LicensePlate,
                        Vehicle = v.Manufacturer + " " + v.Model,
                        v.Mileage,
                        v.LastMaintenanceMileage,
                        NextMaintenanceMileage = v.NextMaintenanceMileage,
                        MaintenanceDate = v.MaintenanceDateText,
                        RouteDistance = v.Routes.Sum(r => r.Distance),
                        Maintenance = v.MaintenanceBadge == "" ? "Не требуется" : v.MaintenanceBadge
                    }).ToList();

                    MileageReportGrid.ItemsSource = rows;

                    int totalMileage = vehicles.Sum(v => v.Mileage);
                    int totalRouteDistance = vehicles.Sum(v => v.Routes.Sum(r => r.Distance));
                    MileageReportSummary.Text = "Суммарный текущий пробег автопарка: "
                        + totalMileage.ToString("N0") + " км. Расстояние по зарегистрированным рейсам: "
                        + totalRouteDistance.ToString("N0") + " км.";
                }
            }
            catch (Exception)
            {
                ShowDbError();
            }
        }

        private void RefreshMileageReport_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;
            LoadMileageReport();
        }

        private void AddVehicle_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;

            var window = new VehicleWindow(null);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                ReloadAll();
            }
        }

        private void EditVehicle_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;

            var vehicle = (sender as Button)?.DataContext as Vehicles;

            if (vehicle == null)
            {
                return;
            }

            var window = new VehicleWindow(vehicle);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                ReloadAll();
            }
        }

        private void MarkMaintenanceCompleted_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;

            var vehicle = (sender as Button)?.DataContext as Vehicles;
            if (vehicle == null) return;

            int nextMaintenance = vehicle.Mileage + FleetService.MaintenanceMileageInterval;
            var confirmation = MessageBox.Show(
                "Подтвердить, что техническое обслуживание автомобиля " + vehicle.LicensePlate + " пройдено?\n\n"
                + "Будет записан текущий пробег: " + vehicle.Mileage.ToString("N0") + " км.\n"
                + "Следующее ТО потребуется через " + FleetService.MaintenanceMileageInterval.ToString("N0")
                + " км — при пробеге " + nextMaintenance.ToString("N0") + " км.",
                "Подтверждение прохождения ТО",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes) return;

            try
            {
                using (var db = new FleetDBEntities())
                {
                    var target = db.Vehicles.Find(vehicle.Id);
                    if (target == null)
                    {
                        MessageBox.Show("Транспортное средство не найдено. Обновите список.");
                        ReloadAll();
                        return;
                    }

                    target.LastMaintenanceMileage = target.Mileage;
                    target.LastMaintenanceDate = DateTime.Today;
                    db.SaveChanges();
                }

                MessageBox.Show(
                    "ТО отмечено как пройденное. Следующее обслуживание потребуется через 10 000 км.",
                    "Техническое обслуживание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                ReloadAll();
            }
            catch (Exception)
            {
                ShowDbError();
            }
        }

        private void DeleteVehicle_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;

            var vehicle = (sender as Button)?.DataContext as Vehicles;

            if (vehicle == null)
            {
                return;
            }

            var result = MessageBox.Show("Удалить транспортное средство " + vehicle.LicensePlate + "?",
                "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (var db = new FleetDBEntities())
                {
                    int id = vehicle.Id;

                    if (db.Routes.Any(r => r.VehicleId == id))
                    {
                        MessageBox.Show("Нельзя удалить: у этого транспортного средства есть рейсы (история рейсов).");
                        return;
                    }

                    var target = db.Vehicles.Find(id);

                    if (target != null)
                    {
                        db.Vehicles.Remove(target);
                        db.SaveChanges();
                    }
                }
            }
            catch (DbUpdateException)
            {
                MessageBox.Show("Не удалось удалить запись: на неё есть ссылки в других таблицах.");
                return;
            }
            catch (Exception)
            {
                ShowDbError();
                return;
            }

            ReloadAll();
        }

        private void VehicleHistory_Click(object sender, RoutedEventArgs e)
        {
            var vehicle = (sender as Button)?.DataContext as Vehicles;

            if (vehicle == null)
            {
                return;
            }

            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            DriverFilterBox.SelectedValue = 0;
            VehicleFilterBox.SelectedValue = vehicle.Id;

            LoadRoutes();
            MainTabs.SelectedIndex = 2;
        }


        private void LoadDrivers()
        {
            if (!isAdmin) return;

            try
            {
                using (var db = new FleetDBEntities())
                {
                    DriversGrid.ItemsSource = db.Drivers
                        .OrderBy(d => d.FullName)
                        .ToList();
                }
            }
            catch (Exception)
            {
                ShowDbError();
            }
        }

        private void AddDriver_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;

            var window = new DriverWindow(null);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                ReloadAll();
            }
        }

        private void EditDriver_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;

            var driver = (sender as Button)?.DataContext as Drivers;

            if (driver == null)
            {
                return;
            }

            var window = new DriverWindow(driver);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                ReloadAll();
            }
        }

        private void DeleteDriver_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;

            var driver = (sender as Button)?.DataContext as Drivers;

            if (driver == null)
            {
                return;
            }

            var result = MessageBox.Show("Удалить водителя " + driver.FullName + "?",
                "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (var db = new FleetDBEntities())
                {
                    int id = driver.Id;

                    if (db.Routes.Any(r => r.DriverId == id))
                    {
                        MessageBox.Show("Нельзя удалить: у этого водителя есть рейсы (история рейсов).");
                        return;
                    }

                    var target = db.Drivers.Find(id);

                    if (target != null)
                    {
                        db.Drivers.Remove(target);
                        db.SaveChanges();
                    }
                }
            }
            catch (DbUpdateException)
            {
                MessageBox.Show("Не удалось удалить запись: на неё есть ссылки в других таблицах.");
                return;
            }
            catch (Exception)
            {
                ShowDbError();
                return;
            }

            ReloadAll();
        }

        private void DriverHistory_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdministrator()) return;

            var driver = (sender as Button)?.DataContext as Drivers;

            if (driver == null)
            {
                return;
            }

            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            VehicleFilterBox.SelectedValue = 0;
            DriverFilterBox.SelectedValue = driver.Id;

            LoadRoutes();
            MainTabs.SelectedIndex = 2;
        }

        private int LoadRoutes()
        {
            DateTime? dateFrom = DateFromPicker.SelectedDate;
            DateTime? dateTo = DateToPicker.SelectedDate;
            int vehicleId = SelectedId(VehicleFilterBox);
            int driverId = isAdmin ? SelectedId(DriverFilterBox) : currentDriverId;

            if (dateFrom != null && dateTo != null && dateFrom.Value.Date > dateTo.Value.Date)
            {
                MessageBox.Show("Дата «с» не может быть позже даты «по».");
                return -1;
            }

            try
            {
                using (var db = new FleetDBEntities())
                {
                    var routes = db.Routes
                        .Include(r => r.Vehicles)
                        .Include(r => r.Drivers)
                        .AsQueryable();

                    if (dateFrom != null)
                    {
                        DateTime start = dateFrom.Value.Date;
                        routes = routes.Where(r => r.StartDate >= start);
                    }

                    if (dateTo != null)
                    {
                        DateTime end = dateTo.Value.Date;
                        routes = routes.Where(r => r.StartDate <= end);
                    }

                    if (vehicleId > 0)
                    {
                        routes = routes.Where(r => r.VehicleId == vehicleId);
                    }

                    if (driverId > 0)
                    {
                        routes = routes.Where(r => r.DriverId == driverId);
                    }

                    var list = routes.OrderByDescending(r => r.StartDate).ThenByDescending(r => r.Id).ToList();

                    RoutesGrid.ItemsSource = list;
                    return list.Count;
                }
            }
            catch (Exception)
            {
                ShowDbError();
                return -1;
            }
        }

        private void ApplyRouteFilter(object sender, RoutedEventArgs e)
        {
            if (LoadRoutes() == 0)
            {
                MessageBox.Show("По вашему запросу ничего не найдено.", "Поиск");
            }
        }

        private void ResetRouteFilter(object sender, RoutedEventArgs e)
        {
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            VehicleFilterBox.SelectedValue = 0;
            DriverFilterBox.SelectedValue = 0;

            LoadRoutes();
        }

        private void AddRoute_Click(object sender, RoutedEventArgs e)
        {
            var window = new RouteWindow(isAdmin ? (int?)null : currentDriverId, isAdmin);
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                ReloadAll();
            }
        }


    }
}
