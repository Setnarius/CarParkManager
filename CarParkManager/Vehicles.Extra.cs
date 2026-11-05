using System;

namespace CarParkManager
{
    public partial class Vehicles
    {
        public string Title
        {
            get { return Manufacturer + " " + Model; }
        }

        // Дата последнего ТО для удобного отображения в таблице.
        public string MaintenanceDateText
        {
            get
            {
                return LastMaintenanceDate.HasValue
                    ? LastMaintenanceDate.Value.ToString("dd.MM.yyyy")
                    : "Не отмечено";
            }
        }

        // Следующее ТО требуется через каждые 10 000 км после последнего пройденного ТО.
        public int NextMaintenanceMileage
        {
            get { return LastMaintenanceMileage + FleetService.MaintenanceMileageInterval; }
        }

        public int MileageUntilMaintenance
        {
            get { return Math.Max(0, NextMaintenanceMileage - Mileage); }
        }

        public bool MaintenanceIsDue
        {
            get { return Mileage >= NextMaintenanceMileage; }
        }

        // Бейдж отображается, если с последнего ТО автомобиль прошёл 10 000 км или больше.
        public string MaintenanceBadge
        {
            get { return MaintenanceIsDue ? "Нужно ТО" : ""; }
        }
    }
}
