using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PMCDash.Repos;
using PMCDash.Models;
namespace PMCDash.Services
{
    public class DeviceDistributionService
    {
        private readonly DistributionRepo _distributionRepo;

        public DeviceDistributionService (DistributionRepo distributionRepo)
        {
            _distributionRepo = distributionRepo;
        }

        public StatusDistribution GetDeviceStatusDistributionByFacotry(string filter = "")
        {
            var data = _distributionRepo.GetDeviceStatusDistribution();
            var dataCount = (decimal) data.Count;

            var result = new StatusDistribution
            (
                 run: Math.Round(data.Where(x => x.Status == "RUN").Count() / dataCount * 100, 2),
                 idle: Math.Round(data.Where(x => x.Status == "IDLE").Count() / dataCount * 100m, 2),
                 alarm: Math.Round((data.Where(x => x.Status == "ALARM").Count() / dataCount * 100), 2),
                 off: Math.Round((data.Where(x => x.Status == "OFF").Count() / dataCount * 100), 2)
            );
            if (filter != "")
            {
                result.Run /= 5;
                result.Idle /= 5;
                result.Alarm /= 5;
                result.Off /= 5;
            }
            return result;
        }
    }
}
