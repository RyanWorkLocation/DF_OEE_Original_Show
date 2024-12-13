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
    public class ScheduleController : BaseApiController
    {
        ConnectStr _ConnectStr = new ConnectStr();
        public ScheduleController()
        {

        }

        /// <summary>
        /// 取得WipStatus Mapping
        /// </summary>
        /// <returns></returns>      
        [HttpGet("define")]
        public ActionResponse<List<StatusMpaping>> GetStatusDefine()
        {
            var wipStatus = new string[] { "ReceiveConfirm", "Dispatch", "WIPStart", "WIPFinish", "Pause" };
            var wipStatusText = new string[] { "收料確認", "派工完成", "加工開始", "加工完成", "暫停加工" };
            var wipStatusColor = new string[] { "yellow", "yellow", "green", "gray", "red" };
            var result = new List<StatusMpaping>();
            for (int i = 0; i < wipStatus.Length; i++)
            {
                result.Add(new StatusMpaping(wipStatus[i], wipStatusText[i], wipStatusColor[i]));
            }
            return new ActionResponse<List<StatusMpaping>>
            {
                Data = result
            };
        }

        /// <summary>
        /// 取回排程狀態
        /// </summary>
        /// <param name="request">廠區名稱 EX: FA-05 產線名稱 EX: 空白、PR-01 機台名稱 EX: 空白、CNC-11</param>
        /// <returns></returns>
        [HttpPost("Status")]
        public ActionResponse<List<ScheduleInformation>> GetScheduleInformationsByFactory([FromBody] RequestFactory request)
        {
            var templist = new List<OrderInfoTemp>();
            var result = new List<ScheduleInformation>();
            //Dispatch:派工完成 Finish:加工完成 ReceiveConfirm:收料確認 WIPStart:加工開始 WIPFinish:加工結束 Pause:暫停
            var wipStatus = new string[] { "ReceiveConfirm", "Dispatch", "WIPStart", "WIPFinish", "Pause" };
            // 使用初始化器將字符串數組轉換為字典
            Dictionary<int, string> wipStatusDictionary = new Dictionary<int, string>
            {
                { -1, "ReceiveConfirm" },
                { 0, "Dispatch" },
                { 1, "WIPStart" },
                { 3, "WIPFinish" },
                { 2, "Pause" }
            };
            #region 撈取排程資料
            //取得工單資料
            var sqlStr = @$"WITH RankedWIP AS (
                            SELECT 
                                a.OrderID,
                                a.OPID,
                                b.OPLTXA1,
                                c.[CustomerInfo],
                                d.[Name],
                                b.MAKTX,
                                a.OrderQTY,
                                a.QtyGood,
                                b.StartTime,
                                b.EndTime,
                                b.AssignDate,
                                WIPSTATUS = 
                                    CASE WHEN b.StartTime IS NOT NULL THEN a.WIPEvent ELSE -1 END,
                                DelayDays = 
                                    CASE 
                                        WHEN a.EndTime IS NOT NULL THEN 
                                            CASE WHEN DATEDIFF(DAY, b.AssignDate, a.EndTime) < 0 THEN 0 ELSE DATEDIFF(DAY, b.AssignDate, a.EndTime) END
                                        ELSE 
                                            CASE WHEN DATEDIFF(DAY, b.AssignDate, GETDATE()) < 0 THEN 0 ELSE DATEDIFF(DAY, b.AssignDate, GETDATE()) END
                                    END,
                                ROW_NUMBER() OVER (PARTITION BY 
                                    CASE WHEN b.StartTime IS NOT NULL THEN a.WIPEvent ELSE -1 END 
                                    ORDER BY a.OrderID DESC, a.OPID DESC) AS RowNum
                            FROM 
                                {_ConnectStr.APSDB}.[dbo].[WIP] AS a
                            INNER JOIN 
                                {_ConnectStr.APSDB}.[dbo].[Assignment] AS b ON a.OrderID = b.OrderID AND a.OPID = b.OPID
                            INNER JOIN 
                                {_ConnectStr.APSDB}.[dbo].[OrderOverview] AS c ON b.ERPOrderID = c.OrderID
                            INNER JOIN 
                                {_ConnectStr.MRPDB}.[dbo].[Part] AS d ON b.maktx = d.number
                        )
                        SELECT 
                            OrderID,
                            OPID,
                            OPLTXA1,
                            CustomerInfo,
                            Name,
                            MAKTX,
                            OrderQTY,
                            QtyGood,
                            StartTime,
                            EndTime,
                            AssignDate,
                            WIPSTATUS,
                            DelayDays
                        FROM RankedWIP
                        WHERE RowNum <= 40";
            if(!String.IsNullOrEmpty(request.DeviceName))
            {
                sqlStr += $" AND b.WorkGroup='{request.DeviceName}'";
            }
            sqlStr += "ORDER BY WIPSTATUS ASC,OrderID DESC, OPID DESC";
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
                                templist.Add(new OrderInfoTemp
                                {
                                    OrderNo = SqlData["OrderID"].ToString().Trim(),
                                    OPNo = Convert.ToInt32(SqlData["OPID"].ToString().Trim()),
                                    OPName = SqlData["OPLTXA1"].ToString().Trim(),
                                    ProductNo = SqlData["Name"].ToString().Trim(),
                                    RequireCount = Convert.ToInt32(SqlData["OrderQTY"].ToString().Trim()),
                                    CurrentCount = Convert.ToInt32(SqlData["QtyGood"].ToString().Trim()),
                                    DueDate = Convert.ToDateTime(SqlData["AssignDate"]).ToString("yyyy-MM-dd"),
                                    WIPStatus = wipStatusDictionary[Convert.ToInt32(SqlData["WIPSTATUS"])],
                                    StratTime = SqlData["StartTime"].ToString().Trim(),
                                    EndTime = SqlData["EndTime"].ToString().Trim(),
                                    DelayDays = Convert.ToInt32(SqlData["DelayDays"].ToString().Trim()),
                                    CustomerInfo = SqlData["CustomerInfo"].ToString().Trim().Substring(0,4),

                                });
                            };
                        }
                    }
                }
            }
            #endregion

            var statuslist = templist.Select(x => x.WIPStatus).Distinct();
            foreach(var status in statuslist)
            {
                foreach(var item in templist.Where(x=>x.WIPStatus== status).Take(40))
                {
                    var temp = new OrderInformation(orderNo: item.OrderNo, oPNo: item.OPNo, opName: item.OPName,
                        productNo: item.ProductNo, requireCount: item.RequireCount, currentCount: item.CurrentCount, dueDate: item.DueDate, customerinfo: item.CustomerInfo);
                    result.Add(new ScheduleInformation(temp, item.WIPStatus, item.StratTime,
                        item.EndTime, item.DelayDays));
                }
            }
            //各狀態放50筆資料顯示
            




            return new ActionResponse<List<ScheduleInformation>>
            {
                Data = result
            };
        }

        /// <summary>
        /// 取回延遲工單列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("DelayedOrderList")]
        public ActionResponse<List<DelayOrderList>> GetDelayedList ()
        {
            var result = new List<DelayOrderList>();
            #region 撈取排程資料
            //取得工單資料
            var sqlStr = @$"
                            -- 遲交工單
                            SELECT DISTINCT a.OrderID,a.AssignDate
                            FROM {_ConnectStr.APSDB}.[dbo].[Assignment] AS a
                            LEFT JOIN {_ConnectStr.APSDB}.[dbo].[WIP] AS b ON a.SeriesID = b.SeriesID
                            WHERE (b.EndTime IS NULL OR b.EndTime <= a.AssignDate) AND (AssignDate <= DATEADD(DAY, DATEDIFF(DAY, 0, GETDATE()), 0))
                            order by a.AssignDate 
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
                                result.Add(new DelayOrderList
                                {
                                    OrderID = SqlData["OrderID"].ToString().Trim(),
                                    AssignData = SqlData["AssignDate"].ToString().Trim()
                                });
                            }
                        }
                    }
                }
            }
            #endregion

            return new ActionResponse<List<DelayOrderList>>
            {
                Data = result
            };
        }

        /// <summary>
        /// 取回延遲工單製程資訊
        /// </summary>
        /// <returns></returns>
        [HttpGet("DelayedOrderInformation")]
        public ActionResponse<List<DelayOrderInformation>> DelayedOrderInformation(string OrderID)
        {
            var result = new List<DelayOrderInformation>();
            var StatusDic = new Dictionary<int, string> { {0, "未開工"},{1,"加工中"},{2, "暫停中"},{3, "已完成" } };
            #region 撈取排程資料
            //取得工單資料
            var sqlStr = @$"
                            SELECT w.OPID,p.ProcessName,w.WIPEvent,w.OrderQTY,w.QtyGood,
                            ROUND(CAST(w.QtyGood as float) / w.OrderQTY * 100, 2) as CompletionRate
                            FROM {_ConnectStr.APSDB}.[dbo].[WIP] as w
                            LEFT JOIN {_ConnectStr.MRPDB}.[dbo].[Process] as p
                            on w.OPID=p.ID
                            LEFT JOIN  {_ConnectStr.APSDB}.[dbo].[Assignment] as a
                            on w.SeriesID=a.SeriesID
                            where w.OrderID='{OrderID}'
                            order by a.Range
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
                                result.Add(new DelayOrderInformation
                                {
                                    OPID = SqlData["OPID"].ToString().Trim(),
                                    OPName = SqlData["ProcessName"].ToString().Trim(),
                                    OPStatus = StatusDic[Convert.ToInt16(SqlData["WIPEvent"])],
                                    Progress = Convert.ToDouble(SqlData["CompletionRate"]),
                                    OrderQty = Convert.ToInt16(SqlData["OrderQTY"]),
                                    CompleteQty = Convert.ToInt16(SqlData["QtyGood"])
                                });
                            }
                        }
                    }
                }
            }
            #endregion

            return new ActionResponse<List<DelayOrderInformation>>
            {
                Data = result
            };
        }
    }
}
