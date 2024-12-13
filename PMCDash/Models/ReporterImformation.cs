using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class ReporterImformation
    {
        /// <summary>
        /// 機台名稱
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 操作人員
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 工單號
        /// </summary>
        public string OrderID { get; set; }

        /// <summary>
        /// 料號
        /// </summary>
        public string Maktxt { get; set; }

        /// <summary>
        /// 批號數量
        /// </summary>
        public int OringinCount { get; set; }

        /// <summary>
        /// 報工數量
        /// </summary>
        public int RepotedConut { get; set; }

        /// <summary>
        /// 工單狀態
        /// </summary>
        public string OrderStatus { get; set; }
    }
}
