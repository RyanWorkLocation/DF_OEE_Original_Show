using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class ActionRequest<T>
    {
        public string Action { get; set; }

        public T Data { get; set; }
    }

    public class RequestFactory
    {
        /// <summary>
        /// 廠區名稱
        /// </summary>
        public string FactoryName { get; set; }

        /// <summary>
        /// 產線名稱
        /// </summary>
        public string ProductionName { get; set; }


        /// <summary>
        /// 機台名稱
        /// </summary>
        public string DeviceName { get; set; }

    }

    public class RequestProductionLine
    {
        /// <summary>
        /// 廠區名稱
        /// </summary>
        public string FactoryName { get; set; }

        /// <summary>
        /// 產線名稱
        /// </summary>
        public string ProductionName { get; set; }
    }
}
