namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddQueryCode_FM : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FieldMappings", "QueryCode", c => c.String(maxLength: 30));
        }
        
        public override void Down()
        {
            DropColumn("dbo.FieldMappings", "QueryCode");
        }
    }
}
