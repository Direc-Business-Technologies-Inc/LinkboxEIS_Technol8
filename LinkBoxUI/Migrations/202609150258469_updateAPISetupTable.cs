namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updateAPISetupTable : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.APISetups", "AccessTokenExpiry", c => c.DateTime());
            AddColumn("dbo.APISetups", "RefreshTokenExpiry", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.APISetups", "RefreshTokenExpiry");
            DropColumn("dbo.APISetups", "AccessTokenExpiry");
        }
    }
}
