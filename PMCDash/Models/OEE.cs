using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class OEERate
    {
        public OEERate(double oee, double oeeLimit)
        {
            OEE = oee;
            OEELimit = oeeLimit;
        }

        public double OEE { get; set; }
        public double OEELimit { get; set; }
    }

    public class AvailbilityRate
    {
        public AvailbilityRate(double availbility, double availbilityLimit)
        {
            Availbility = availbility;
            AvailbilityLimit = availbilityLimit;
        }

        public double Availbility { get; set; }
        public double AvailbilityLimit { get; set; }
    }

    public class PerformanceRate
    {
        public PerformanceRate(double performance, double performanceLimit)
        {
            Performance = performance;
            PerformanceLimit = performanceLimit;
        }

        public double Performance { get; set; }
        public double PerformanceLimit { get; set; }
    }

    public class YieldRate
    {
        public YieldRate(double yield, double yieldLimit)
        {
            Yield = yield;
            YieldLimit = yieldLimit;
        }

        public double Yield { get; set; }

        public double YieldLimit { get; set; }
    }

    public class DeliveryRate
    {
        public DeliveryRate(double delivery, double deliveryLimit)
        {
            Delivery = delivery;
            DeliveryLimit = deliveryLimit;
        }

        public double Delivery { get; set; }

        public double DeliveryLimit { get; set; }
    }

    public class OEEOverViewHistory
    {
        public OEEOverViewHistory(string date, OEEOverView oeeOverView)
        {
            Date = date;
            this.oeeOverView = oeeOverView;
        }

        /// <summary>
        /// 日期
        /// </summary>
        public string Date { get; set; }
        /// <summary>
        /// OEE參數
        /// </summary>
        public OEEOverView oeeOverView { get; set; }
    }

    public class OEEOverView
    {
        public OEEOverView(OEERate oEE, AvailbilityRate availbility, PerformanceRate performance, YieldRate yield, DeliveryRate delivery)
        {
           
            OEE = oEE;
            Availbility = availbility;
            Performance = performance;
            Yield = yield;
            Delivery = delivery;
        }
        public OEERate OEE { get; set; }
        /// <summary>
        /// 時間稼動率
        /// </summary>
        public AvailbilityRate Availbility { get; set; }
        /// <summary>
        /// 性能稼動率
        /// </summary>
        public PerformanceRate Performance { get; set; }
        /// <summary>
        /// 良率
        /// </summary>
        public YieldRate Yield { get; set; }
        /// <summary>
        /// 準交率
        /// </summary>
        public DeliveryRate Delivery { get; set; }
    }

    public class YiledDetails
    {
        public YiledDetails(string proudctName, double rateValue)
        {
            ProudctName = proudctName;
            RateValue = rateValue;
        }
        /// <summary>
        /// 產品名稱
        /// </summary>
        public string ProudctName { get; set; }
        /// <summary>
        /// 良品率
        /// </summary>
        public double RateValue { get; set; }
    }
    public class Availability
    {
        public Availability(string deviceid, DateTime date, double actualrun)
        {
            DeviceID = deviceid;
            Date = date;
            ActualRun = actualrun;
        }
        /// <summary>
        /// 機台編號
        /// </summary>
        public string DeviceID { get; set; }
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// 實際運轉時間(分鐘)
        /// </summary>
        public double ActualRun { get; set; }
    }

    public class Performance
    {
        public Performance(string deviceid, DateTime date, double totaltime)
        {
            DeviceID = deviceid;
            Date = date;
            TotalTime = totaltime;
        }
        /// <summary>
        /// 機台編號
        /// </summary>
        public string DeviceID { get; set; }
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// 實際加工時間(分鐘)
        /// </summary>
        public double TotalTime { get; set; }
    }
}
