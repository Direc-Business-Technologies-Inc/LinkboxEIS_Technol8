namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExtractedModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EISExtractedDocuments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MapId = c.Int(nullable: false),
                        Module = c.String(),
                        DocEntry = c.Int(nullable: false),
                        EisUniqueId = c.String(),
                        Json = c.String(),
                        Status = c.Byte(nullable: false),
                        ErrorMessage = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        UpdatedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.EISExtractedDocuments");
        }
    }
}
