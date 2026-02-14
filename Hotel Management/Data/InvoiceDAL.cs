using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class InvoiceDAL
    {
        public void CreateInvoice(Invoice invoice)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"INSERT INTO HOADON (DatPhongID, NgayLap, TongTien, DaThanhToan)
                                 VALUES(@DatPhongID, @NgayLap, @TongTien, @DaThanhToan)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DatPhongID", invoice.DatPhongID);
                cmd.Parameters.AddWithValue("@NgayLap", invoice.NgayLap);
                cmd.Parameters.AddWithValue("@TongTien", invoice.TongTien);
                cmd.Parameters.AddWithValue("@DaThanhToan", invoice.DaThanhToan ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        public List<Invoice> GetInvoices(DateTime from, DateTime to)
        {
            var list = new List<Invoice>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT HoaDonID, DatPhongID, NgayLap, TongTien, DaThanhToan
                                 FROM HOADON
                                 WHERE NgayLap BETWEEN @From AND @To";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new Invoice
                        {
                            HoaDonID = rd.GetInt32(0),
                            DatPhongID = rd.GetInt32(1),
                            NgayLap = rd.GetDateTime(2),
                            TongTien = rd.GetDecimal(3),
                            DaThanhToan = rd.GetBoolean(4)
                        });
                    }
                }
            }
            return list;
        }
    }
}