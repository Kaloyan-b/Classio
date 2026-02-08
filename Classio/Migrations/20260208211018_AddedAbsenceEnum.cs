using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classio.Migrations
{
    /// <inheritdoc />
    public partial class AddedAbsenceEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExcused",
                table: "Absences");

            migrationBuilder.AddColumn<int>(
                name: "AttendanceState",
                table: "Absences",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceState",
                table: "Absences");

            migrationBuilder.AddColumn<bool>(
                name: "IsExcused",
                table: "Absences",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
