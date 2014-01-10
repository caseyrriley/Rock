//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class UserAccounts : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            DropColumn("dbo.UserLogin", "ServiceType");

            AddPage( "0B213645-FA4E-44A5-8E4C-B2D8EF054985", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "User Accounts", "", "306BFEF8-596C-482A-8DEC-34A7B622E688", "fa fa-user" ); // Site:Rock Internal
            AddBlockType( "User Logins", "Block for displaying logins.  By default displays all logins, but can be configured to use person context to display logins for a specific person.", "~/Blocks/Security/UserLogins.ascx", "CE06640D-C1BA-4ACE-AF03-8D733FD3247C" );
            AddBlock( "306BFEF8-596C-482A-8DEC-34A7B622E688", "", "CE06640D-C1BA-4ACE-AF03-8D733FD3247C", "User Logins", "Main", 0, "2788A15A-3392-4750-BDBC-C6CB394D34AA" );

        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DeleteBlock( "2788A15A-3392-4750-BDBC-C6CB394D34AA" );
            DeleteBlockType( "CE06640D-C1BA-4ACE-AF03-8D733FD3247C" ); 
            DeletePage( "306BFEF8-596C-482A-8DEC-34A7B622E688" ); 

            AddColumn("dbo.UserLogin", "ServiceType", c => c.Int(nullable: false));
        }
    }
}