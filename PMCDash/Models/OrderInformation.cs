using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class OrderInformation
    {
        public OrderInformation(string orderNo, int oPNo, string opName, string productNo, int requireCount, int currentCount, string dueDate, string customerinfo)
        {
            OrderNo = orderNo;
            OPNo = oPNo;
            OPName = opName;
            ProductNo = productNo;
            RequireCount = requireCount;
            CurrentCount = currentCount;
            DueDate = dueDate;
            CustomerInfo = customerinfo;
        }

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
        /// 顧客資訊
        /// </summary>
        public string CustomerInfo { get; set; }
    }
}
