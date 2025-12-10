using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixModeratorIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter ModeratorId column from int to Guid (char(36))
            migrationBuilder.AlterColumn<Guid>(
                name: "ModeratorId",
                table: "GymGroups",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert ModeratorId column from Guid back to int
            migrationBuilder.AlterColumn<int>(
                name: "ModeratorId",
                table: "GymGroups",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "ascii_general_ci");
        }
    }
}
