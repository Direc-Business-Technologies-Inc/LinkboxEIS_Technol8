namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatecolumnFulfillment : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.PlatformTable", "Fulfillment", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.PlatformTable", "Fulfillment", c => c.Boolean());
        }
    }
}
