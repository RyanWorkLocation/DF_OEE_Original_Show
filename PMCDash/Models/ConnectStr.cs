using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class ConnectStr
    {
        /// <summary>
        /// 本地端連線字串
        /// </summary>
        public readonly string Local = @"Data Source=127.0.0.1;Initial Catalog=DPI ;User ID=MES2014;Password=PMCMES;";
        /// <summary>
        /// 外網連線字串
        /// </summary>
        public readonly string Remote = @"Data Source=192.168.0.156;Initial Catalog=V3 ;User ID=MES2014;Password=PMCMES;";

        public ConnectStr()
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json");
            var config = builder.Build();
            Local = config["ConnectionStrings:DefaultConnectionString"];
            APSDB = config["OtherDB:APS"];
            MRPDB = config["OtherDB:MRP"];
            MeasureDB = config["OtherDB:Mesure"];
            SkyMarsDB = config["OtherDB:SkyMars"];
            AccuntDB = config["OtherDB:Account"];
            ERPurl = config["OtherDB:ERPurl"];
            AdminCode = int.Parse(config["OtherDB:AdminCode"]);
        }

        /// <summary>
        /// 排程報工資料庫名稱
        /// </summary>
        //public readonly string APSDB = "[01_APS]";
        public readonly string APSDB = "[DPI]";

        /// <summary>
        /// ERP/MRP串接資料中介區資料庫名稱
        /// </summary>
        //public readonly string MRPDB = "[01_MRP]";
        public readonly string MRPDB = "[MRP_Lite]";

        /// <summary>
        /// 無線量測資料庫名稱
        /// </summary>
        public readonly string MeasureDB = "[Measure_Lite]";

        /// <summary>
        /// SkyMars資料庫名稱
        /// </summary>
        public readonly string SkyMarsDB = "[SkyMarsDB]";

        /// <summary>
        /// 系統帳號資料庫名稱
        /// </summary>
        public readonly string AccuntDB = "[soco]";

        /// <summary>
        /// ERP的URL
        /// </summary>
        public readonly string ERPurl = "[soco]";

        /// <summary>
        /// 跌代次數
        /// </summary>
        public readonly int iteration =300;

        /// <summary>
        /// 染色體數
        /// </summary>
        public readonly int chromosomesCount = 10;

        /// <summary>
        /// 管理者權限code
        /// </summary>
        public readonly int AdminCode = 10;

        /// <summary>
        /// Debug Mode
        /// </summary>
        public readonly int Debug = 0;

        /// <summary>
        /// 客製化版:0為標準版、1為客製化版
        /// </summary>
        public readonly int Customer = 0;

        /// <summary>
        /// SSSSSSSSSSSSSSS
        /// </summary>
        public readonly DateTime DateTimeNow = DateTime.Now;

    }
}
