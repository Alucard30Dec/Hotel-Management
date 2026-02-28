using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class CustomerDAL
    {
        private readonly AuditLogDAL _auditLogDal = new AuditLogDAL();

        public List<Customer> GetAll()
        {
            var list = new List<Customer>();
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string query = @"SELECT KhachHangID, HoTen, CCCD, DienThoai, DiaChi
                                 FROM KHACHHANG
                                 WHERE COALESCE(DataStatus, 'active') <> 'deleted'";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new Customer
                        {
                            KhachHangID = rd.GetInt32(0),
                            HoTen = rd.GetString(1),
                            CCCD = rd.GetString(2),
                            DienThoai = rd.IsDBNull(3) ? null : rd.GetString(3),
                            DiaChi = rd.IsDBNull(4) ? null : rd.GetString(4)
                        });
                    }
                }
            }
            return list;
        }

        public void Insert(Customer c)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string actor = AuditContext.ResolveActor(null);
                DateTime nowUtc = DateTime.UtcNow;
                string query = @"INSERT INTO KHACHHANG
                                 (HoTen, CCCD, DienThoai, DiaChi,
                                  CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy, DataStatus)
                                 VALUES(@HoTen, @CCCD, @DienThoai, @DiaChi,
                                        @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy, 'active');
                                 SELECT LAST_INSERT_ID();";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@HoTen", c.HoTen);
                cmd.Parameters.AddWithValue("@CCCD", c.CCCD);
                cmd.Parameters.AddWithValue("@DienThoai", (object)c.DienThoai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DiaChi", (object)c.DiaChi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", nowUtc);
                cmd.Parameters.AddWithValue("@CreatedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);

                int customerId = Convert.ToInt32(cmd.ExecuteScalar());

                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "KHACHHANG",
                    EntityId = customerId,
                    ActionType = "CREATE",
                    Actor = actor,
                    Source = "CustomerDAL.Insert",
                    AfterData = AuditLogDAL.SerializeState(new Dictionary<string, object>
                    {
                        { "KhachHangID", customerId },
                        { "HoTen", c.HoTen },
                        { "CCCD", c.CCCD },
                        { "DienThoai", c.DienThoai },
                        { "DiaChi", c.DiaChi },
                        { "DataStatus", "active" }
                    })
                });
            }
        }

        public void Update(Customer c)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string actor = AuditContext.ResolveActor(null);
                var before = GetCustomerSnapshot(conn, c.KhachHangID);

                string query = @"UPDATE KHACHHANG
                                 SET HoTen = @HoTen,
                                     CCCD = @CCCD,
                                     DienThoai = @DienThoai,
                                     DiaChi = @DiaChi,
                                     UpdatedAtUtc = @UpdatedAtUtc,
                                     UpdatedBy = @UpdatedBy,
                                     DataStatus = 'active'
                                 WHERE KhachHangID = @KhachHangID";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@HoTen", c.HoTen);
                cmd.Parameters.AddWithValue("@CCCD", c.CCCD);
                cmd.Parameters.AddWithValue("@DienThoai", (object)c.DienThoai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DiaChi", (object)c.DiaChi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.Parameters.AddWithValue("@KhachHangID", c.KhachHangID);
                cmd.ExecuteNonQuery();

                var after = GetCustomerSnapshot(conn, c.KhachHangID);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "KHACHHANG",
                    EntityId = c.KhachHangID,
                    ActionType = "UPDATE",
                    Actor = actor,
                    Source = "CustomerDAL.Update",
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });
            }
        }

        public void Delete(int id)
        {
            using (MySqlConnection conn = DbHelper.GetConnection())
            {
                string actor = AuditContext.ResolveActor(null);
                var before = GetCustomerSnapshot(conn, id);

                string query = @"UPDATE KHACHHANG
                                 SET DeletedAtUtc = @DeletedAtUtc,
                                     DeletedBy = @DeletedBy,
                                     UpdatedAtUtc = @UpdatedAtUtc,
                                     UpdatedBy = @UpdatedBy,
                                     DataStatus = 'deleted'
                                 WHERE KhachHangID = @Id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DeletedAtUtc", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@DeletedBy", actor);
                cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@UpdatedBy", actor);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();

                var after = GetCustomerSnapshot(conn, id);
                _auditLogDal.Write(new AuditLogDAL.AuditLogWriteModel
                {
                    EntityName = "KHACHHANG",
                    EntityId = id,
                    ActionType = "SOFT_DELETE",
                    Actor = actor,
                    Source = "CustomerDAL.Delete",
                    BeforeData = AuditLogDAL.SerializeState(before),
                    AfterData = AuditLogDAL.SerializeState(after)
                });
            }
        }

        private static Dictionary<string, object> GetCustomerSnapshot(MySqlConnection conn, int customerId)
        {
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            string query = @"SELECT KhachHangID, HoTen, CCCD, DienThoai, DiaChi, DataStatus, UpdatedAtUtc, UpdatedBy
                             FROM KHACHHANG
                             WHERE KhachHangID = @KhachHangID";
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@KhachHangID", customerId);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return data;
                    data["KhachHangID"] = rd.IsDBNull(0) ? (object)null : rd.GetInt32(0);
                    data["HoTen"] = rd.IsDBNull(1) ? null : rd.GetString(1);
                    data["CCCD"] = rd.IsDBNull(2) ? null : rd.GetString(2);
                    data["DienThoai"] = rd.IsDBNull(3) ? null : rd.GetString(3);
                    data["DiaChi"] = rd.IsDBNull(4) ? null : rd.GetString(4);
                    data["DataStatus"] = rd.IsDBNull(5) ? null : rd.GetString(5);
                    data["UpdatedAtUtc"] = rd.IsDBNull(6) ? (object)null : rd.GetDateTime(6);
                    data["UpdatedBy"] = rd.IsDBNull(7) ? null : rd.GetString(7);
                }
            }
            return data;
        }
    }
}
