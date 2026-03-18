using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMD.PERSISTENCE.Migrations
{
    /// <inheritdoc />
    public partial class _102 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClinicProfileId",
                table: "PatientInfos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PatientInfos_ClinicProfileId",
                table: "PatientInfos",
                column: "ClinicProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientInfos_ClinicProfiles_ClinicProfileId",
                table: "PatientInfos",
                column: "ClinicProfileId",
                principalTable: "ClinicProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientInfos_ClinicProfiles_ClinicProfileId",
                table: "PatientInfos");

            migrationBuilder.DropIndex(
                name: "IX_PatientInfos_ClinicProfileId",
                table: "PatientInfos");

            migrationBuilder.DropColumn(
                name: "ClinicProfileId",
                table: "PatientInfos");
        }
    }
}
