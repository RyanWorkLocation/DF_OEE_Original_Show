using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class OperationInfo
    {
        public OperationInfo(double utilizationRate, string status, double productionProgress, string customName, OrderInformation orderInfo, string deviceImg)
        {
            UtilizationRate = utilizationRate;
            Status = status;
            ProductionProgress = productionProgress;
            CustomName = customName;
            OrderInfo = orderInfo;
            DeviceImg = deviceImg;
        }

        /// <summary>
        /// 稼動率
        /// </summary>
        public double UtilizationRate { get; set; }
        
        /// <summary>
        /// 當前機台運轉狀態
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 當前生產進度
        /// </summary>
        public double ProductionProgress { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CustomName { get; set; }

        public OrderInformation OrderInfo {get;set;}

        /// <summary>
        /// 機台圖片
        /// </summary>
        public string DeviceImg { get; set; }
    }

    public class DeviceInfoTemp
    {
        /// <summary>
        /// 加工狀態
        /// </summary>
        public string WIPEvent { get; set; }

        /// <summary>
        /// 工單名稱
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// 工序編號
        /// </summary>
        public int OPNo { get; set; }

        /// <summary>
        /// 工序名稱
        /// </summary>
        public string OPName { get; set; }

        /// <summary>
        /// 產品名稱
        /// </summary>
        public string ProductNo { get; set; }

        /// <summary>
        /// 工單數量
        /// </summary>
        public int RequireCount { get; set; }

        /// <summary>
        /// 當前數量
        /// </summary>
        public int CurrentCount { get; set; }

        /// <summary>
        /// 預交日期
        /// </summary>
        public string DueDate { get; set; }
        
        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CustomName { get; set; }
        
        /// <summary>
        /// 當前生產進度
        /// </summary>
        public double ProductionProgress { get; set; }

        /// <summary>
        /// 機台圖片
        /// </summary>
        public string DeviceImg { get; set; }
    }
}
