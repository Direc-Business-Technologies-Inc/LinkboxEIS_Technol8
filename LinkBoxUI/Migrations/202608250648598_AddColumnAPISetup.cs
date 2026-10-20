namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddColumnAPISetup : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.APISetups", "Platform", c => c.String());
            AddColumn("dbo.APISetups", "AuthCode", c => c.String());
            AddColumn("dbo.APISetups", "AccessToken", c => c.String());
            AddColumn("dbo.APISetups", "RefreshToken", c => c.String());
            AddColumn("dbo.APISetups", "ShopCode", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.APISetups", "ShopCode");
            DropColumn("dbo.APISetups", "RefreshToken");
            DropColumn("dbo.APISetups", "AccessToken");
            DropColumn("dbo.APISetups", "AuthCode");
            DropColumn("dbo.APISetups", "Platform");
        }
    }
}
