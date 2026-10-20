namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addColumnPlatformTable : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PlatformTable", "OrderNumber", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PlatformTable", "OrderNumber");
        }
    }
}
