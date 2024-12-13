using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class ScheduleInformation
    {
        public ScheduleInformation(OrderInformation orderInfo, string wIPStatus, string stratTime, string endTime, int delayDays)
        {
            OrderInfo = orderInfo;
            WIPStatus = wIPStatus;
            StratTime = stratTime;
            EndTime = endTime;
            DelayDays = delayDays;
        }

        public OrderInformation OrderInfo { get; set; }
        /// <summary>
        /// 製程狀態
        /// </summary>
        public string WIPStatus { get; set; }
        /// <summary>
        /// 開始時間
        /// </summary>
        public string StratTime { get; set; }
        /// <summary>
        /// 結束時間
        /// </summary>
        public string EndTime { get; set; }
        /// <summary>
        /// 延遲天數
        /// </summary>
        public int DelayDays { get; set; }
    }

    public class OrderInfoTemp
    {
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
        /// 製程狀態
        /// </summary>
        public string WIPStatus { get; set; }
        /// <summary>
        /// 開始時間
        /// </summary>
        public string StratTime { get; set; }
        /// <summary>
        /// 結束時間
        /// </summary>
        public string EndTime { get; set; }
        /// <summary>
        /// 延遲天數
        /// </summary>
        public int DelayDays { get; set; }
        /// <summary>
        /// 顧客資訊
        /// </summary>
        public string CustomerInfo { get; set; }
    }

    public class DelayOrderList
    {
        /// <summary>
        /// 工單編號
        /// </summary>
        public string OrderID { get; set; }
        /// <summary>
        /// 預交日期
        /// </summary>
        public string AssignData { get; set; }
    }
    public class DelayOrderInformation
    {
        /// <summary>
        /// 製程編號
        /// </summary>
        public string OPID { get; set; }
        /// <summary>
        /// 製程名稱
        /// </summary>
        public string OPName { get; set; }
        /// <summary>
        /// 製程狀態
        /// </summary>
        public string OPStatus { get; set; }
        /// <summary>
        /// 生產進度
        /// </summary>
        public double Progress { get; set; }
        /// <summary>
        /// 需求數量
        /// </summary>
        public int OrderQty { get; set; }
        /// <summary>
        /// 完成數量
        /// </summary>
        public int CompleteQty { get; set; }
    }
}
