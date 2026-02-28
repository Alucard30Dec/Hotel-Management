namespace Hotel_Management.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemoveRoomGhiChu : DbMigration
    {
        public override void Up()
        {
            Sql("ALTER TABLE `PHONG` DROP COLUMN IF EXISTS `GhiChu`;");
        }

        public override void Down()
        {
            Sql("ALTER TABLE `PHONG` ADD COLUMN IF NOT EXISTS `GhiChu` LONGTEXT NULL;");
        }
    }
}
