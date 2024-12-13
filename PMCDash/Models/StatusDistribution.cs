using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class StatusDistribution
    {
        public StatusDistribution(decimal run, decimal idle, decimal alarm, decimal off)
        {
            Run = run;
            Idle = idle;
            Alarm = alarm;
            Off = off;
        }

        /// <summary>
        /// 運轉比例
        /// </summary>
        public decimal Run { get; set; }

        /// <summary>
        /// 待機比例
        /// </summary>
        public decimal Idle { get; set; }

        /// <summary>
        /// 警報比例
        /// </summary>
        public decimal Alarm { get; set; }

        /// <summary>
        /// 停機比例
        /// </summary>
        public decimal Off { get; set; }
    }

    public class FactorysStatusDistribution
    {
        public FactorysStatusDistribution(string factoryName, StatusDistribution distribution)
        {
            FactoryName = factoryName;
            Distribution = distribution;
        }

        public string FactoryName { get; set; }

        public StatusDistribution Distribution { get; set; }
    }

    public class ProductionLineStatusDistribution
    {
        public ProductionLineStatusDistribution(string productionLineName, StatusDistribution distribution)
        {
            ProductionLineName = productionLineName;
            Distribution = distribution;
        }

        public string ProductionLineName { get; set; }

        public StatusDistribution Distribution {get;set;}
    }
}
