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
	                    where b.SkyMars_connect=0 and b.external_com=0 and b.Location_zone is not null
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
            // 先四捨五入
            decimal runPercent = (decimal)Math.Round(run / (double)devices.Count() * 100, 1);
            decimal idlePercent = (decimal)Math.Round(idle / (double)devices.Count() * 100, 1);
            decimal alarmPercent = (decimal)Math.Round(alarm / (double)devices.Count() * 100, 1);
            decimal offPercent = (decimal)Math.Round(off / (double)devices.Count() * 100, 1);

            // 計算總和與差值
            decimal total = runPercent + idlePercent + alarmPercent + offPercent;
            decimal diff = 100 - total;

            // 找出最大項目並調整
            decimal maxPercent = Math.Max(Math.Max(runPercent, idlePercent), Math.Max(alarmPercent, offPercent));
            if (maxPercent == runPercent) runPercent += diff;
            else if (maxPercent == idlePercent) idlePercent += diff;
            else if (maxPercent == alarmPercent) alarmPercent += diff;
            else offPercent += diff;
            #endregion
            StatusDistribution statusDistribution = new StatusDistribution(
                                                        run: runPercent,
                                                        idle: idlePercent,
                                                        alarm: alarmPercent,
                                                        off: offPercent
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
                // 先獲取當前區域的設備總數，避免重複計算
                int zoneDeviceCount = devices.Count(x => x.LocationZone == Zone_List[zone]);

                // 如果該區域沒有設備，跳過或添加零值
                if (zoneDeviceCount == 0)
                {
                    result.Add(new ProductionLineStatusDistribution(
                        productionLineName: zone,
                        distribution: new StatusDistribution(run: 0, idle: 0, alarm: 0, off: 0)
                    ));
                    continue;
                }

                // 計算各狀態數量
                run = devices.Count(x => x.Status == "RUN" && x.LocationZone == Zone_List[zone]);
                idle = devices.Count(x => x.Status == "IDLE" && x.LocationZone == Zone_List[zone]);
                alarm = devices.Count(x => x.Status == "ALARM" && x.LocationZone == Zone_List[zone]);
                off = devices.Count(x => x.Status == "OFF" && x.LocationZone == Zone_List[zone]);

                // 先四捨五入
                decimal runPercent = (decimal)Math.Round((double)run / zoneDeviceCount * 100, 1);
                decimal idlePercent = (decimal)Math.Round((double)idle / zoneDeviceCount * 100, 1);
                decimal alarmPercent = (decimal)Math.Round((double)alarm / zoneDeviceCount * 100, 1);
                decimal offPercent = (decimal)Math.Round((double)off / zoneDeviceCount * 100, 1);

                // 計算總和與差值
                decimal total = runPercent + idlePercent + alarmPercent + offPercent;
                decimal diff = 100 - total;

                // 找出最大項目並調整
                if (Math.Abs(diff) > 0)
                {
                    decimal maxPercent = Math.Max(Math.Max(runPercent, idlePercent), Math.Max(alarmPercent, offPercent));
                    if (maxPercent == runPercent && run > 0) runPercent += diff;
                    else if (maxPercent == idlePercent && idle > 0) idlePercent += diff;
                    else if (maxPercent == alarmPercent && alarm > 0) alarmPercent += diff;
                    else if (maxPercent == offPercent && off > 0) offPercent += diff;
                    else
                    {
                        // 如果最大值為0，則將差值添加到第一個非0項
                        if (run > 0) runPercent += diff;
                        else if (idle > 0) idlePercent += diff;
                        else if (alarm > 0) alarmPercent += diff;
                        else offPercent += diff;
                    }
                }

                // 創建並添加結果
                StatusDistribution statusDistribution = new StatusDistribution(
                    run: runPercent,
                    idle: idlePercent,
                    alarm: alarmPercent,
                    off: offPercent
                );

                result.Add(new ProductionLineStatusDistribution(
                    productionLineName: zone,
                    distribution: statusDistribution
                ));
            }
            #endregion






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
	                    where b.SkyMars_connect=0 and b.external_com=0 and b.Location_zone is not null
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
            // 先四捨五入
            decimal runPercent = (decimal)Math.Round(run / (double)devices.Count() * 100, 1);
            decimal idlePercent = (decimal)Math.Round(idle / (double)devices.Count() * 100, 1);
            decimal alarmPercent = (decimal)Math.Round(alarm / (double)devices.Count() * 100, 1);
            decimal offPercent = (decimal)Math.Round(off / (double)devices.Count() * 100, 1);

            // 計算總和與差值
            decimal total = runPercent + idlePercent + alarmPercent + offPercent;
            decimal diff = 100 - total;

            // 找出最大項目並調整
            decimal maxPercent = Math.Max(Math.Max(runPercent, idlePercent), Math.Max(alarmPercent, offPercent));
            if (maxPercent == runPercent) runPercent += diff;
            else if (maxPercent == idlePercent) idlePercent += diff;
            else if (maxPercent == alarmPercent) alarmPercent += diff;
            else offPercent += diff;
            #endregion

            /////////////////////////////////////////////////////////////
            // 將結果封裝到 StatusDistribution 對象中
            StatusDistribution statusDistribution = new StatusDistribution(
                                                        run: runPercent,
                                                        idle: idlePercent,
                                                        alarm: alarmPercent,
                                                        off: offPercent
                                                    );

            // 返回 ActionResponse
            return new ActionResponse<StatusDistribution>
            {
                Data = statusDistribution
            };
        }

    }
}
