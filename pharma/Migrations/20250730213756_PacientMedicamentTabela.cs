using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharma.Migrations
{
    /// <inheritdoc />
    public partial class PacientMedicamentTabela : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicamentPacient");

            migrationBuilder.CreateTable(
                name: "PacientMedicamente",
                columns: table => new
                {
                    PacientId = table.Column<int>(type: "int", nullable: false),
                    MedicamentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacientMedicamente", x => new { x.PacientId, x.MedicamentId });
                    table.ForeignKey(
                        name: "FK_PacientMedicamente_Medicamente_MedicamentId",
                        column: x => x.MedicamentId,
                        principalTable: "Medicamente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PacientMedicamente_Pacienti_PacientId",
                        column: x => x.PacientId,
                        principalTable: "Pacienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PacientMedicamente_MedicamentId",
                table: "PacientMedicamente",
                column: "MedicamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PacientMedicamente");

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
    }
}
