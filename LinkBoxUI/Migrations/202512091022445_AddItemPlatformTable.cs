namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddItemPlatformTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PlatformItemTables",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ReferenceId = c.String(nullable: false, maxLength: 50),
                        Platform = c.String(nullable: false, maxLength: 10),
                        ItemId = c.String(),
                        variantId = c.String(),
                        BuildJson = c.String(),
                        PlatformJson = c.String(),
                        CreatedDate = c.DateTime(),
                        UploadDate = c.DateTime(),
                        Message = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.PlatformItemTables");
        }
    }
}
