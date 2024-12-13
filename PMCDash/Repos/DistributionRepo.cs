using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using PMCDash.Models;
namespace PMCDash.Repos
{
    public class DistributionRepo
    {
        private readonly string _connStr;
        public DistributionRepo()
        {
            // Integrated Security=SSPI;
            // user id = pmc; password = PMCMES
            _connStr = @"Data Source = DESKTOP-OCCROF4\SQLEXPRESS; Initial Catalog = MES2014V2; Integrated Security=SSPI";
        }

        public List<DeviceStatus> GetDeviceStatusDistribution(string factory = "")
        {
            string sqlStr = @"select a.DeviceID,b.DeviceStatus,a.WorkCenter_No,
                              c.OrderNo,d.ProcessNo,d.ProductNo,d.BookingShippingDate,c.OrderQty,c.CompleteQty
                              from [dbo].[Devices] a 
                              left join [dbo].[DeviceCurrentStatus] b on a.DeviceID = b.DeviceID
                              left join [dbo].[DeviceCurrentStatusWIP] c on a.DeviceID = b.DeviceID
                              left join [dbo].[Orders] d on a.DeviceID = d.MaterialNo";
            var result = new List<DeviceStatus>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                using (var comm = new SqlCommand(sqlStr, conn))
                {
                    using (var sqlData = comm.ExecuteReader())
                    {
                        if (sqlData.HasRows)
                        {
                            while (sqlData.Read())
                            {
                                result.Add(new DeviceStatus(sqlData[0].ToString(), sqlData[1].ToString()));
                            }
                        }
                        else
                        {
                            throw new Exception("Not find");
                        }
                    }
                }
            }
            return result;
        }
    }
}
