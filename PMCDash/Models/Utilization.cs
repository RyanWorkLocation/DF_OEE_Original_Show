using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{

    public class WeekUtilization
    {
        public WeekUtilization(string deviceName, List<Utilization> utilizations)
        {
            DeviceName = deviceName;
            Utilizations = utilizations;
        }

        /// <summary>
        /// 機台名稱
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 運轉狀態參數
        /// </summary>
        public virtual List<Utilization> Utilizations { get; set; }
    }

    public class Utilization
    {
        public Utilization(string date, double run, double idle, double alarm, double off)
        {
            Date = date;
            Run = run;
            Idle = idle;
            Alarm = alarm;
            Off = off;
        }

        public string Date { get; set; }

        public double Run { get; set; }

        public double Idle { get; set; }

        public double Alarm { get; set; }

        public double Off { get; set; }
    }
}
