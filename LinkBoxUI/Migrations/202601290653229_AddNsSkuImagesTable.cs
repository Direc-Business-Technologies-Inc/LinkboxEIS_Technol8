namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNsSkuImagesTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FileCabinetImages",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SKUFolder = c.String(nullable: false, maxLength: 50),
                        FileID = c.String(nullable: false, maxLength: 10),
                        Shopify = c.String(maxLength: 10),
                        Zalora = c.String(maxLength: 10),
                        TikTok = c.String(maxLength: 10),
                        Lazada = c.String(maxLength: 10),
                        Shopee = c.String(maxLength: 10),
                        Base64Image = c.String(),
                        CreatedDate = c.DateTime(),
                        status = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.FileCabinetImages");
        }
    }
}
