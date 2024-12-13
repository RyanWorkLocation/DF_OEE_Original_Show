using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PMCDash.Models;
using PMCDash.Services;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace PMCDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DistributionController : BaseApiController
    {
        private readonly DeviceDistributionService _deviceDistributionService;
        ConnectStr _ConnectStr = new ConnectStr();
        public DistributionController(DeviceDistributionService deviceDistributionService)
        {
            _deviceDistributionService = deviceDistributionService;
        }

        /// <summary>
        /// 取得各廠機台狀態分布比例
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResponse<List<FactorysStatusDistribution>> Get()
        {
            var result = new List<FactorysStatusDistribution>();


            double run = 0;
            double idle = 0;
            double alarm = 0;
            double off = 0;
            var devices = new List<MachineStatus>();

            #region 由實際生產資料統計廠區機台狀態
            //取得工單資料
            var sqlStr = @$"-- 從 skymars 來
                        SELECT 
                            a.DeviceID,
                            a.DeviceStatus
                        FROM 
                            {_ConnectStr.SkyMarsDB}.[dbo].[DeviceCurrentStatus] AS a
                        LEFT JOIN 
                            {_ConnectStr.APSDB}.[dbo].[Device] AS d ON a.DeviceID = d.remark
                        WHERE 
                            d.SkyMars_connect = 1

                        UNION ALL

                        -- 從 MES 來
                        SELECT b.remark as DeviceID ,
	                    CASE 
	                     WHEN a.WorkOrderID is null THEN 'IDLE'
	                     WHEN a.WorkOrderID is not null THEN 'RUN'
	                     ELSE 'Unknown Status'
	                     END AS DeviceStatus
	                      FROM {_ConnectStr.APSDB}.[dbo].[WipRegisterLog] as a
	                      INNER JOIN {_ConnectStr.APSDB}.[dbo].[Device] as b
	                      on a.DeviceID = b.ID
	                    where b.SkyMars_connect=0 and b.external_com=0
                    ";
            using (var conn = new SqlConnection(_ConnectStr.Local))
            {
                using (var comm = new SqlCommand(sqlStr, conn))
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    using (SqlDataReader SqlData = comm.ExecuteReader())
                    {
                        if (SqlData.HasRows)
                        {
                            while (SqlData.Read())
                            {

                                devices.Add(new MachineStatus(
                                    machineName: SqlData["DeviceID"].ToString().Trim(),
                                    status: SqlData["DeviceStatus"].ToString().Trim()

                                    ));

                            };
                        }
                    }
                }
            }

            run = devices.Exists(x => x.Status == "RUN") ? devices.Where(x => x.Status == "RUN").Count() : 0;
            idle = devices.Exists(x => x.Status == "IDLE") ? devices.Where(x => x.Status == "IDLE").Count() : 0;
            alarm = devices.Exists(x => x.Status == "ALARM") ? devices.Where(x => x.Status == "ALARM").Count() : 0;
            off = devices.Exists(x => x.Status == "OFF") ? devices.Where(x => x.Status == "OFF").Count() : 0;
            #endregion
            StatusDistribution statusDistribution = new StatusDistribution
            (
                run: (decimal)Math.Round(run / devices.Count() * 100, 1),
                idle: (decimal)Math.Round(idle / devices.Count() * 100, 1),
                alarm: (decimal)Math.Round(alarm / devices.Count() * 100, 1),
                off: (decimal)Math.Round(off / devices.Count() * 100, 1)
            );

            result.Add(new FactorysStatusDistribution
                (
                    factoryName: "安南新廠",
                    distribution: statusDistribution

                ));

            return new ActionResponse<List<FactorysStatusDistribution>>
            {
                Data = result
            };
        }

      /// <summary>
      /// 取得各產線狀態分布
      /// </summary>
      /// <param name="factory">廠區代號</param>
      /// <returns></returns>
        [HttpGet("productionline/{factory}")]
        public ActionResponse<List<ProductionLineStatusDistribution>> GetPrductionLines(string factory)
        {
            var result = new List<ProductionLineStatusDistribution>();

            double run = 0;
            double idle = 0;
            double alarm = 0;
            double off = 0;
            var devices = new List<MachineStatus>();

            #region 由實際生產資料統計廠區機台狀態
            //取得工單資料
            var sqlStr = @$"-- 從 skymars 來
                        SELECT 
                            a.DeviceID,
                            a.DeviceStatus
                        FROM 
                            {_ConnectStr.SkyMarsDB}.[dbo].[DeviceCurrentStatus] AS a
                        LEFT JOIN 
                            {_ConnectStr.APSDB}.[dbo].[Device] AS d ON a.DeviceID = d.remark
                        WHERE 
                            d.SkyMars_connect = 1

                        UNION ALL

                        -- 從 MES 來
                        SELECT b.remark as DeviceID ,
	                    CASE 
	                     WHEN a.WorkOrderID is null THEN 'IDLE'
	                     WHEN a.WorkOrderID is not null THEN 'RUN'
	                     ELSE 'Unknown Status'
	                     END AS DeviceStatus
	                      FROM {_ConnectStr.APSDB}.[dbo].[WipRegisterLog] as a
	                      INNER JOIN {_ConnectStr.APSDB}.[dbo].[Device] as b
	                      on a.DeviceID = b.ID
	                    where b.SkyMars_connect=0 and b.external_com=0
                    ";
            using (var conn = new SqlConnection(_ConnectStr.Local))
            {
                using (var comm = new SqlCommand(sqlStr, conn))
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    using (SqlDataReader SqlData = comm.ExecuteReader())
                    {
                        if (SqlData.HasRows)
                        {
                            while (SqlData.Read())
                            {

                                devices.Add(new MachineStatus(
                                    machineName: SqlData["DeviceID"].ToString().Trim(),
                                    status: SqlData["DeviceStatus"].ToString().Trim()

                                    ));

                            };
                        }
                    }
                }
            }
            run = devices.Exists(x => x.Status == "RUN") ? devices.Where(x => x.Status == "RUN").Count() : 0;
            idle = devices.Exists(x => x.Status == "IDLE") ? devices.Where(x => x.Status == "IDLE").Count() : 0;
            alarm = devices.Exists(x => x.Status == "ALARM") ? devices.Where(x => x.Status == "ALARM").Count() : 0;
            off = devices.Exists(x => x.Status == "OFF") ? devices.Where(x => x.Status == "OFF").Count() : 0;
            #endregion

            //StatusDistribution statusDistribution = new StatusDistribution
            //(
            //    run: (decimal)run,
            //    idle: (decimal)idle,
            //    alarm: (decimal)alarm,
            //    off: (decimal)off
            //);

            StatusDistribution statusDistribution = new StatusDistribution
            (
                run: (decimal)Math.Round(run / devices.Count() * 100, 1),
                idle: (decimal)Math.Round(idle / devices.Count() * 100, 1),
                alarm: (decimal)Math.Round(alarm / devices.Count() * 100, 1),
                off: (decimal)Math.Round(off / devices.Count() * 100, 1)
            );


            result.Add(new ProductionLineStatusDistribution
                (
                    productionLineName: "全產線",
                    distribution: statusDistribution

                ));


            //for (int i = 0; i < 5; i++)
            //{
            //    result.Add(new ProductionLineStatusDistribution
            //    (
            //        $@"PRL-0{i + 1}",
            //        new StatusDistribution
            //        (
            //            run: 52.3m,
            //            idle: 27.8m,
            //            alarm: 16.7m,
            //            off: 3.2m
            //        )
            //    ));
            //}

            return new ActionResponse<List<ProductionLineStatusDistribution>>
            {
                Data = result
            };
        }

        /// <summary>
        /// 取得全廠機台狀態分布比例(狀態比例)
        /// </summary>
        /// <returns></returns>
        [HttpGet("all")]
        public ActionResponse<StatusDistribution> GetAllStatusDistribution()
        {

            double run = 0;
            double idle = 0;
            double alarm = 0;
            double off = 0;
            var devices = new List<MachineStatus>();

            #region 由實際生產資料統計廠區機台狀態
            //取得工單資料
            var sqlStr = @$"-- 從 skymars 來
                        SELECT 
                            a.DeviceID,
                            a.DeviceStatus
                        FROM 
                            {_ConnectStr.SkyMarsDB}.[dbo].[DeviceCurrentStatus] AS a
                        LEFT JOIN 
                            {_ConnectStr.APSDB}.[dbo].[Device] AS d ON a.DeviceID = d.remark
                        WHERE 
                            d.SkyMars_connect = 1

                        UNION ALL

                        -- 從 MES 來
                        SELECT b.remark as DeviceID ,
	                    CASE 
	                     WHEN a.WorkOrderID is null THEN 'IDLE'
	                     WHEN a.WorkOrderID is not null THEN 'RUN'
	                     ELSE 'Unknown Status'
	                     END AS DeviceStatus
	                      FROM {_ConnectStr.APSDB}.[dbo].[WipRegisterLog] as a
	                      INNER JOIN {_ConnectStr.APSDB}.[dbo].[Device] as b
	                      on a.DeviceID = b.ID
	                    where b.SkyMars_connect=0 and b.external_com=0
                    ";
            using (var conn = new SqlConnection(_ConnectStr.Local))
            {
                using (var comm = new SqlCommand(sqlStr, conn))
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    using (SqlDataReader SqlData = comm.ExecuteReader())
                    {
                        if (SqlData.HasRows)
                        {
                            while (SqlData.Read())
                            {

                                devices.Add(new MachineStatus(
                                    machineName: SqlData["DeviceID"].ToString().Trim(),
                                    status: SqlData["DeviceStatus"].ToString().Trim()

                                    ));

                            };
                        }
                    }
                }
            }

            run = devices.Exists(x=>x.Status == "RUN")?devices.Where(x => x.Status == "RUN").Count() : 0;
            idle = devices.Exists(x => x.Status == "IDLE") ? devices.Where(x => x.Status == "IDLE").Count() : 0;
            alarm = devices.Exists(x => x.Status == "ALARM") ? devices.Where(x => x.Status == "ALARM").Count() : 0;
            off = devices.Exists(x => x.Status == "OFF") ? devices.Where(x => x.Status == "OFF").Count() : 0;

            #endregion

            /////////////////////////////////////////////////////////////
            // 將結果封裝到 StatusDistribution 對象中
            StatusDistribution statusDistribution = new StatusDistribution
            (
                run: (decimal) Math.Round(run/ devices.Count()*100,1),
                idle: (decimal) Math.Round(idle / devices.Count()*100, 1),
                alarm: (decimal) Math.Round(alarm / devices.Count()*100, 1),
                off: (decimal) Math.Round(off / devices.Count()*100, 1)
            );

            // 返回 ActionResponse
            return new ActionResponse<StatusDistribution>
            {
                Data = statusDistribution
            };
        }

    }
}
