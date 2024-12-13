using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PMCDash.Models;
using System.Data;
using System.Data.SqlClient;
namespace PMCDash.Services
{
    public class AlarmService
    {
        ConnectStr _ConnectStr = new ConnectStr();
        public AlarmService()
        {

        }

        public List<AlarmStatistics> GetAlarm(object requst)
        {
            switch (requst)
            {
                case ActionRequest<Factory> req:
                    break;
                case ActionRequest<RequestFactory> req:
                    break;
            }

            var result = new List<AlarmStatistics>();
            Random random = new Random();

            #region 撈取警報資料
            //
            var sqlStr = @$"WITH AlarmStats AS (
                            SELECT 
                                AlarmMessage,
                                COUNT(*) AS OccurrenceCount,
                                SUM(DATEDIFF(MINUTE, StartDateTime, EndDateTime)) AS TotalDurationMinutes
                            FROM 
                               {_ConnectStr.SkyMarsDB}.[dbo].[UtilizationLog] AS U
                            WHERE 
                                AlarmMessage != ''
                            GROUP BY 
                                AlarmMessage
                        ),
                        CTE AS (
                            SELECT 
                                a.OrderID, 
                                a.OPID,
                                a.StartTime,
                                ISNULL(a.EndTime, GETDATE()) AS EndTime, 
                                a.ReasonCode, 
                                b.item, 
                                b.Category,
                                DATEDIFF(MINUTE, a.StartTime, ISNULL(a.EndTime, GETDATE())) AS IdleTime
                            FROM 
                                {_ConnectStr.APSDB}.[dbo].[IdleReasonBinding] AS a
                            LEFT JOIN 
                                {_ConnectStr.APSDB}.[dbo].[IdleResult] AS b
                            ON 
                                a.ReasonCode = b.ID
                            WHERE 
                                b.Category = 2
                        )
                        SELECT 
                        TOP 10
                        AlarmMessage,
                        OccurrenceCount,
                        TotalDurationMinutes
                    FROM 
                        (
                            SELECT 
                                AlarmMessage,
                                OccurrenceCount,
                                TotalDurationMinutes
                            FROM 
                                AlarmStats
                            UNION ALL
                            SELECT 
                                item AS AlarmMessage,
                                COUNT(*) AS OccurrenceCount,
                                SUM(CAST(IdleTime AS BIGINT)) AS TotalDurationMinutes
                            FROM 
                                CTE
                            WHERE 
                                item LIKE '%故障%' OR 
                                item LIKE '%維修%' OR 
                                item LIKE '%跳電%'
                            GROUP BY 
                                item
                        ) AS CombinedResults
                    ORDER BY  
                        OccurrenceCount DESC;";
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

                                result.Add(new AlarmStatistics(
                                    //alarmMSg:"",
                                    alarmMSg: (SqlData["AlarmMessage"]).ToString(),
                                    times: Convert.ToInt32(SqlData["OccurrenceCount"]),
                                    totalMin: Convert.ToDouble(SqlData["TotalDurationMinutes"]),
                                    display:""
                                    ));

                            };
                        }
                    }
                }
            }
            #endregion



            for(int i=0;i<result.Count();i++)
            {
                result[i].AlarmMSg = $"Alarm_{i + 1}";
            }

            return result;
        }

    }
}
