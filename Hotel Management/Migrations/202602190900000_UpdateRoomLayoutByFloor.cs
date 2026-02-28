namespace Hotel_Management.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;

    public partial class UpdateRoomLayoutByFloor : DbMigration
    {
        private static readonly string[] TargetRoomCodes =
        {
            "01", "02", "03", "04",
            "101", "102", "103", "104", "105",
            "201", "202"
        };

        private static readonly string[] LegacyRoomCodes =
        {
            "101", "102", "103", "104", "105",
            "201", "202", "203", "204", "205",
            "301", "302", "303", "304", "305"
        };

        public override void Up()
        {
            EnsureRoom("01", 1, 0);
            EnsureRoom("02", 1, 0);
            EnsureRoom("03", 1, 0);
            EnsureRoom("04", 2, 0);

            EnsureRoom("101", 1, 1);
            EnsureRoom("102", 1, 1);
            EnsureRoom("103", 1, 1);
            EnsureRoom("104", 1, 1);
            EnsureRoom("105", 1, 1);

            EnsureRoom("201", 2, 2);
            EnsureRoom("202", 1, 2);

            RemoveDuplicateCodes(TargetRoomCodes);
            DeleteRoomsOutside(TargetRoomCodes);
        }

        public override void Down()
        {
            EnsureRoom("101", 1, 1);
            EnsureRoom("102", 1, 1);
            EnsureRoom("103", 1, 1);
            EnsureRoom("104", 1, 1);
            EnsureRoom("105", 1, 1);

            EnsureRoom("201", 2, 2);
            EnsureRoom("202", 2, 2);
            EnsureRoom("203", 2, 2);
            EnsureRoom("204", 2, 2);
            EnsureRoom("205", 2, 2);

            EnsureRoom("301", 2, 3);
            EnsureRoom("302", 2, 3);
            EnsureRoom("303", 2, 3);
            EnsureRoom("304", 2, 3);
            EnsureRoom("305", 2, 3);

            RemoveDuplicateCodes(LegacyRoomCodes);
            DeleteRoomsOutside(LegacyRoomCodes);
        }

        private void EnsureRoom(string maPhong, int loaiPhongId, int tang)
        {
            string code = Escape(maPhong);
            Sql("UPDATE PHONG SET LoaiPhongID = " + loaiPhongId + ", Tang = " + tang + " WHERE MaPhong = '" + code + "';");

            Sql(@"INSERT INTO PHONG (MaPhong, LoaiPhongID, Tang, TrangThai, ThoiGianBatDau, KieuThue, TenKhachHienThi)
                  SELECT '" + code + "', " + loaiPhongId + ", " + tang + @", 0, NULL, NULL, NULL
                  FROM DUAL
                  WHERE NOT EXISTS (SELECT 1 FROM PHONG WHERE MaPhong = '" + code + "');");
        }

        private void RemoveDuplicateCodes(string[] roomCodes)
        {
            string inClause = BuildInClause(roomCodes);
            Sql(@"DELETE p1
                  FROM PHONG p1
                  INNER JOIN PHONG p2 ON p1.MaPhong = p2.MaPhong AND p1.PhongID > p2.PhongID
                  WHERE p1.MaPhong IN (" + inClause + ");");
        }

        private void DeleteRoomsOutside(string[] roomCodes)
        {
            string inClause = BuildInClause(roomCodes);
            Sql("DELETE FROM PHONG WHERE MaPhong IS NULL OR MaPhong NOT IN (" + inClause + ");");
        }

        private static string BuildInClause(string[] roomCodes)
        {
            return string.Join(", ", roomCodes.Select(c => "'" + Escape(c) + "'"));
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }
    }
}
