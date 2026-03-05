using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureMobileNumberColumnExists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF COL_LENGTH('Users', 'MobileNumber') IS NULL
                  BEGIN
                      ALTER TABLE [Users] ADD [MobileNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_Users_MobileNumber] DEFAULT '';
                  END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"IF COL_LENGTH('Users', 'MobileNumber') IS NOT NULL
                  BEGIN
                      ALTER TABLE [Users] DROP CONSTRAINT [DF_Users_MobileNumber];
                      ALTER TABLE [Users] DROP COLUMN [MobileNumber];
                  END");
        }
    }
}
