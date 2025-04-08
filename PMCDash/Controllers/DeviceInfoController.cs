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
                                        AND b.Location_zone is not null
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
                    //sqlStr = @$"DECLARE @TodayStart0800 DATETIME = DATEADD(HOUR, 8, CAST(CONVERT(DATE, GETDATE()) AS DATETIME));  -- 今天早上8:00
                    //            DECLARE @CurrentTime DATETIME = GETDATE();  -- 當前時間
                    //            DECLARE @TotalTimeInSeconds INT = DATEDIFF(SECOND, @TodayStart0800, @CurrentTime);
                    //            -- 今日早上8點到現在的總時間 
                    //            --SELECT @TodayStart0800 AS TodayStart0800;
                    //            --SELECT @TotalTimeInSeconds AS TotalTimeInSeconds;

                    //            --撈出所有在時間範圍內的生產數據
                    //            WITH OrderedData AS (
                    //                SELECT 
                    //                    [OrderID],
                    //                    [OPID],
                    //                    [WIPEvent],
                    //                    [DeviceID],
                    //                    CONVERT(date, CreateTime) AS Date,
                    //                    CreateTime,
                    //                    LAG(CreateTime) OVER (PARTITION BY [DeviceID] ORDER BY CreateTime) AS PreviousTime,
                    //                    ROW_NUMBER() OVER (PARTITION BY [DeviceID] ORDER BY CreateTime DESC) AS RowNum
                    //                FROM 
                    //                    {_ConnectStr.APSDB}.[dbo].[WIPLog]
                    //                WHERE 
                    //                    DeviceID = '{device.DeviceName}'
                    //              AND CreateTime >= @TodayStart0800  -- 只納入8:00之後的記錄
                    //              AND CreateTime < @CurrentTime  -- 確保記錄在當前時間之前
                    //            ),
                    //            TotalData AS(
                    //            SELECT 
                    //                [OrderID],
                    //                [OPID],
                    //                [WIPEvent],
                    //                [DeviceID],
                    //                Date,
                    //                CreateTime,
                    //                PreviousTime,
                    //                RowNum,
                    //                CASE 
                    //                    -- 如果是最新一筆數據且狀態為加工中，則計算開始加工至現在的總時間
                    //                    WHEN RowNum = 1 AND WIPEvent = 1 THEN DATEDIFF(SECOND, CreateTime, GETDATE())
                    //                    WHEN RowNum != 1 AND WIPEvent = 1 THEN NULL
                    //                    WHEN WIPEvent IN (2, 3) AND PreviousTime IS NULL THEN NULL  -- 新增判斷條件
                    //                    ELSE DATEDIFF(SECOND, PreviousTime, CreateTime)
                    //                END AS ProcessingTime
                    //            FROM 
                    //                OrderedData)
                    //            select DeviceID AS MachineName,
                    //              SUM(ProcessingTime) as TotalRunTimeInSeconds,
                    //              CAST(SUM(ProcessingTime) AS FLOAT) / @TotalTimeInSeconds AS RunTimeRatio  -- 計算運行時間比例
                    //            from TotalData
                    //            GROUP BY 
                    //                DeviceID
                    //            ORDER BY 
                    //                DeviceID;";
                    sqlStr = @$"DECLARE @TodayStart0800 DATETIME = DATEADD(HOUR, 8, CAST(CONVERT(DATE, GETDATE()) AS DATETIME));  -- 今天早上8:00
                                DECLARE @YesterdayStart0800 DATETIME = DATEADD(DAY, -1, @TodayStart0800);  -- 昨天早上8:00
                                DECLARE @CurrentTime DATETIME = GETDATE();  -- 當前時間
                                DECLARE @TotalTimeInSeconds INT = DATEDIFF(SECOND, @TodayStart0800, @CurrentTime);

                                -- 第一步：找出該機台在昨天8點至今天期間的所有製程記錄，並按時間排序
                                WITH AllRecords AS (
                                    SELECT 
                                        [OrderID],
                                        [OPID],
                                        [WIPEvent],
                                        [DeviceID],
                                        CreateTime
                                    FROM 
                                        {_ConnectStr.APSDB}.[dbo].[WIPLog]
                                    WHERE 
                                        DeviceID = '{device.DeviceName}'
                                        AND CreateTime >= @YesterdayStart0800  -- 只檢查昨天8點以後的記錄
                                        AND CreateTime < @CurrentTime
                                ),
                                -- 第二步：使用窗口函數找出每個開始記錄對應的結束記錄
                                ProcessPairs AS (
                                    SELECT 
                                        [DeviceID],
                                        [OrderID],
                                        [OPID],
                                        CreateTime AS StartTime,
                                        LEAD(CreateTime) OVER (PARTITION BY DeviceID ORDER BY CreateTime) AS EndTime,
                                        LEAD(WIPEvent) OVER (PARTITION BY DeviceID ORDER BY CreateTime) AS NextEvent,
                                        WIPEvent
                                    FROM 
                                        AllRecords
                                ),
                                -- 第三步：計算每個有效製程的運行時間
                                ValidProcesses AS (
                                    SELECT 
                                        [DeviceID],
                                        [OrderID],
                                        [OPID],
                                        StartTime,
                                        CASE 
                                            -- 如果下一個事件是結束或中斷，則使用記錄的結束時間
                                            WHEN NextEvent IN (2, 3) THEN EndTime
                                            -- 如果是最後一個開始事件且未結束，則使用當前時間
                                            WHEN NextEvent IS NULL AND WIPEvent = 1 THEN @CurrentTime
                                            ELSE NULL
                                        END AS EffectiveEndTime,
                                        WIPEvent
                                    FROM 
                                        ProcessPairs
                                    WHERE 
                                        -- 只計算開始事件
                                        WIPEvent = 1
                                        -- 確保此開始事件有對應的結束，或是最後一個開始事件
                                        AND (NextEvent IN (2, 3) OR NextEvent IS NULL)
                                ),
                                -- 第四步：計算今天8點以後的有效運行時間
                                TodayRuntime AS (
                                    SELECT 
                                        [DeviceID],
                                        [OrderID],
                                        [OPID],
                                        CASE 
                                            -- 如果開始時間早於今天8點，則計算從今天8點開始
                                            WHEN StartTime < @TodayStart0800 THEN @TodayStart0800
                                            ELSE StartTime
                                        END AS AdjustedStartTime,
                                        EffectiveEndTime,
                                        DATEDIFF(SECOND, 
                                            CASE 
                                                -- 如果開始時間早於今天8點，則計算從今天8點開始
                                                WHEN StartTime < @TodayStart0800 THEN @TodayStart0800
                                                ELSE StartTime
                                            END, 
                                            EffectiveEndTime
                                        ) AS RunTimeInSeconds
                                    FROM 
                                        ValidProcesses
                                    WHERE 
                                        -- 只計算運行時間覆蓋今天8點以後的製程
                                        (StartTime >= @TodayStart0800 OR EffectiveEndTime > @TodayStart0800)
                                )
                                -- 最終結果：計算總運行時間和稼動率
                                SELECT 
                                    DeviceID AS MachineName,
                                    SUM(RunTimeInSeconds) AS TotalRunTimeInSeconds,
                                    CAST(SUM(RunTimeInSeconds) AS FLOAT) / @TotalTimeInSeconds AS RunTimeRatio
                                FROM 
                                    TodayRuntime
                                GROUP BY 
                                    DeviceID
                                ORDER BY 
                                    DeviceID;";
                    break;
                //聯網=>從skymars撈
                case "True":
                    //           sqlStr = @$"--聯網機台單機稼動率
                    //                       DECLARE @TodayStart0800 DATETIME = DATEADD(HOUR, 8, CAST(CONVERT(DATE, GETDATE()) AS DATETIME));  -- 今天早上8:00
                    //                       DECLARE @CurrentTime DATETIME = GETDATE();  -- 當前時間

                    //                       -- 計算今天早上8:00到目前的總時間（以秒為單位）
                    //                       DECLARE @TotalTimeInSeconds INT = DATEDIFF(SECOND, @TodayStart0800, @CurrentTime);

                    //                       -- 計算每個機台的實際運行時間和比例
                    //                       SELECT 
                    //                           MachineName, 
                    //sum(datediff(SECOND,StartDateTime,enddatetime)),
                    //                            (sum(datediff(SECOND,StartDateTime,enddatetime))*1.0/@TotalTimeInSeconds) AS RunTimeRatio  -- 計算運行時間比例
                    //                       FROM 
                    //                           {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog]
                    //                       WHERE 
                    //                           MachineStatus = 1
                    //                           AND MachineName = '{device.DeviceName}'
                    //                           AND StartDateTime >= @TodayStart0800  -- 只納入8:00之後的記錄
                    //                           AND StartDateTime < @CurrentTime  -- 確保記錄在當前時間之前
                    //                       GROUP BY 
                    //                           MachineName
                    //                       ORDER BY 
                    //                           MachineName;";
                    sqlStr = @$"--聯網機台單機稼動率
                                DECLARE @TodayStart0800 DATETIME = DATEADD(HOUR, 8, CAST(CONVERT(DATE, GETDATE()) AS DATETIME));  -- 今天早上8:00
                                DECLARE @YesterdayStart0800 DATETIME = DATEADD(DAY, -1, @TodayStart0800);  -- 昨天早上8:00
                                DECLARE @CurrentTime DATETIME = GETDATE();  -- 當前時間

                                -- 計算今天早上8:00到目前的總時間（以秒為單位）
                                DECLARE @TotalTimeInSeconds INT = DATEDIFF(SECOND, @TodayStart0800, @CurrentTime);

                                -- 第一步：先篩選出相關時間段的記錄，減少後續處理的數據量
                                WITH FilteredRecords AS (
                                    SELECT 
                                        MachineName,
                                        StartDateTime,
                                        EndDateTime
                                    FROM 
                                        {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog]
                                    WHERE 
                                        MachineStatus = 1
                                        AND MachineName = '{device.DeviceName}'
                                        -- 只查詢昨天8點以後的記錄，而且必須與今天8點以後有時間重疊
                                        AND StartDateTime >= @YesterdayStart0800
                                        AND (EndDateTime IS NULL OR EndDateTime > @TodayStart0800)
                                ),
                                -- 第二步：計算每條記錄在今天8點以後的實際運行時間
                                TodayRuntime AS (
                                    SELECT 
                                        MachineName,
                                        -- 調整開始時間：如果早於今天8點，則使用今天8點
                                        CASE 
                                            WHEN StartDateTime < @TodayStart0800 THEN @TodayStart0800
                                            ELSE StartDateTime
                                        END AS AdjustedStartTime,
                                        -- 調整結束時間：如果尚未結束，則使用當前時間
                                        CASE 
                                            WHEN EndDateTime IS NULL THEN @CurrentTime
                                            ELSE EndDateTime
                                        END AS AdjustedEndTime,
                                        -- 計算調整後的時間差
                                        DATEDIFF(
                                            SECOND,
                                            CASE 
                                                WHEN StartDateTime < @TodayStart0800 THEN @TodayStart0800
                                                ELSE StartDateTime
                                            END,
                                            CASE 
                                                WHEN EndDateTime IS NULL THEN @CurrentTime
                                                ELSE EndDateTime
                                            END
                                        ) AS RunTimeInSeconds
                                    FROM 
                                        FilteredRecords
                                    WHERE
                                        -- 確保只計算有效的時間區間
                                        (StartDateTime < @CurrentTime) AND
                                        (EndDateTime IS NULL OR EndDateTime > @TodayStart0800)
                                )
                                -- 最終結果：計算總運行時間和稼動率
                                SELECT 
                                    MachineName,
                                    SUM(RunTimeInSeconds) AS TotalRunTimeInSeconds,
                                    (SUM(RunTimeInSeconds) * 1.0 / @TotalTimeInSeconds) AS RunTimeRatio
                                FROM 
                                    TodayRuntime
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
