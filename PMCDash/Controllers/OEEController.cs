using Microsoft.AspNetCore.Mvc;
using PMCDash.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace PMCDash.Controllers
{
    [Route("api/[controller]")]
    public class OEEController : BaseApiController
    {
        ConnectStr _ConnectStr = new ConnectStr();
        public OEEController()
        {

        }

        /// <summary>
        /// 取得當天工廠OEE
        /// </summary>
        /// <param name="request">廠區名稱(EX:FA-01、all(此為整公司)) 產線名稱(可忽略(Empty)若指定特定產線再填入)</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResponse<OEEOverView> Post([FromBody] RequestProductionLine request)
        {
            var OnTimeRate = new double();
            var GoodYieldRate = new double();
            List<Availability> availabilities = new List<Availability>();
            List<Performance> performances = new List<Performance>(); 
            #region 撈取時間稼動、性能稼動資料
            //取得時間稼動資料(運轉時間扣掉中午休息需大於100分鐘)
            //計算每種機台每一天報工紀錄最早和最晚的時間戳記
            //計算實際運轉時間
            //分為聯網與未聯網
            var sqlStr = @$"
                           -- 合併兩個查詢結果
                            WITH CTE1 AS (
                                SELECT 
                                    [DatabaseDate] AS Date,
                                    a.[MachineName] AS DeviceID,
                                    --DATEDIFF(MINUTE, MIN(StartDateTime), MAX(EndDateTime)) AS ActualRUN
                                    SUM(DATEDIFF(MINUTE, StartDateTime, EndDateTime)) AS ActualRUN
                                FROM 
                                    {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog] AS a
                                INNER JOIN 
                                    {_ConnectStr.APSDB}.[dbo].[Device] AS b
                                ON 
                                    a.MachineName = b.remark
                                WHERE 
                                    MachineStatus IN (1, 2, 3) 
                                    AND b.SkyMars_connect = 1
                                GROUP BY 
                                    [DatabaseDate], 
                                    a.[MachineName]
                            ),
                            CTE2 AS (
                                SELECT 
                                    CONVERT(date, CreateTime) AS Date,
                                    DeviceID,
                                    DATEDIFF(MINUTE, MIN(CreateTime), MAX(CreateTime)) AS ActualRUN
                                FROM 
                                    {_ConnectStr.APSDB}.[dbo].[WIPLog]
                                INNER JOIN 
                                    {_ConnectStr.APSDB}.[dbo].[Device] AS b
                                ON 
                                    {_ConnectStr.APSDB}.[dbo].[WIPLog].DeviceID = b.remark
                                WHERE 
                                    b.external_com = 0 AND b.SkyMars_connect = 0
                                GROUP BY 
                                    CONVERT(date, CreateTime), 
                                    DeviceID
                            )
                            SELECT 
                                DeviceID,
                                Date,
                                ActualRUN
                            FROM 
                                CTE1
                            UNION ALL
                            SELECT 
                                DeviceID,
                                Date,
                                ActualRUN
                            FROM 
                                CTE2
                            WHERE 
                                ActualRUN > 100
                                AND EXISTS (
                                    SELECT 1
                                    FROM {_ConnectStr.APSDB}.[dbo].[Device] AS b
                                    WHERE CTE2.DeviceID = b.remark
                                      AND b.SkyMars_connect = 0
                                )
                            ORDER BY 
                                DeviceID, 
                                Date;
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
                                availabilities.Add(new Availability
                                (
                                    deviceid: (SqlData["DeviceID"].ToString().Trim()),
                                    date: Convert.ToDateTime(SqlData["Date"].ToString().Trim()),
                                    actualrun: Convert.ToDouble(SqlData["ActualRUN"])
                                ));
                            };
                        }
                    }
                }
            }

            //取得性能稼動資料
            //計算每台機台每道製程實際運作時間(扣掉換線間隔)
            sqlStr = @$"
                           -- 合併兩個查詢結果
                            WITH OrderedData AS (
                                SELECT 
                                    [OrderID],
                                    [OPID],
                                    [WIPEvent],
                                    [DeviceID],
                                    CONVERT(date, CreateTime) AS Date,
                                    CreateTime,
                                    LAG(CreateTime) OVER (PARTITION BY [DeviceID], CONVERT(date, CreateTime) ORDER BY CreateTime) AS PreviousTime
                                FROM 
                                    {_ConnectStr.APSDB}.[dbo].[WIPLog]
                            ),
                            ProcessedData AS (
                                SELECT 
                                    [OrderID],
                                    [OPID],
                                    [DeviceID],
                                    Date,
                                    DATEDIFF(MINUTE, PreviousTime, CreateTime) AS ProcessingTime
                                FROM 
                                    OrderedData
                                INNER JOIN {_ConnectStr.APSDB}.[dbo].[Device] as b
                                ON 
                                    OrderedData.DeviceID = b.remark
                                WHERE 
                                    b.external_com = 0 
                                    AND PreviousTime IS NOT NULL
                                    -- 僅有狀態為暫停或完工，這段時間才有加工
                                    AND WIPEvent in (2,3)
                                    AND DATEDIFF(MINUTE, PreviousTime, CreateTime) > 0
                                    AND b.SkyMars_connect = 0
                            ),
                            WIPLogData AS (
                                SELECT 
                                    DeviceID,
                                    Date,
                                    SUM(ProcessingTime) AS TotalTime
                                FROM 
                                    ProcessedData
                                GROUP BY 
                                    DeviceID, Date
                            ),
                            UtilizationLogData AS (
                                SELECT 
                                    [DatabaseDate] AS Date,
                                    a.[MachineName] AS DeviceID,
                                    SUM(DATEDIFF(MINUTE, StartDateTime, EndDateTime)) AS TotalTime
                                FROM 
                                    {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog] AS a
                                INNER JOIN 
                                    {_ConnectStr.APSDB}.[dbo].[Device] AS b
                                ON 
                                    a.MachineName = b.remark
                                WHERE 
                                    MachineStatus = 1 
                                    AND b.SkyMars_connect = 1
                                GROUP BY 
                                    [DatabaseDate], 
                                    a.[MachineName]
                            )
                            SELECT 
                                DeviceID,
                                Date,
                                TotalTime
                            FROM 
                                WIPLogData
                           
                            UNION ALL
                            SELECT 
                                DeviceID,
                                Date,
                                TotalTime
                            FROM 
                                UtilizationLogData
                            ORDER BY 
                                DeviceID, 
                                Date;
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
                                performances.Add(new Performance
                                (
                                    deviceid: (SqlData["DeviceID"].ToString().Trim()),
                                    date: Convert.ToDateTime(SqlData["Date"].ToString().Trim()),
                                    totaltime: Convert.ToDouble(SqlData["TotalTime"])
                                ));
                            };
                        }
                    }
                }
            }
            #endregion




            #region 撈取良率、準交率資料
            //取得工單資料
            sqlStr = @$"
                            -- 計算目前準交的工單數量(交期為今天以前)
                            -- 已完工且完工時間在預交日期前
                            DECLARE @TotalOrders INT
                            SELECT @TotalOrders = COUNT(DISTINCT OrderID)
                            FROM (
                                SELECT b1.OrderID
                                FROM {_ConnectStr.APSDB}.[dbo].[WIP] AS b1
                                JOIN {_ConnectStr.APSDB}.[dbo].[Assignment] AS a ON b1.SeriesID = a.SeriesID
                                WHERE a.AssignDate <= DATEADD(DAY, DATEDIFF(DAY, 0, GETDATE()), 0)
                                GROUP BY b1.OrderID
                                HAVING 
                                    COUNT(CASE WHEN b1.EndTime IS NOT NULL AND b1.EndTime <= a.AssignDate THEN 1 ELSE NULL END) = COUNT(*)
                                    AND COUNT(CASE WHEN b1.EndTime > a.AssignDate THEN 1 ELSE NULL END) = 0
                                    AND COUNT(CASE WHEN b1.WIPEvent = 3 THEN 1 ELSE NULL END) = COUNT(*)
                            ) AS FilteredOrders

                            -- 計算已完工良品數量
                            DECLARE @TotalGood INT
                            SELECT @TotalGood = SUM(b.QtyGood)
                            FROM  {_ConnectStr.APSDB}.[dbo].[Assignment] AS a
                            LEFT JOIN  {_ConnectStr.APSDB}.[dbo].[WIP] AS b ON a.SeriesID = b.SeriesID
                            WHERE b.QtyTol>0 AND a.AssignDate <= DATEADD(DAY, DATEDIFF(DAY, 0, GETDATE()), 0)
            
                            -- 計算總工單數量
                            DECLARE @PastOrders INT
                            SELECT @PastOrders = COUNT(DISTINCT OrderID)
                            FROM  {_ConnectStr.APSDB}.[dbo].[Assignment]
                            WHERE AssignDate <= DATEADD(DAY, DATEDIFF(DAY, 0, GETDATE()), 0)

                            -- 計算總需求數量
                            DECLARE @TotalQuantity INT
                            SELECT @TotalQuantity = SUM(b.QtyTol)
                            FROM  {_ConnectStr.APSDB}.[dbo].[Assignment] as a
                            LEFT JOIN  {_ConnectStr.APSDB}.[dbo].[WIP] as b
                            ON a.SeriesID = b.SeriesID
                            WHERE b.QtyTol>0 AND a.AssignDate <= DATEADD(DAY, DATEDIFF(DAY, 0, GETDATE()), 0)


                            -- 計算比率
                            DECLARE @Ratio FLOAT, @YieldRatio FLOAT
                            SET @Ratio = CAST(@TotalOrders AS FLOAT) / @PastOrders
                            SET @YieldRatio = CAST(@TotalGood AS FLOAT) / @TotalQuantity

                            -- 輸出比率
                            SELECT @Ratio AS OrderID_Ratio, @YieldRatio AS YieldRatio";
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
                                OnTimeRate =  Convert.ToDouble(SqlData["OrderID_Ratio"].ToString().Trim());
                                GoodYieldRate = Convert.ToDouble(SqlData["YieldRatio"].ToString().Trim());
                            };
                        }
                    }
                }
            }
            #endregion

            //計算各稼動率平均
            double performance_rate = double.MinValue;
            var weekendlist = availabilities.Where(x => x.Date.DayOfWeek == DayOfWeek.Saturday || x.Date.DayOfWeek == DayOfWeek.Sunday).ToList();
            var weekdaylist = availabilities.Where(x => x.Date.DayOfWeek != DayOfWeek.Saturday && x.Date.DayOfWeek != DayOfWeek.Sunday).ToList();

            
            //時間稼動把實際運轉時間加總/(總機台數*8小時)
            double avg_availability = (weekendlist.Sum(x => x.ActualRun) / (weekendlist.Count() * 60 * 8)*0.5)+ (weekdaylist.Sum(x => x.ActualRun) / (weekdaylist.Count() * 60 * 11)*0.5);
            List<double> avg_performance = new List<double>();
            //性能稼動紀錄(實際加工時間/實際運轉時間)
            foreach(var item in performances)
            {
                if(availabilities.Exists(x => x.Date == item.Date && x.DeviceID == item.DeviceID))
                {
                    var date_run = availabilities.Find(x => x.Date == item.Date && x.DeviceID==item.DeviceID).ActualRun;
                    performance_rate = item.TotalTime / date_run;
                    avg_performance.Add(performance_rate<=1? performance_rate:1);

                }
            }

            // 串接實際資料
            return new ActionResponse<OEEOverView>
            {
                Data = new OEEOverView
                (

                    oEE: new OEERate(Math.Round(avg_availability * avg_performance.Average() * GoodYieldRate* 100d, 2), 60d),
                    availbility: new AvailbilityRate(Math.Round(avg_availability * 100,2), 70d),
                    performance: new PerformanceRate(Math.Round(avg_performance.Average() * 100,2), 97d),
                    yield: new YieldRate(Math.Round(GoodYieldRate * 100d, 2), 95d),
                    delivery: new DeliveryRate(Math.Round(OnTimeRate * 100d, 2), 97d)
                )
            };
        }

        /// <summary>
        /// 取固定天數的整廠OEE
        /// </summary>       
        /// <param name="days">整數值(EX:7、15....)</param> 
        /// <returns></returns>
        [HttpGet("days/{days}")]
        public ActionResponse<List<OEEOverViewHistory>> Get(int days)
        {
            var result = new List<OEEOverViewHistory>();
            var random = new Random();
            List<Availability> availabilities = new List<Availability>();
            List<Performance> performances = new List<Performance>();
            #region 撈取時間稼動
            //取得時間稼動資料(運轉時間扣掉中午休息需大於100分鐘)
            var sqlStr = @$"
                             WITH CTE1 AS (
                                SELECT 
                                    [DatabaseDate] AS Date,
                                    a.[MachineName] AS DeviceID,
                                    --DATEDIFF(MINUTE, MIN(StartDateTime), MAX(EndDateTime)) AS ActualRUN
                                    SUM(DATEDIFF(MINUTE, StartDateTime, EndDateTime)) AS ActualRUN
                                FROM 
                                    {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog] AS a
                                INNER JOIN 
                                    {_ConnectStr.APSDB}.[dbo].[Device] AS b
                                ON 
                                    a.MachineName = b.remark
                                WHERE 
                                    MachineStatus IN (1, 2, 3) 
                                    AND b.SkyMars_connect = 1
                                    AND [DatabaseDate] >= CONVERT(date, DATEADD(DAY, -{days-1}, GETDATE()))
                                    AND [DatabaseDate] < CONVERT(date, GETDATE())
                                GROUP BY 
                                    [DatabaseDate], 
                                    a.[MachineName]
                            ),
                            CTE2 AS (
                                SELECT 
                                    CONVERT(date, CreateTime) AS Date,
                                    DeviceID,
                                    DATEDIFF(MINUTE, MIN(CreateTime), MAX(CreateTime)) AS ActualRUN
                                FROM 
                                    {_ConnectStr.APSDB}.[dbo].[WIPLog]
                                INNER JOIN 
                                    {_ConnectStr.APSDB}.[dbo].[Device] AS b
                                ON 
                                    {_ConnectStr.APSDB}.[dbo].[WIPLog].DeviceID = b.remark
                                WHERE 
                                    b.external_com = 0
                                    AND b.SkyMars_connect = 0
                                    AND CONVERT(date, CreateTime) >= CONVERT(date, DATEADD(DAY, -{days - 1}, GETDATE()))
                                    AND CONVERT(date, CreateTime) < CONVERT(date, GETDATE())
                                GROUP BY 
                                    CONVERT(date, CreateTime), 
                                    DeviceID
                            )
                            SELECT 
                                DeviceID,
                                Date,
                                ActualRUN
                            FROM 
                                CTE1
                            UNION ALL
                            SELECT 
                                DeviceID,
                                Date,
                                ActualRUN
                            FROM 
                                CTE2
                            WHERE 
                                ActualRUN > 100
                            ORDER BY 
                                DeviceID, 
                                Date;";
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
                                availabilities.Add(new Availability
                                (
                                    deviceid: (SqlData["DeviceID"].ToString().Trim()),
                                    date: Convert.ToDateTime(SqlData["Date"].ToString().Trim()),
                                    actualrun: Convert.ToDouble(SqlData["ActualRUN"])
                                ));
                            };
                        }
                    }
                }
            }
            #endregion

            #region 撈取性能稼動
            //取得性能稼動資料
            sqlStr = @$"
                           WITH OrderedData AS (
                                SELECT 
                                    [OrderID],
                                    [OPID],
                                    [WIPEvent],
                                    [DeviceID],
                                    CONVERT(date, CreateTime) AS Date,
                                    CreateTime,
                                    LAG(CreateTime) OVER (PARTITION BY [DeviceID], CONVERT(date, CreateTime) ORDER BY CreateTime) AS PreviousTime
                                FROM 
                                    {_ConnectStr.APSDB}.[dbo].[WIPLog]
                            ),
                            ProcessedData AS (
                                SELECT 
                                    [OrderID],
                                    [OPID],
                                    [DeviceID],
                                    Date,
                                    DATEDIFF(MINUTE, PreviousTime, CreateTime) AS ProcessingTime
                                FROM 
                                    OrderedData
                                INNER JOIN {_ConnectStr.APSDB}.[dbo].[Device] as b
                                ON 
                                    OrderedData.DeviceID = b.remark
                                WHERE 
                                    b.external_com = 0 
                                    AND PreviousTime IS NOT NULL
                                    -- 僅有狀態為暫停或完工，這段時間才有加工
                                    AND WIPEvent in (2,3)
                                    AND DATEDIFF(MINUTE, PreviousTime, CreateTime) > 0
                                    AND b.SkyMars_connect = 0
                            ),
                            WIPLogData AS (
                                SELECT 
                                    DeviceID,
                                    Date,
                                    SUM(ProcessingTime) AS TotalTime
                                FROM 
                                    ProcessedData
                                WHERE 
                                    Date >= CONVERT(date, DATEADD(DAY, -{days - 1}, GETDATE())) 
                                    AND Date < CONVERT(date, GETDATE())
                                GROUP BY 
                                    DeviceID, Date
                            ),
                            UtilizationLogData AS (
                                SELECT 
                                    [DatabaseDate] AS Date,
                                    a.[MachineName] AS DeviceID,
                                    SUM(DATEDIFF(MINUTE, StartDateTime, EndDateTime)) AS TotalTime
                                FROM 
                                    {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog] AS a
                                INNER JOIN 
                                    {_ConnectStr.APSDB}.[dbo].[Device] AS b
                                ON 
                                    a.MachineName = b.remark
                                WHERE 
                                    MachineStatus = 1 
                                    AND b.SkyMars_connect = 1
                                    AND [DatabaseDate] >= CONVERT(date, DATEADD(DAY, -{days-1}, GETDATE()))
                                    AND [DatabaseDate] < CONVERT(date, GETDATE())
                                GROUP BY 
                                    [DatabaseDate], 
                                    a.[MachineName]
                            )
                            SELECT 
                                DeviceID,
                                Date,
                                TotalTime
                            FROM 
                                WIPLogData

                            UNION ALL
                            SELECT 
                                DeviceID,
                                Date,
                                TotalTime
                            FROM 
                                UtilizationLogData
                            ORDER BY 
                                DeviceID, 
                                Date;
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
                                performances.Add(new Performance
                                (
                                    deviceid: (SqlData["DeviceID"].ToString().Trim()),
                                    date: Convert.ToDateTime(SqlData["Date"].ToString().Trim()),
                                    totaltime: Convert.ToDouble(SqlData["TotalTime"])
                                ));
                            };
                        }
                    }
                }
            }

            //計算平均時間&性能稼動率
            
            Dictionary<DateTime, double> avg_AVA  = new Dictionary<DateTime, double>();//每天時間稼動
            List<DateTime> diffdate = availabilities.Select(x => x.Date).Distinct().ToList();
            Dictionary<DateTime, double> avg_performance = new Dictionary<DateTime, double>();//每天性能稼動
            foreach (var date in diffdate)
            {
                List<double> performance_rate = new List<double>();
                var samedaylist = availabilities.Where(x => x.Date == date).ToList();
                //判斷是否為周末加班
                var dayonweekend = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) ? true : false;
                switch(dayonweekend)
                {
                    case true:
                        avg_AVA[date] = (samedaylist.Select(x => x.ActualRun).Sum()) / (samedaylist.Count() * 60 * 8);
                        break;
                    case false:
                        avg_AVA[date] = (samedaylist.Select(x => x.ActualRun).Sum()) / (samedaylist.Count() * 60 * 11);
                        break ;
                }
                
                foreach (var item in performances.Where(x=>x.Date==date))
                {
                    if (availabilities.Exists(x=>x.DeviceID == item.DeviceID && x.Date == item.Date) )
                    {
                        var date_run = availabilities.Find(x => x.Date == item.Date && x.DeviceID == item.DeviceID).ActualRun;
                        performance_rate.Add((item.TotalTime / date_run)<=1? (item.TotalTime / date_run):1);
                    }
                }
                if(performance_rate.Count>0)
                {
                    avg_performance[date] = performance_rate.Average();
                }
                
            }
            #endregion

            #region 撈取各天數良品率與準交率
            //取得工單資料
            //sqlStr = @$"WITH DateRange AS (
            //                    SELECT CAST(GETDATE() AS DATE) AS dt
            //                    UNION ALL
            //                    SELECT DATEADD(DAY, -1, dt)
            //                    FROM DateRange
            //                    WHERE dt > DATEADD(DAY, -{days-1}, GETDATE())
            //                ), CTE AS (
            //                    SELECT
            //                        dr.dt,
            //                        COUNT(DISTINCT CASE WHEN (b.EndTime IS NOT NULL AND a.AssignDate = DATEADD(DAY,1,dr.dt) AND b.EndTime<=DATEADD(DAY,1,dr.dt)) THEN a.OrderID END) AS TotalOrders,
            //                        --SUM(CASE WHEN b.QtyTol > 0 AND ((b.EndTime IS NULL AND a.AssignDate >= dr.dt) OR ((b.EndTime <= a.AssignDate) AND (a.AssignDate<=dr.dt)))  THEN b.QtyGood END) AS TotalGood,
            //                        COUNT(DISTINCT CASE WHEN a.AssignDate <= DATEADD(DAY,1,dr.dt) THEN a.OrderID END) AS PastOrders,
            //                        --SUM(CASE WHEN b.QtyTol > 0 AND ((b.EndTime IS NULL AND a.AssignDate >= dr.dt) OR ((b.EndTime <= a.AssignDate) AND (a.AssignDate<=dr.dt)))  THEN b.QtyTol END) AS TotalQuantity,
		          //                  SUM(CASE WHEN c.CreateTime <= DATEADD(DAY,1,dr.dt) THEN c.QtyGood + c.QtyBad ELSE 0 END) AS TotalQuantity, 
		          //                  SUM(CASE WHEN c.CreateTime <= DATEADD(DAY,1,dr.dt) THEN c.QtyGood ELSE 0 END) AS TotalGood
	           //                 FROM DateRange dr
            //                    CROSS JOIN {_ConnectStr.APSDB}.[dbo].[Assignment] a
            //                    inner JOIN {_ConnectStr.APSDB}.[dbo].[WIP] b ON a.OrderID = b.OrderID AND a.OPID = b.OPID
	           //                 inner JOIN  {_ConnectStr.APSDB}.[dbo].[WIPLog] c on a.OrderID=c.OrderID and a.OPID=c.OPID
            //                    GROUP BY dr.dt
            //                )
            //                SELECT
            //                    dt,
            //                    TotalOrders,
            //                    TotalGood,
            //                    PastOrders,
            //                    TotalQuantity,
            //                    CASE WHEN PastOrders = 0 THEN 0 ELSE CAST(TotalOrders AS FLOAT) / PastOrders END AS OrderID_Ratio,
            //                    CASE WHEN TotalQuantity = 0 THEN 0 ELSE CAST(TotalGood AS FLOAT) / TotalQuantity END AS YieldRatio
            //                FROM CTE
            //                order by dt desc";

            sqlStr = @$"WITH DateRange AS (
                            SELECT CAST(GETDATE() AS DATE) AS dt
                            UNION ALL
                            SELECT DATEADD(DAY, -1, dt)
                            FROM DateRange
                            WHERE dt > DATEADD(DAY, -{days-1}, GETDATE())
                        ), OrderCompletion AS (
                            SELECT 
                                a.OrderID,
                                a.AssignDate,
                                MAX(CASE WHEN b.EndTime IS NOT NULL AND b.EndTime <= a.AssignDate THEN 1 ELSE 0 END) AS AllCompletedOnTime,
                                MAX(CASE WHEN b.EndTime > a.AssignDate THEN 1 ELSE 0 END) AS AnyCompletedLate,
                                COUNT(*) AS TotalSteps,
                                SUM(CASE WHEN b.WIPEvent = 3 THEN 1 ELSE 0 END) AS CompletedSteps
                            FROM {_ConnectStr.APSDB}.[dbo].[Assignment] a
                            INNER JOIN {_ConnectStr.APSDB}.[dbo].[WIP] b ON a.OrderID = b.OrderID AND a.OPID = b.OPID
                            GROUP BY a.OrderID, a.AssignDate
                        ), CTE AS (
                            SELECT
                                dr.dt,
                                COUNT(DISTINCT CASE 
                                    WHEN a.AssignDate <= dr.dt 
                                    AND oc.AllCompletedOnTime = 1 
                                    AND oc.AnyCompletedLate = 0 
                                    AND oc.TotalSteps = oc.CompletedSteps 
                                    THEN a.OrderID 
                                    END) AS TotalOrders,
                                COUNT(DISTINCT CASE WHEN a.AssignDate <= dr.dt THEN a.OrderID END) AS PastOrders,
                                SUM(CASE WHEN c.CreateTime <= dr.dt THEN c.QtyGood + c.QtyBad ELSE 0 END) AS TotalQuantity,
                                SUM(CASE WHEN c.CreateTime <= dr.dt THEN c.QtyGood ELSE 0 END) AS TotalGood
                            FROM DateRange dr
                            CROSS JOIN {_ConnectStr.APSDB}.[dbo].[Assignment] a
                            INNER JOIN OrderCompletion oc ON a.OrderID = oc.OrderID AND a.AssignDate = oc.AssignDate
                            INNER JOIN {_ConnectStr.APSDB}.[dbo].[WIPLog] c ON a.OrderID = c.OrderID AND a.OPID = c.OPID
                            GROUP BY dr.dt
                        )
                        SELECT
                            dt,
                            TotalOrders,
                            TotalGood,
                            PastOrders,
                            TotalQuantity,
                            CASE WHEN PastOrders = 0 THEN 0 ELSE CAST(TotalOrders AS FLOAT) / PastOrders END AS OrderID_Ratio,
                            CASE WHEN TotalQuantity = 0 THEN 0 ELSE CAST(TotalGood AS FLOAT) / TotalQuantity END AS YieldRatio
                        FROM CTE
                        ORDER BY dt DESC;
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
                                var ava_temp = 0.0;
                                var per_temp = 0.0;
                                var day = Convert.ToDateTime(SqlData["dt"]);
                                if(avg_AVA.ContainsKey(day))
                                {
                                    ava_temp = avg_AVA[day];
                                }
                                if (avg_performance.ContainsKey(day))
                                {
                                    per_temp = avg_performance[day];
                                }
                           
                                var yie_temp = Math.Round(Convert.ToDouble(SqlData["YieldRatio"].ToString().Trim()), 2);
                                var del_temp = Math.Round(Convert.ToDouble(SqlData["OrderID_Ratio"].ToString().Trim()), 2);
                                result.Add(new OEEOverViewHistory
                                (
                                    date: Convert.ToDateTime(SqlData["dt"]).ToString("yyyy/MM/dd").Trim(),

                                    oeeOverView: new OEEOverView
                                    (
                                        
                                        availbility: new AvailbilityRate(Math.Round(ava_temp * 100, 2), 90d),
                                        performance: new PerformanceRate(Math.Round(per_temp * 100, 2), 90d),
                                        yield: new YieldRate(Math.Round(yie_temp * 100, 2), 95d),
                                        delivery: new DeliveryRate(Math.Round(del_temp * 100, 2), 95d),
                                        oEE: new OEERate(Math.Round(ava_temp * per_temp * yie_temp * 100, 2), 90d)
                                    )

                                ));
                            };
                        }
                    }
                }
            }
            #endregion



            //for (int i = 0; i < days; i++)
            //{
            //    result.Add(new OEEOverViewHistory
            //    (
            //        date: DateTime.Now.AddDays(-i).ToString("yyyy/MM/dd"),
            //        oeeOverView: new OEEOverView
            //        (
            //            oEE: new OEERate(Math.Round(random.NextDouble() * 100, 2), 60d),
            //            availbility: new AvailbilityRate(Math.Round(random.NextDouble() * 100, 2), 90d),
            //            performance: new PerformanceRate(Math.Round(random.NextDouble() * 100, 2), 64d),
            //            yield: new YieldRate(Math.Round(random.NextDouble() * 100, 2), 95d),
            //            delivery: new DeliveryRate(Math.Round(random.NextDouble() * 100, 2), 95d)
            //        )
            //    ));
            //}
            return new ActionResponse<List<OEEOverViewHistory>>
            {
                Data = result
            };           
        }

        /// <summary>
        /// 取回良品率細節列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("YieldDetails")]
        public ActionResponse<List<YiledDetails>> GetYieldRateDetails()
        {
            var result = new List<YiledDetails>();
            #region 撈取品名與良率資料
            //取得工單資料
            var sqlStr = @$"SELECT
                                MAKTX,
                                CASE WHEN TotalQtyTol > 0 THEN CAST(TotalQtyGood AS FLOAT) / TotalQtyTol ELSE 0 END AS Ratio
                            FROM
                                (
                                    SELECT
                                        a.MAKTX,
                                        SUM(b.QtyTol) AS TotalQtyTol,
                                        SUM(b.QtyGood) AS TotalQtyGood
                                    FROM
                                        {_ConnectStr.APSDB}.[dbo].[Assignment] AS a
                                    LEFT JOIN
                                        {_ConnectStr.APSDB}.[dbo].[WIP] AS b ON a.OrderID = b.OrderID AND a.OPID = b.OPID
                                    WHERE
                                        a.AssignDate < GETDATE() AND b.QtyTol > 0
                                    GROUP BY
                                        a.MAKTX
                                ) AS SubQuery";
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
                                result.Add(new YiledDetails
                                (
                                    proudctName: SqlData["MAKTX"].ToString().Trim(),
                                    rateValue: Math.Round(Convert.ToDouble( SqlData["Ratio"].ToString().Trim())*100,2)
                                ));

                            };
                        }
                    }
                }
            }
            #endregion

            //string[] product = new string[] { "AS-ASF0060WQR-FPOX", "AF-ASF0060PPW-FAOX", "AS-ASF0075WRQ-FPOX", "AS-ASF0070WQR-FPOX", "AS-ASF0080WQR-FPOX",
            //    "AK-ASF0060QQR-FPOX", "AS-ASF0100WQR-FPOX", "AT-ASF0060WQR-FPOX" ,"AP-ASF0060WQR-FPOX", "AK-ASF0060WQR-FFPS" };
            
            //for(int i = 0; i < 10; i++)
            //{
            //    result.Add(new YiledDetails
            //    (
            //        proudctName: product[i],
            //        rateValue : (i * 10) + 5.6
            //    ));
            //}
            return new ActionResponse<List<YiledDetails>>
            {
                Data = result
            };
        }

        /// <summary>
        /// 取回機台一周稼動率統計
        /// </summary>
        /// <param name="device">請輸入 Factroy ProductionLine Device Name</param>
        /// <returns></returns>
        [HttpPost("week")]
        public ActionResponse<WeekUtilization> GetOeeofWeek([FromBody] RequestFactory device)
        {
            Random random = new Random();
            var result = new List<Utilization>();
            for (int i = 1; i <= 7; i++)
            {
                //result.Add(new Utilization
                //    (
                //        //創建一個包含日期和星期幾的字串ex."02/23(Mon)"
                //        date: $@"{DateTime.Now.AddDays(-i):MM/dd}({CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-i).DayOfWeek)[2]})", 
                //        run : 40.0 + i, 
                //        idle : 20 - i, 
                //        alarm : 20 + 2 * i,
                //        off : 20 - 2 * i));
                var run = 50 + (random.NextDouble() * (70 - 50));//50~70
                var idle = 10 + (random.NextDouble() * (30 - 10));//10~30
                var alarm = 5 + (random.NextDouble() * (10 - 5));//5~10

                result.Add(new Utilization
                    (
                        //創建一個包含日期和星期幾的字串ex."02/23(Mon)"
                        date: $@"{DateTime.Now.AddDays(-i):MM/dd}({CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(-i).DayOfWeek)[2]})",
                        run: run,//50~70
                        idle: idle,//10~30
                        alarm: alarm,//5~10
                        off: 100- (run+ idle+ alarm)));
            }
            return new ActionResponse<WeekUtilization>
            {
                Data = new WeekUtilization(device.DeviceName, result)
            };
        }
    }
}
