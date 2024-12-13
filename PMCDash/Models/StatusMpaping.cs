using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class StatusMpaping
    {
        public StatusMpaping(string wipStatus, string wipStatusText, string wipStatusColor)
        {
            WipStatus = wipStatus;
            WipStatusText = wipStatusText;
            WipStatusColor = wipStatusColor;
        }

        /// <summary>
        /// WIP狀態
        /// </summary>
        public string WipStatus { get; set; }

        /// <summary>
        /// WIP狀態描述
        /// </summary>
        public string WipStatusText { get; set; }

        /// <summary>
        /// WIP狀態顏色
        /// </summary>
        public string WipStatusColor { get; set; }
    }
}
