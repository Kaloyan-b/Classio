using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Classio.Migrations
{
    /// <inheritdoc />
    public partial class TeacherClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Teachers_TeacherId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_TeacherId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Classes");

            migrationBuilder.AddColumn<int>(
                name: "HeadTeacherId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassTeachers",
                columns: table => new
                {
                    ClassesTaughtId = table.Column<int>(type: "int", nullable: false),
                    SubjectTeachersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassTeachers", x => new { x.ClassesTaughtId, x.SubjectTeachersId });
                    table.ForeignKey(
                        name: "FK_ClassTeachers_Classes_ClassesTaughtId",
                        column: x => x.ClassesTaughtId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassTeachers_Teachers_SubjectTeachersId",
                        column: x => x.SubjectTeachersId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_HeadTeacherId",
                table: "Classes",
                column: "HeadTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeachers_SubjectTeachersId",
                table: "ClassTeachers",
                column: "SubjectTeachersId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Teachers_HeadTeacherId",
                table: "Classes",
                column: "HeadTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Teachers_HeadTeacherId",
                table: "Classes");

            migrationBuilder.DropTable(
                name: "ClassTeachers");

            migrationBuilder.DropIndex(
                name: "IX_Classes_HeadTeacherId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "HeadTeacherId",
                table: "Classes");

            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "Classes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TeacherId",
                table: "Classes",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Teachers_TeacherId",
                table: "Classes",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
