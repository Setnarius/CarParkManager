using System;
using System.Linq;

namespace CarParkManager
{
    public static class VehicleStatus
    {
        public const string Available = "Доступен";
        public const string OnRoute = "В рейсе";
        public const string Repair = "В ремонте";
    }

    public class Option
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public static class FleetService
    {
        public const int MaintenanceMileageInterval = 10000;

        public static void RefreshVehicleStatuses(FleetDBEntities db)
        {
            DateTime today = DateTime.Today;

            var busyIds = db.Routes
                .Where(r => r.StartDate <= today && r.EndDate >= today)
                .Select(r => r.VehicleId)
                .Distinct()
                .ToList();

            bool changed = false;

            foreach (var vehicle in db.Vehicles.ToList())
            {
                if (vehicle.Status == VehicleStatus.Repair)
                {
                    continue;
                }

                string status = busyIds.Contains(vehicle.Id) ? VehicleStatus.OnRoute : VehicleStatus.Available;

                if (vehicle.Status != status)
                {
                    vehicle.Status = status;
                    changed = true;
                }
            }

            if (changed)
            {
                db.SaveChanges();
            }
        }
    }
}
