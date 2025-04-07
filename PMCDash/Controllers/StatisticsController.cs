using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMCDash.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using PMCDash.Services;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace PMCDash.Controllers
{
    [Route("api/[controller]")]
    public class StatisticsController : BaseApiController
    {
        private readonly AlarmService _alarmService;
        ConnectStr _ConnectStr = new ConnectStr();
        public StatisticsController(AlarmService alarmService)
        {
            _alarmService = alarmService;
        }

        /// <summary>
        /// 取得全場域各廠狀態統計資料(狀態個數)
        /// </summary>
        /// <returns></returns>
        public ActionResponse<List<FactoryStatistics>> GetStaticsByFactory()
        {
            var result = new List<FactoryStatistics>();

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
                            d.SkyMars_connect = 1 and d.Location_zone is not null

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
	                    where b.SkyMars_connect=0 and b.external_com=0 AND b.Location_zone is not null
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

            result.Add(new FactoryStatistics
                (
                    factoryName : "安南新廠",
                    statistics :
                    new StatusStatistics
                    (
                        run: (int)run,
                        idle: (int)idle,
                        alarm: (int)alarm,
                        off: (int)off
                    )
                ));
            return new ActionResponse<List<FactoryStatistics>>
            {
                Data = result
            };
        }

        /// <summary>
        /// 取得特定廠區中的產線統計資料
        /// </summary>
        /// <param name="factory">廠區名稱</param>
        /// <returns></returns>
        [HttpGet("productionline/{factory}")]
        public ActionResponse<FactoryStatisticsImformation> GetStaticsByProductionLine(string factory)
        {
            var result = new List<ProductionLineStatistics>();


            double run = 0;
            double idle = 0;
            double alarm = 0;
            double off = 0;
            var devices = new List<MachineStatus_Zone>();

            #region 由實際生產資料統計廠區機台狀態
            //取得工單資料
            var sqlStr = @$"-- 從 skymars 來
                        SELECT 
                            a.DeviceID,
                            a.DeviceStatus,
                            d.Location_Zone
                        FROM 
                            {_ConnectStr.SkyMarsDB}.[dbo].[DeviceCurrentStatus] AS a
                        LEFT JOIN 
                            {_ConnectStr.APSDB}.[dbo].[Device] AS d ON a.DeviceID = d.remark
                        WHERE 
                            d.SkyMars_connect = 1 and d.Location_zone is not null

                        UNION ALL

                        -- 從 MES 來
                        SELECT b.remark as DeviceID ,
	                    CASE 
	                     WHEN a.WorkOrderID is null THEN 'IDLE'
	                     WHEN a.WorkOrderID is not null THEN 'RUN'
	                     ELSE 'Unknown Status'
	                     END AS DeviceStatus, b.Location_Zone
	                      FROM {_ConnectStr.APSDB}.[dbo].[WipRegisterLog] as a
	                      INNER JOIN {_ConnectStr.APSDB}.[dbo].[Device] as b
	                      on a.DeviceID = b.ID
	                    where b.SkyMars_connect=0 and b.external_com=0 AND b.Location_zone is not null
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

                                devices.Add(new MachineStatus_Zone(
                                    machineName: SqlData["DeviceID"].ToString().Trim(),
                                    status: SqlData["DeviceStatus"].ToString().Trim(),
                                    locationZone: Convert.ToInt16(SqlData["Location_Zone"])

                                    ));
                            };
                        }
                    }
                }
            }
            var Zone_List = new Dictionary<string, int> {
                                                            { "1F_第一區", 1 },
                                                            { "1F_第二區", 2 },
                                                            { "1F_第三區", 3 },
                                                            { "3F_第四區", 4 }
                                                        };
            foreach (var zone in Zone_List.Keys)
            {
                run = devices.Exists(x => x.Status == "RUN" && x.LocationZone == Zone_List[zone]) ? devices.Where(x => x.Status == "RUN" && x.LocationZone == Zone_List[zone]).Count() : 0;
                idle = devices.Exists(x => x.Status == "IDLE" && x.LocationZone == Zone_List[zone]) ? devices.Where(x => x.Status == "IDLE" && x.LocationZone == Zone_List[zone]).Count() : 0;
                alarm = devices.Exists(x => x.Status == "ALARM" && x.LocationZone == Zone_List[zone]) ? devices.Where(x => x.Status == "ALARM" && x.LocationZone == Zone_List[zone]).Count() : 0;
                off = devices.Exists(x => x.Status == "OFF" && x.LocationZone == Zone_List[zone]) ? devices.Where(x => x.Status == "OFF" && x.LocationZone == Zone_List[zone]).Count() : 0;
                #endregion
                result.Add(new ProductionLineStatistics
                    (
                        productionLineName: zone,
                        statistics:
                        new StatusStatistics
                        (
                            run: (int)run,
                            idle: (int)idle,
                            alarm: (int)alarm,
                            off: (int)off
                        )
                    ));
            }
            

            return new ActionResponse<FactoryStatisticsImformation>
            {
                Data = new FactoryStatisticsImformation(factory, result)
            };
        }

        /// <summary>
        /// 取的特定產線中的機台狀態統計資料
        /// </summary>       
        /// <returns></returns>
        [HttpPost("status")]
        public ActionResponse<ProductionLineMachineImformation> GetMachineStatus([FromBody] RequestFactory info)
        {
            var result = new List<MachineStatus>();
            var Zone_List = new Dictionary<string, int> {
                                                            { "1F_第一區", 1 },
                                                            { "1F_第二區", 2 },
                                                            { "1F_第三區", 3 },
                                                            { "3F_第四區", 4 }
                                                       };
            var AddString = "";
            if(!string.IsNullOrWhiteSpace(info.ProductionName))
            {
                AddString = $"and Location_zone={Zone_List[info.ProductionName]}";
            }
            #region 撈取機台編號資料
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
                            d.SkyMars_connect = 1 and d.Location_zone is not null {AddString}

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
	                    where b.SkyMars_connect=0 and b.external_com=0 and Location_zone is not null {AddString}";
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

                                result.Add(new MachineStatus(
                                    machineName: SqlData["DeviceID"].ToString().Trim(),
                                    status: SqlData["DeviceStatus"].ToString().Trim()
                                    ));

                            };
                        }
                    }
                }
            }
            #endregion

           
            return new ActionResponse<ProductionLineMachineImformation>
            {
                Data = new ProductionLineMachineImformation(result)
            };
        }

        /// <summary>
        /// 取得TOP 10 異常訊息累計資料
        /// </summary>
        /// <param name="request">廠區名稱 EX: FA-05、all(整廠) 產線名稱 EX: 空白、PR-01</param>
        /// <returns></returns>
        [HttpPost("alarm")]
        public ActionResponse<List<AlarmStatistics>> GetAlarm([FromBody] RequestFactory request)
        {
            return new ActionResponse<List<AlarmStatistics>>
            {
                Data = _alarmService.GetAlarm(request)
            };
        }

        /// <summary>
        /// 取得停機次數統計
        /// </summary>
        /// <param name="request">廠區名稱 EX: FA-05、all(整廠) 產線名稱 EX: 空白、PR-01</param>
        /// <returns></returns>
        [HttpPost("stop")]
        public ActionResponse<List<StopStatistics>> GetStop([FromBody] RequestFactory request)
        {
            var result = new List<StopStatistics>();
            var devices = new List<Device>();

            #region 撈取機台編號資料
            //取得工單資料
            //var sqlStr = @$"SELECT distinct remark
            //                FROM {_ConnectStr.APSDB}.[dbo].[Device]
            //                WHERE external_com=0 and SkyMars_connect=1";
            var sqlStr = @$"SELECT MachineName,COUNT(*) as stoptime
                              FROM {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog]
                              where MachineStatus=0 
                              group by MachineName";
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

                                //devices.Add(new Device(SqlData["remark"].ToString().Trim(), SqlData["remark"].ToString().Trim()));
                                result.Add(new StopStatistics(
                                machineName: SqlData["MachineName"].ToString().Trim(),
                                times: Convert.ToInt32(SqlData["stoptime"])
                                ));

                            };
                        }
                    }
                }
            }
            #endregion



            //var random = new Random(Guid.NewGuid().GetHashCode());
            //for(int i=0;i<devices.Count();i+=10)
            //{
            //    result.Add(new StopStatistics(
            //        machineName: devices[i].Text,
            //        times: random.Next(5, 150)
            //        ));
            //}
            return new ActionResponse<List<StopStatistics>>
            {
                Data = result
            };
        }

    }
}
