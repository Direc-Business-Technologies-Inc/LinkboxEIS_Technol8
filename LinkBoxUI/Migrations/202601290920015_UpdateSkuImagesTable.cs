namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class UpdateSkuImagesTable : DbMigration
    {
        public override void Up()
        {
            // Drop table (destructive)
            DropTable("dbo.FileCabinetImages");

            // Recreate table
            CreateTable(
                "dbo.FileCabinetImages",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),

                    SKUFolder = c.String(nullable: false, maxLength: 50),
                    FileID = c.String(nullable: false, maxLength: 50),

                    Shopify = c.String(maxLength: 10),
                    Zalora = c.String(maxLength: 10),
                    TikTok = c.String(maxLength: 10),
                    Lazada = c.String(maxLength: 10),
                    Shopee = c.String(maxLength: 10),

                    ImageName = c.String(maxLength: 255),
                    Type = c.String(maxLength: 20),
                    Base64Image = c.String(),

                    Status = c.String(maxLength: 20),
                    CreatedDate = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.FileID, unique: true, name: "IX_FileCabinetImages_FileID");
        }

        public override void Down()
        {
            DropIndex("dbo.FileCabinetImages", "IX_FileCabinetImages_FileID");
            DropTable("dbo.FileCabinetImages");
        }
    }
}
