using PMCDash.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
namespace PMCDash.Repos
{
    public class ReportRepo
    {
        private readonly string _connectStr;

        public ReportRepo()
        {
            _connectStr = @"Data Source=ACER\SQLEXPRESS;Initial Catalog=MES2014V2;integrated security=true;";
        }

        public IEnumerable<ReporterInformation> GetImformation()
        {
            var result = new List<ReporterInformation>();
            var sqlStr = @"select DCSW.生產機台, DCSW.生產員工代號, DCSW.加工單號, O.成品料號, DCSW.批號數量, DCSW.已生產數量, DCSW.狀態
                           from DeviceCurrentStatusWIP DCSW, Orders o
                           where DCSW.加工單號 = o.加工單號";
            using (var conn = new SqlConnection(_connectStr))
            {
                using (var comm = new SqlCommand(sqlStr, conn))
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    using (var data = comm.ExecuteReader())
                    {
                        if (data.HasRows)
                        {
                            while (data.Read())
                            {
                                var temp = new ReporterInformation();
                                temp.DeviceName = data["生產機台"].ToString();
                                temp.Operator = data["生產員工代號"].ToString();
                                temp.OrderID = data["加工單號"].ToString();
                                temp.Maktxt = data["成品料號"].ToString();
                                temp.OringinCount = Convert.ToInt32(data["批號數量"].ToString());
                                temp.RepotedConut = Convert.ToInt32(data["已生產數量"].ToString());
                                temp.OrderStatus = data["狀態"].ToString();
                                result.Add(temp);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public ReporterInformation GetImformation(string orderID)
        {
            var result = new ReporterInformation();
            var sqlStr = @"select top 1 DCSW.生產機台, DCSW.生產員工代號, DCSW.加工單號, O.成品料號, DCSW.批號數量, DCSW.已生產數量, DCSW.狀態
                           from DeviceCurrentStatusWIP DCSW, Orders o
                           where DCSW.加工單號  = @orderID and DCSW.加工單號 = o.加工單號";
            using (var conn = new SqlConnection(_connectStr))
            {
                using (var comm = new SqlCommand(sqlStr, conn))
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    comm.Parameters.AddWithValue("@orderID", orderID);
                    using (var data = comm.ExecuteReader())
                    {
                        if (data.HasRows)
                        {
                            data.Read();
                            result.DeviceName = data["生產機台"].ToString();
                            result.Operator = data["生產員工代號"].ToString();
                            result.OrderID = data["加工單號"].ToString();
                            result.Maktxt = data["成品料號"].ToString();
                            result.OringinCount = Convert.ToInt32(data["批號數量"].ToString());
                            result.RepotedConut = Convert.ToInt32(data["已生產數量"].ToString());
                            result.OrderStatus = data["狀態"].ToString();
                        }
                    }
                }
            }
            return result;
        }
    }
}
