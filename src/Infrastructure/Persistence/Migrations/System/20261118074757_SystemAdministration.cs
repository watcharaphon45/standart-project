using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clean.Infrastructure.Persistence.Migrations.System
{
    /// <inheritdoc />
    public partial class SystemAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "SuRole");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SuUser",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<bool>(
                name: "CodeLocked",
                table: "SuRole",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SuRole",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SuRole",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SuCompanyAssignment",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateIndex(
                name: "IX_SuRole_CompanyId_NormalizedName",
                table: "SuRole",
                columns: new[] { "CompanyId", "NormalizedName" },
                unique: true,
                filter: " \"CompanyId\" IS NOT NULL ");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "SuRole",
                column: "NormalizedName",
                unique: true,
                filter: " \"CompanyId\" IS NULL ");

            migrationBuilder.Sql("""
                UPDATE "SuRole" r SET "CodeLocked" = true
                  WHERE EXISTS (SELECT 1 FROM "SuCompanyAssignment" a WHERE a."RoleId" = r."Id")
                     OR EXISTS (SELECT 1 FROM "SuUserRole" ur WHERE ur."RoleId" = r."Id");

                CREATE FUNCTION clean_lock_role_from_assignment() RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE active boolean;
                DECLARE role_company bigint;
                BEGIN
                  SELECT "IsActive", "CompanyId" INTO active, role_company FROM "SuRole" WHERE "Id" = NEW."RoleId" FOR UPDATE;
                  IF NOT FOUND OR role_company IS DISTINCT FROM NEW."CompanyId" THEN
                    RAISE EXCEPTION 'Role does not match company' USING ERRCODE = '23514';
                  END IF;
                  IF NEW."IsActive" AND NOT active THEN
                    RAISE EXCEPTION 'Role is inactive' USING ERRCODE = '23514';
                  END IF;
                  UPDATE "SuRole" SET "CodeLocked" = true, "Version" = "Version" + 1
                    WHERE "Id" = NEW."RoleId" AND NOT "CodeLocked";
                  RETURN NEW;
                END $$;
                CREATE TRIGGER role_assignment_reference BEFORE INSERT OR UPDATE ON "SuCompanyAssignment"
                  FOR EACH ROW EXECUTE FUNCTION clean_lock_role_from_assignment();

                CREATE FUNCTION clean_lock_role_from_user_role() RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE active boolean;
                DECLARE role_company bigint;
                BEGIN
                  SELECT "IsActive", "CompanyId" INTO active, role_company FROM "SuRole" WHERE "Id" = NEW."RoleId" FOR UPDATE;
                  IF NOT FOUND OR role_company IS NOT NULL THEN
                    RAISE EXCEPTION 'User role must reference a global role' USING ERRCODE = '23514';
                  END IF;
                  IF NOT active THEN
                    RAISE EXCEPTION 'Role is inactive' USING ERRCODE = '23514';
                  END IF;
                  UPDATE "SuRole" SET "CodeLocked" = true, "Version" = "Version" + 1
                    WHERE "Id" = NEW."RoleId" AND NOT "CodeLocked";
                  RETURN NEW;
                END $$;
                CREATE TRIGGER role_user_role_reference BEFORE INSERT OR UPDATE ON "SuUserRole"
                  FOR EACH ROW EXECUTE FUNCTION clean_lock_role_from_user_role();

                CREATE FUNCTION clean_guard_role() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  IF OLD."CodeLocked" AND (NOT NEW."CodeLocked" OR NEW."NormalizedName" IS DISTINCT FROM OLD."NormalizedName") THEN
                    RAISE EXCEPTION 'Role code is locked' USING ERRCODE = '23514';
                  END IF;
                  IF NEW."CompanyId" IS DISTINCT FROM OLD."CompanyId" THEN
                    RAISE EXCEPTION 'Role company cannot change' USING ERRCODE = '23514';
                  END IF;
                  IF OLD."IsActive" AND NOT NEW."IsActive" AND (
                    EXISTS (SELECT 1 FROM "SuCompanyAssignment" WHERE "RoleId" = NEW."Id" AND "IsActive") OR
                    EXISTS (SELECT 1 FROM "SuUserRole" WHERE "RoleId" = NEW."Id")
                  ) THEN
                    RAISE EXCEPTION 'Role has active assignments' USING ERRCODE = '23514';
                  END IF;
                  RETURN NEW;
                END $$;
                CREATE TRIGGER role_guard BEFORE UPDATE ON "SuRole"
                  FOR EACH ROW EXECUTE FUNCTION clean_guard_role();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER role_guard ON "SuRole";
                DROP FUNCTION clean_guard_role();
                DROP TRIGGER role_user_role_reference ON "SuUserRole";
                DROP FUNCTION clean_lock_role_from_user_role();
                DROP TRIGGER role_assignment_reference ON "SuCompanyAssignment";
                DROP FUNCTION clean_lock_role_from_assignment();
                """);

            migrationBuilder.DropIndex(
                name: "IX_SuRole_CompanyId_NormalizedName",
                table: "SuRole");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "SuRole");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SuUser");

            migrationBuilder.DropColumn(
                name: "CodeLocked",
                table: "SuRole");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SuRole");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SuRole");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SuCompanyAssignment");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "SuRole",
                column: "NormalizedName",
                unique: true);
        }
    }
}
