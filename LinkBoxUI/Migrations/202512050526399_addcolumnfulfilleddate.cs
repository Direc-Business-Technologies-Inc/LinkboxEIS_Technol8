namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addcolumnfulfilleddate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PlatformTable", "FulfilledDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PlatformTable", "FulfilledDate");
        }
    }
}
