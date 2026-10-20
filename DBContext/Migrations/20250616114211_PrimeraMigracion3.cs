using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBContext.Migrations
{
    /// <inheritdoc />
    public partial class PrimeraMigracion3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgrammedDateToStart",
                table: "ProjectTask");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TaskResource",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TaskResource");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProgrammedDateToStart",
                table: "ProjectTask",
                type: "datetime2",
                nullable: true);
        }
    }
}
