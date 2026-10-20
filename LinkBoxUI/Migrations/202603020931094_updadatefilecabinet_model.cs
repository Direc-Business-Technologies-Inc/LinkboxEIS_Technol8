namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updadatefilecabinet_model : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FileCabinetImages", "Platform", c => c.String());
            AddColumn("dbo.FileCabinetImages", "ImageID", c => c.String());
            DropColumn("dbo.FileCabinetImages", "Shopify");
            DropColumn("dbo.FileCabinetImages", "Zalora");
            DropColumn("dbo.FileCabinetImages", "TikTok");
            DropColumn("dbo.FileCabinetImages", "Lazada");
            DropColumn("dbo.FileCabinetImages", "Shopee");
        }
        
        public override void Down()
        {
            AddColumn("dbo.FileCabinetImages", "Shopee", c => c.String());
            AddColumn("dbo.FileCabinetImages", "Lazada", c => c.String());
            AddColumn("dbo.FileCabinetImages", "TikTok", c => c.String());
            AddColumn("dbo.FileCabinetImages", "Zalora", c => c.String());
            AddColumn("dbo.FileCabinetImages", "Shopify", c => c.String());
            DropColumn("dbo.FileCabinetImages", "ImageID");
            DropColumn("dbo.FileCabinetImages", "Platform");
        }
    }
}
