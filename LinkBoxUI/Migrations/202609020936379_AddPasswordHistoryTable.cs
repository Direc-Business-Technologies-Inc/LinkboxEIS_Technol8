namespace LinkBoxUI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPasswordHistoryTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PasswordHistoryModels",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        PasswordHash = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Users", "PasswordChangedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "PasswordChangedAt");
            DropTable("dbo.PasswordHistoryModels");
        }
    }
}
