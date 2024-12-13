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
    public class DeviceInfoController : BaseApiController
    {
        ConnectStr _ConnectStr = new ConnectStr();
        public DeviceInfoController()
        {

        }

        [HttpPost]
        public ActionResponse<OperationInfo> Post([FromBody] RequestFactory device)
        {
            var tempinfo = new DeviceInfoTemp();
            var SkyMars_connect = String.Empty;
            var DeviceStatus = String.Empty;
            var Utilization_rate = 0.0;

            var sqlStr = @$"-- 首先獲取特定機台的 SkyMars_connect 值
                                WITH DeviceInfo AS (
                                    SELECT 
                                        SkyMars_connect,
                                        remark AS DeviceName
                                    FROM 
                                        {_ConnectStr.APSDB}.[dbo].[Device]
                                    WHERE 
                                        remark = '{device.DeviceName}'
                                )

                                -- 根據 SkyMars_connect 決定資料來源並獲取機台狀態
                                SELECT 
                                    di.DeviceName,
                                    di.SkyMars_connect,
                                    CASE 
                                        WHEN di.SkyMars_connect = 1 THEN dcs.DeviceStatus
                                        WHEN di.SkyMars_connect = 0 THEN wrl.DeviceStatus
                                        ELSE 'Unknown Status'
                                    END AS DeviceStatus
                                FROM 
                                    DeviceInfo di
                                LEFT JOIN 
                                    -- 如果 SkyMars_connect = 1，從 DeviceCurrentStatus 中撈取
                                    (SELECT 
                                        a.DeviceID,
                                        a.DeviceStatus
                                     FROM 
                                        {_ConnectStr.SkyMarsDB}.[dbo].[DeviceCurrentStatus] AS a
                                     WHERE 
                                        a.DeviceID = '{device.DeviceName}'
                                    ) AS dcs ON di.DeviceName = dcs.DeviceID
                                LEFT JOIN 
                                    -- 如果 SkyMars_connect = 0，從 WipRegisterLog 中撈取
                                    (SELECT 
                                        b.remark AS DeviceID,
                                        CASE 
                                            WHEN a.WorkOrderID IS NULL THEN 'IDLE'
                                            WHEN a.WorkOrderID IS NOT NULL THEN 'RUN'
                                            ELSE 'Unknown Status'
                                        END AS DeviceStatus
                                     FROM 
                                        {_ConnectStr.APSDB}.[dbo].[WipRegisterLog] AS a
                                     INNER JOIN 
                                        {_ConnectStr.APSDB}.[dbo].[Device] AS b ON a.DeviceID = b.ID
                                     WHERE 
                                        b.remark = '{device.DeviceName}'
                                        AND b.SkyMars_connect = 0 
                                        AND b.external_com = 0
                                    ) AS wrl ON di.DeviceName = wrl.DeviceID";
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
                                SkyMars_connect = SqlData["SkyMars_connect"].ToString();
                                DeviceStatus = SqlData["DeviceStatus"].ToString();
                            };
                        }
                    }
                }
            }

            //撈取機台稼動率資訊
            switch(SkyMars_connect)
            {
                //未聯網=>從MES撈
                case "False":
                    sqlStr = @$"DECLARE @TodayStart0800 DATETIME = DATEADD(HOUR, 8, CAST(CONVERT(DATE, GETDATE()) AS DATETIME));  -- 今天早上8:00
                                DECLARE @CurrentTime DATETIME = GETDATE();  -- 當前時間
                                DECLARE @TotalTimeInSeconds INT = DATEDIFF(SECOND, @TodayStart0800, @CurrentTime);
                                ---- 顯示 TodayStart0800 的值
                                --SELECT @TodayStart0800 AS TodayStart0800;
                                --SELECT @TotalTimeInSeconds AS TotalTimeInSeconds;

                                WITH OrderedData AS (
                                    SELECT 
                                        [OrderID],
                                        [OPID],
                                        [WIPEvent],
                                        [DeviceID],
                                        CONVERT(date, CreateTime) AS Date,
                                        CreateTime,
                                        LAG(CreateTime) OVER (PARTITION BY [DeviceID] ORDER BY CreateTime) AS PreviousTime,
                                        ROW_NUMBER() OVER (PARTITION BY [DeviceID] ORDER BY CreateTime DESC) AS RowNum
                                    FROM 
                                        {_ConnectStr.APSDB}.[dbo].[WIPLog]
                                    WHERE 
                                        DeviceID = '{device.DeviceName}'
		                                AND CreateTime >= @TodayStart0800  -- 只納入8:00之後的記錄
		                                AND CreateTime < @CurrentTime  -- 確保記錄在當前時間之前
                                ),
                                TotalData AS(
                                SELECT 
                                    [OrderID],
                                    [OPID],
                                    [WIPEvent],
                                    [DeviceID],
                                    Date,
                                    CreateTime,
                                    PreviousTime,
                                    RowNum,
                                    CASE 
                                        WHEN RowNum = 1 AND WIPEvent = 1 THEN DATEDIFF(SECOND, CreateTime, GETDATE())
                                        WHEN RowNum != 1 AND WIPEvent = 1 THEN NULL
                                        WHEN WIPEvent IN (2, 3) AND PreviousTime IS NULL THEN NULL  -- 新增判斷條件
                                        ELSE DATEDIFF(SECOND, PreviousTime, CreateTime)
                                    END AS ProcessingTime
                                FROM 
                                    OrderedData)
                                select DeviceID AS MachineName,
		                                SUM(ProcessingTime) as TotalRunTimeInSeconds,
		                                CAST(SUM(ProcessingTime) AS FLOAT) / @TotalTimeInSeconds AS RunTimeRatio  -- 計算運行時間比例
                                from TotalData
                                GROUP BY 
                                    DeviceID
                                ORDER BY 
                                    DeviceID;";
                    break;
                //聯網=>從skymars撈
                case "True":
                    sqlStr = @$"--聯網機台單機稼動率
                                DECLARE @TodayStart0800 DATETIME = DATEADD(HOUR, 8, CAST(CONVERT(DATE, GETDATE()) AS DATETIME));  -- 今天早上8:00
                                DECLARE @CurrentTime DATETIME = GETDATE();  -- 當前時間

                                -- 計算今天早上8:00到目前的總時間（以秒為單位）
                                DECLARE @TotalTimeInSeconds INT = DATEDIFF(SECOND, @TodayStart0800, @CurrentTime);

                                -- 計算每個機台的實際運行時間和比例
                                SELECT 
                                    MachineName, 
                                    SUM(Duration) AS TotalRunTimeInSeconds,
                                    CAST(SUM(Duration) AS FLOAT) / @TotalTimeInSeconds AS RunTimeRatio  -- 計算運行時間比例
                                FROM 
                                    {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog]
                                WHERE 
                                    MachineStatus = 1
                                    AND MachineName = '{device.DeviceName}'
                                    AND StartDateTime >= @TodayStart0800  -- 只納入8:00之後的記錄
                                    AND StartDateTime < @CurrentTime  -- 確保記錄在當前時間之前
                                GROUP BY 
                                    MachineName
                                ORDER BY 
                                    MachineName;";
                    break;
            }
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
                                Utilization_rate = Convert.ToDouble(SqlData["RunTimeRatio"]);
                            };
                        }
                    }
                }
            }

            #region 撈取各機台生產資料
            //取得工單資料
            sqlStr = @$"SELECT
                            a.WIPEvent,
                            a.OrderID,
                            a.OPID,
                            c.OPLTXA1,
                            c.MAKTX,
                            g.Name,
                            d.CustomerInfo,
                            c.AssignDate,
                            a.OrderQTY,
                            a.QtyGood,
                            (CAST(a.QtyGood AS FLOAT) / CAST(a.OrderQTY AS FLOAT) * 100.0) AS ProductionProgress,
                            f.img
                        FROM
                            {_ConnectStr.APSDB}.[dbo].[WIP] AS a
                        LEFT JOIN
                            {_ConnectStr.APSDB}.[dbo].[Assignment] AS c ON a.OrderID = c.OrderID AND a.OPID = c.OPID AND a.WIPEvent in (1,2)
                        LEFT JOIN
                           {_ConnectStr.APSDB}.[dbo].OrderOverview AS d ON c.ERPOrderID = d.OrderID
                        INNER JOIN 
                            {_ConnectStr.MRPDB}.[dbo].[Part] as g ON c.maktx = g.number
                        RIGHT JOIN
                            {_ConnectStr.APSDB}.[dbo].[Device] AS f ON c.WorkGroup = f.remark
						LEFT JOIN
							{_ConnectStr.APSDB}.[dbo].[WipRegisterLog] AS e ON f.ID = e.DeviceID
                        WHERE
                            f.remark = '{device.DeviceName}'";


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
                                tempinfo.WIPEvent = String.IsNullOrEmpty(SqlData["WIPEvent"].ToString().Trim()) ? "-" : SqlData["WIPEvent"].ToString().Trim();
                                tempinfo.OrderNo = String.IsNullOrEmpty(SqlData["OrderID"].ToString().Trim()) ? "-" : SqlData["OrderID"].ToString().Trim();
                                tempinfo.OPNo = Convert.ToInt32(String.IsNullOrEmpty(SqlData["OPID"].ToString().Trim())?"00": SqlData["OPID"].ToString().Trim());
                                tempinfo.OPName = String.IsNullOrEmpty(SqlData["OPLTXA1"].ToString().Trim())? "-" : SqlData["OPLTXA1"].ToString().Trim();
                                tempinfo.ProductNo = String.IsNullOrEmpty(SqlData["Name"].ToString().Trim())? "-" : SqlData["Name"].ToString().Trim();
                                tempinfo.DueDate = !Convert.IsDBNull(SqlData["AssignDate"]) ? Convert.ToDateTime(SqlData["AssignDate"]).ToString("yyyy-MM-dd") : "-";
                                tempinfo.RequireCount = Convert.ToInt32(!Convert.IsDBNull(SqlData["OrderQTY"]) ? SqlData["OrderQTY"].ToString().Trim() : "0");
                                tempinfo.CurrentCount = Convert.ToInt32(!Convert.IsDBNull(SqlData["QtyGood"]) ? SqlData["QtyGood"].ToString().Trim() : "0");
                                tempinfo.CustomName = String.IsNullOrEmpty(SqlData["CustomerInfo"].ToString().Trim())? "-" : SqlData["CustomerInfo"].ToString().Trim();
                                tempinfo.ProductionProgress = Convert.ToDouble(!Convert.IsDBNull(SqlData["ProductionProgress"]) ? SqlData["ProductionProgress"].ToString() : "0.0");
                                tempinfo.DeviceImg = String.IsNullOrEmpty(SqlData["img"].ToString().Trim()) ? "default.jpg" : SqlData["img"].ToString().Trim();
                            };
                        }
                    }
                }
            }
            if (tempinfo.DeviceImg[0]=='?')
            {
                tempinfo.DeviceImg = "default.jpg";
            }

            #endregion

            return new ActionResponse<OperationInfo>
            {
                Data = new OperationInfo
                (
                    utilizationRate: Math.Round(Utilization_rate * 100, 1),
                    //utilizationRate: Math.Round((rand.NextDouble()*0.3+0.7)*100,1),
                    status: DeviceStatus,
                    productionProgress: tempinfo.ProductionProgress,
                    customName: tempinfo.CustomName=="-" ? "-" : tempinfo.CustomName.Split('/')[1],
                    deviceImg: "/images/device/" + tempinfo.DeviceImg,
                    orderInfo: new OrderInformation(orderNo: tempinfo.OrderNo, oPNo: tempinfo.OPNo, opName: tempinfo.OPName,
                    productNo: tempinfo.ProductNo, requireCount: tempinfo.RequireCount, currentCount: tempinfo.CurrentCount, dueDate: tempinfo.DueDate, customerinfo: ""))

            };



            //if (!string.IsNullOrEmpty(tempinfo.DeviceImg) && tempinfo.ProductNo != "-" && tempinfo.WIPEvent=="1")
            //{
            //    return new ActionResponse<OperationInfo>
            //    {
            //        Data = new OperationInfo
            //    (
            //        utilizationRate: Math.Round(Utilization_rate * 100, 1),
            //        //utilizationRate: Math.Round((rand.NextDouble()*0.3+0.7)*100,1),
            //        status: DeviceStatus,
            //        productionProgress: tempinfo.ProductionProgress,
            //        customName: tempinfo.CustomName.Split('/')[1],
            //        deviceImg: "/images/device/"+ tempinfo.DeviceImg,
            //        orderInfo: new OrderInformation(orderNo: tempinfo.OrderNo, oPNo: tempinfo.OPNo, opName: tempinfo.OPName,
            //        productNo: tempinfo.ProductNo, requireCount: tempinfo.RequireCount, currentCount: tempinfo.CurrentCount, dueDate: tempinfo.DueDate, customerinfo:""))
                    
            //    };
            //}
            //else if(tempinfo.WIPEvent == "2")
            //{
            //    return new ActionResponse<OperationInfo>
            //    {
            //        Data = new OperationInfo
            //    (
            //        //utilizationRate: Math.Round((rand.NextDouble() * 0.3 + 0.7) * 100, 1),
            //        utilizationRate: Math.Round(Utilization_rate * 100, 1),
            //        status: DeviceStatus,
            //        productionProgress: tempinfo.ProductionProgress,
            //        customName: tempinfo.CustomName.Split('/')[1],
            //        deviceImg: "/images/device/" + tempinfo.DeviceImg,
            //        orderInfo: new OrderInformation(orderNo: tempinfo.OrderNo, oPNo: tempinfo.OPNo, opName: tempinfo.OPName,
            //        productNo: tempinfo.ProductNo, requireCount: tempinfo.RequireCount, currentCount: tempinfo.CurrentCount, dueDate: tempinfo.DueDate, customerinfo: ""))

            //    };
            //}
            //else
            //{
            //    return new ActionResponse<OperationInfo>
            //    {
            //        Data = new OperationInfo
            //    (
            //        //utilizationRate: Math.Round((rand.NextDouble() * 0.4 + 0.6) * 100, 1),
            //        utilizationRate: Math.Round(Utilization_rate * 100, 1),
            //        status: "IDLE",
            //        productionProgress: tempinfo.ProductionProgress,
            //        customName: "-",
            //        deviceImg: "/images/device/" + tempinfo.DeviceImg,
            //        orderInfo: new OrderInformation(orderNo: "-", oPNo: 0, opName: "-",
            //        productNo: "-", requireCount: 0, currentCount: 0, dueDate: "-", customerinfo: "-"))

            //    };
            //}
        }

    }
}
