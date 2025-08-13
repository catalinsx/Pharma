using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharma.Migrations
{
    /// <inheritdoc />
    public partial class PacientMedicament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicamentPacient",
                columns: table => new
                {
                    MedicamenteId = table.Column<int>(type: "int", nullable: false),
                    PacientiId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicamentPacient", x => new { x.MedicamenteId, x.PacientiId });
                    table.ForeignKey(
                        name: "FK_MedicamentPacient_Medicamente_MedicamenteId",
                        column: x => x.MedicamenteId,
                        principalTable: "Medicamente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicamentPacient_Pacienti_PacientiId",
                        column: x => x.PacientiId,
                        principalTable: "Pacienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicamentPacient_PacientiId",
                table: "MedicamentPacient",
                column: "PacientiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicamentPacient");
        }
    }
}
