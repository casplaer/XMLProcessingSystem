using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataProcessorService.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Modules",
                table: "Modules");

            migrationBuilder.RenameTable(
                name: "Modules",
                newName: "modules");

            migrationBuilder.RenameColumn(
                name: "ModuleState",
                table: "modules",
                newName: "module_state");

            migrationBuilder.RenameColumn(
                name: "ModuleCategoryID",
                table: "modules",
                newName: "module_category_id");

            migrationBuilder.AddColumn<int>(
                name: "index_within_role",
                table: "modules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "package_id",
                table: "modules",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_modules",
                table: "modules",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_modules_package_id_module_category_id_index_within_role",
                table: "modules",
                columns: new[] { "package_id", "module_category_id", "index_within_role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_modules",
                table: "modules");

            migrationBuilder.DropIndex(
                name: "IX_modules_package_id_module_category_id_index_within_role",
                table: "modules");

            migrationBuilder.DropColumn(
                name: "index_within_role",
                table: "modules");

            migrationBuilder.DropColumn(
                name: "package_id",
                table: "modules");

            migrationBuilder.RenameTable(
                name: "modules",
                newName: "Modules");

            migrationBuilder.RenameColumn(
                name: "module_state",
                table: "Modules",
                newName: "ModuleState");

            migrationBuilder.RenameColumn(
                name: "module_category_id",
                table: "Modules",
                newName: "ModuleCategoryID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Modules",
                table: "Modules",
                column: "Id");
        }
    }
}
