using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class StatusStatistics
    {
        public StatusStatistics(int run, int idle, int alarm, int off)
        {
            Run = run;
            Idle = idle;
            Alarm = alarm;
            Off = off;
        }

        /// <summary>
        /// 運轉
        /// </summary>
        public int Run { get; set; }
        /// <summary>
        /// 閒置
        /// </summary>
        public int Idle { get; set; }
        /// <summary>
        /// 警報
        /// </summary>
        public int Alarm { get; set; }
        /// <summary>
        /// 關機
        /// </summary>
        public int Off { get; set; }
    }

    public class FactoryStatistics
    {
        public FactoryStatistics(string factoryName, StatusStatistics statistics)
        {
            FactoryName = factoryName;
            Statistics = statistics;
        }

        public string FactoryName { get; set; }

        public StatusStatistics Statistics { get; set; }
    }

    public class ProductionLineStatistics
    {
        public ProductionLineStatistics(string productionLineName, StatusStatistics statistics)
        {
            ProductionLineName = productionLineName;
            Statistics = statistics;
        }

        public string ProductionLineName { get; set; }

        public StatusStatistics Statistics { get; set; }
    }

    public class FactoryStatisticsImformation
    {
        public FactoryStatisticsImformation(string factoryName, List<ProductionLineStatistics> stattistics)
        {
            FactoryName = factoryName;
            Stattistics = stattistics;
        }

        public string FactoryName { get; set; }

        public List<ProductionLineStatistics> Stattistics { get; set; }
    }

    public class ProductionLineMachineImformation
    {
        public ProductionLineMachineImformation(List<MachineStatus> statusImformation)
        {
            StatusImformation = statusImformation;
        }

        public List<MachineStatus> StatusImformation { get; set; }
    }

    public class MachineStatus
    {
        public MachineStatus(string machineName, string status)
        {
            MachineName = machineName;
            Status = status;
        }

        public string MachineName { get; set; }

        public string Status { get; set; }
    }

    public class DeviceList
    {
        public DeviceList(string remark, string workorderID, string opid, string maktx)
        {
            Remark = remark;
            WorkOrderID = workorderID;
            OPID = opid;
            MAKTX = maktx;
        }
        /// <summary>
        /// 機台名稱
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 工單編號
        /// </summary>
        public string WorkOrderID { get; set; }
        /// <summary>
        /// 製程編號
        /// </summary>
        public string OPID { get; set; }
        /// <summary>
        /// 產品名稱
        /// </summary>
        public string MAKTX { get; set; }
    }


    public class AlarmStatistics
    {
        public AlarmStatistics(string alarmMSg, int times, double totalMin, string display)
        {
            AlarmMSg = alarmMSg;
            Times = times;
            TotalMin = totalMin;
            Display = display;
        }
        /// <summary>
        /// 警報編號
        /// </summary>
        public string AlarmMSg { get; set; }
        /// <summary>
        /// 總次數
        /// </summary>
        public int Times { get; set; }
        /// <summary>
        /// 總時間
        /// </summary>
        public double TotalMin { get; set; }
        /// <summary>
        /// 待確認用途
        /// </summary>
        public string Display { get; set; }
    }

    public class StopStatistics
    {
        public StopStatistics(string machineName, int times)
        {
            MachineName = machineName;
            Times = times;
        }

        /// <summary>
        /// 機台名稱
        /// </summary>
        public string MachineName { get; set; }
        /// <summary>
        /// 停機次數
        /// </summary>
        public int Times { get; set; }
    }

    public class MachineStatusStatistics
    {
        public MachineStatusStatistics(string status, double percent, string dateDisplay)
        {
            Status = status;
            Percent = percent;
            DateDisplay = dateDisplay;
        }

        public string Status { get; set; }

        public double Percent { get; set; }

        public string DateDisplay { get; set; }
    }
 
}
