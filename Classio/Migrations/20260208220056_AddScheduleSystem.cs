using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classio.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsBreak = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    ClassPeriodId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    GoogleEventId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleSlots_ClassPeriods_ClassPeriodId",
                        column: x => x.ClassPeriodId,
                        principalTable: "ClassPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleSlots_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleSlots_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleSlots_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_ClassId",
                table: "ScheduleSlots",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_ClassPeriodId",
                table: "ScheduleSlots",
                column: "ClassPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_SubjectId",
                table: "ScheduleSlots",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_TeacherId",
                table: "ScheduleSlots",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleSlots");

            migrationBuilder.DropTable(
                name: "ClassPeriods");
        }
    }
}
