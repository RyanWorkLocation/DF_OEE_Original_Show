using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace PMCDash.Models
{
    public class DeviceStatus
    {
        public DeviceStatus(string deviceName, string status)
        {
            DeviceName = deviceName;
            Status = status;
        }

        public string DeviceName { get; set; }

        public string Status { get; set; }
    }
}
