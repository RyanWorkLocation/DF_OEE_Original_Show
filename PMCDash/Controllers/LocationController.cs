using Microsoft.AspNetCore.Http;
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
    [ApiController]
    public class LocationController : BaseApiController
    {
        ConnectStr _ConnectStr = new ConnectStr();
        public LocationController()
        {

        }

        /// <summary>
        /// 取回圖檔與座標描述
        /// </summary>
        /// <param name="request">輸入需要哪個廠區或產線瀏覽圖(產線可空白廠區不行)</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResponse<ImageDefine> Post(RequestProductionLine request)
        {
            //判斷是要廠區圖還是產線圖
            var choice = string.IsNullOrEmpty(request.FactoryName) ? "404" :  
                string.IsNullOrEmpty(request.ProductionName) ? "factory" :"productionline";
            switch (choice)
            {
                case "factory":
                    return new ActionResponse<ImageDefine>
                    {
                        Data = new ImageDefine(
                            imageUrl: $"http://{Request.Host.Value}/Images/Factory.png",
                            imageInfos: new List<ImageInfo>
                            {
                    new ImageInfo("全產線", 65.0d, 50.0d, "")
                            })
                    };

                case "productionline":
                    string productionImage;
                    int machineIndex;

                    switch (request.ProductionName)
                    {
                        case "1F_第一區":
                            productionImage = "/Images/1F_ZONE1.png";
                            machineIndex = 1;
                            break;

                        case "1F_第二區":
                            productionImage = "/Images/1F_ZONE2.png";
                            machineIndex = 2;
                            break;

                        case "1F_第三區":
                            productionImage = "/Images/1F_ZONE3.png";
                            machineIndex = 3;
                            break;

                        case "3F_第四區":
                            productionImage = "/Images/3F_ZONE4.png";
                            machineIndex = 4;
                            break;

                        default:
                            return new ActionResponse<ImageDefine>
                            {
                                Data = new ImageDefine(imageUrl: "Not Find", imageInfos: new List<ImageInfo>())
                            };
                    }

                    return new ActionResponse<ImageDefine>
                    {
                        Data = new ImageDefine(
                            imageUrl: $"http://{Request.Host.Value}{productionImage}",
                            imageInfos: GetMachinePosandSta(machineIndex))
                    };

                default:
                    return new ActionResponse<ImageDefine>
                    {
                        Data = new ImageDefine(imageUrl: "Not Find", imageInfos: new List<ImageInfo>())
                    };
            }


        }

        private List<ImageInfo> GetMachinePosandSta(int zone)
        {
            var result = new List<ImageInfo>();
            var sqlStr = @$"WITH CombinedDeviceStatus AS (
                            -- 從 skymars 來的設備狀態
                            SELECT 
                                a.DeviceID,
                                a.DeviceStatus
                            FROM 
                                {_ConnectStr.SkyMarsDB}.[dbo].[DeviceCurrentStatus] AS a
                            LEFT JOIN 
                                {_ConnectStr.APSDB}.[dbo].[Device] AS d ON a.DeviceID = d.remark
                            WHERE 
                                d.SkyMars_connect = 1 and d.Location_zone={zone}
    
                            UNION ALL
    
                            -- 從 MES 來的設備狀態
                            SELECT 
                                b.remark as DeviceID,
                                CASE 
                                    WHEN a.WorkOrderID is null THEN 'IDLE'
                                    WHEN a.WorkOrderID is not null THEN 'RUN'
                                    ELSE 'Unknown Status'
                                END AS DeviceStatus
                            FROM {_ConnectStr.APSDB}.[dbo].[WipRegisterLog] as a
                            INNER JOIN {_ConnectStr.APSDB}.[dbo].[Device] as b
                                ON a.DeviceID = b.ID
                            WHERE b.SkyMars_connect = 0 AND b.external_com = 0 AND b.Location_zone={zone}
                        )

                        SELECT 
                            p.MachineName,
                            c.DeviceStatus,
                            ROUND(p.ratio_x, 2) as ratio_x,
                            ROUND(p.ratio_y, 2) as ratio_y
                        FROM 
                            CombinedDeviceStatus c
                        INNER JOIN 
                            {_ConnectStr.APSDB}.[dbo].[DevicePosRatio] p ON c.DeviceID = p.MachineName";
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
                                result.Add(new ImageInfo(SqlData["MachineName"].ToString(), Convert.ToDouble(SqlData["ratio_x"]), Convert.ToDouble(SqlData["ratio_y"]), SqlData["DeviceStatus"].ToString()));
                            };
                        }
                    }
                }
            }
            return result;
        }

    }
}
