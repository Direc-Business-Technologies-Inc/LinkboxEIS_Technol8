namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatefilecabinettablev3 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.FileCabinetImages", "Shopify", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.FileCabinetImages", "Shopify", c => c.String(maxLength: 10));
        }
    }
}
