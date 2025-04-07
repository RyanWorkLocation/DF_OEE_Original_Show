using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PMCDash.Models;
using System.Data;
using System.Data.SqlClient;

namespace PMCDash.Controllers
{
    [Route("api/[controller]")]
    public class InformationController : BaseApiController
    {

        ConnectStr _ConnectStr = new ConnectStr();
        public InformationController()
        {

        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //[HttpGet("Factorys")]
        //public ActionResponse<List<FactoryImformation>> GetFactory()
        //{
        //    var result = new List<FactoryImformation>();
        //    var factorynName = new string[] { "大里廠", "松竹廠", "松竹五廠", "鐮村廠", "松竹七廠" };
        //    for (int i = 0; i < 5; i++)
        //    {
        //        result.Add(new FactoryImformation($@"FA-0{i + 1}", factorynName[i]));
        //    }
        //    return new ActionResponse<List<FactoryImformation>>
        //    {
        //        Data = result
        //    };
        //}

        /// <summary>
        /// 工廠與產線清單
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResponse<FactoryDefine> Get()
        {
            //var devices = new List<Device>();
            var devices = new Dictionary<int, List<Device>>();
            var productionLines = new List<ProductionLine>();

            #region 撈取機台編號資料
            //取得工單資料
            var sqlStr = @$"SELECT remark, Location_zone
                            FROM {_ConnectStr.APSDB}.[dbo].[Device]
                            WHERE external_com=0 and Location_zone is not null";
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
                                int zone = Convert.ToInt16(SqlData["Location_zone"]);
                                string remark = SqlData["remark"].ToString().Trim();
                                if (!string.IsNullOrEmpty(remark))
                                {
                                    if (!devices.ContainsKey(zone))
                                    {
                                        devices.Add(zone, new List<Device>());
                                    }
                                    //devices.Add(new Device(remark, remark));
                                    devices[zone].Add(new Device(remark, remark));
                                }

                            };
                        }
                    }
                }
            }
            #endregion

            //加入產線分區名稱
            //productionLines.Add(new ProductionLine(@$"全產線", @$"全產線"));
            productionLines.AddRange(new List<ProductionLine>
            {
                new ProductionLine(@$"1F_第一區", @$"1F_第一區"),
                new ProductionLine(@$"1F_第二區", @$"1F_第二區"),
                new ProductionLine(@$"1F_第三區", @$"1F_第三區"),
                new ProductionLine(@$"3F_第四區", @$"3F_第四區"),
            });
            //分別加入各籠機台
            //productionLines[0].Devices = devices.ToList();
            for(int i=0;i< productionLines.Count;i++)
            {
                productionLines[i].Devices = devices[i + 1];
            }
            var factorys = new List<Factory>();
            //加入廠房名稱
            var facotorys = new Factory("安南新廠", "安南新廠");
            facotorys.ProductionLines = productionLines.ToList();
            factorys.Add(facotorys);
            var result = new FactoryDefine();
            result.Factorys = factorys;
            return new ActionResponse<FactoryDefine>
            {
                Data = result
            };
        }
        /// <summary>
        /// 取得特定產線名稱
        /// </summary>
        /// <param name="factory">廠區名稱 EX:FA-01</param>
        /// <returns></returns>
        [HttpGet("Productionlines/{factory}")]
        public ActionResponse<List<ProductionLineImformation>> GetProduction(string factory)
        {
            var result = new List<ProductionLineImformation>();
            var factorynName = new string[] { "WGAM", "WGCM", "WEA", "WTA", "WGPK" };
            for (int i = 0; i < 5; i++)
            {
                result.Add(new ProductionLineImformation($@"PRL-0{i}", factorynName[i]));
            }
            return new ActionResponse<List<ProductionLineImformation>>
            {
                Data = result
            };
        }

        /// <summary>
        /// 取得產線中所有的機台名稱
        /// </summary>
        /// <param name="prl">輸入廠區名稱與產線名稱</param>
        /// <returns></returns>
        [HttpPost("Machines")]
        public ActionResponse<List<MachineInformation>> GetMachine([FromBody] RequestProductionLine prl)
        {
            var result = new List<MachineInformation>();

            var devices = new List<MachineStatus>();

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
            #endregion

            foreach (var item in devices)
            {
                result.Add(new MachineInformation
                (
                    machineName: item.MachineName,
                    status: item.Status,
                    displayName: item.MachineName
                ));
            }
            return new ActionResponse<List<MachineInformation>>
            {
                Data = result
            };
        }
    }
}
