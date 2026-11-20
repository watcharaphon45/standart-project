using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace clean.Infrastructure.Persistence.Migrations.System;

[DbContext(typeof(SystemDbContext))]
[Migration("20261116999999_CompanyIntegrity")]
public sealed class CompanyIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE "SuRole" ADD CONSTRAINT "FK_SuRole_Company"
          FOREIGN KEY ("CompanyId") REFERENCES "DbCompany" ("Id") ON DELETE RESTRICT;
        ALTER TABLE "SuCompanyAssignment" ADD CONSTRAINT "FK_Assignment_Company"
          FOREIGN KEY ("CompanyId") REFERENCES "DbCompany" ("Id") ON DELETE RESTRICT;
        ALTER TABLE "SuCompanyAssignment" ADD CONSTRAINT "FK_Assignment_CompanyRole"
          FOREIGN KEY ("RoleId", "CompanyId") REFERENCES "SuRole" ("Id", "CompanyId") ON DELETE RESTRICT;
        CREATE FUNCTION clean_lock_company_reference() RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE active boolean;
        BEGIN
          IF NEW."CompanyId" IS NULL THEN RETURN NEW; END IF;
          SELECT "IsActive" INTO active FROM "DbCompany" WHERE "Id" = NEW."CompanyId" FOR UPDATE;
          IF NEW."IsActive" AND NOT active THEN
            RAISE EXCEPTION 'Company is inactive' USING ERRCODE = '23514';
          END IF;
          UPDATE "DbCompany" SET "CodeLocked" = true, "Version" = "Version" + 1
            WHERE "Id" = NEW."CompanyId" AND NOT "CodeLocked";
          RETURN NEW;
        END $$;
        CREATE TRIGGER company_branch_reference BEFORE INSERT OR UPDATE ON "DbBranch"
          FOR EACH ROW EXECUTE FUNCTION clean_lock_company_reference();
        CREATE TRIGGER company_role_reference BEFORE INSERT OR UPDATE ON "SuRole"
          FOR EACH ROW EXECUTE FUNCTION clean_lock_company_reference();
        CREATE TRIGGER company_assignment_reference BEFORE INSERT OR UPDATE ON "SuCompanyAssignment"
          FOR EACH ROW EXECUTE FUNCTION clean_lock_company_reference();
        CREATE FUNCTION clean_guard_company() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF OLD."CodeLocked" AND (NOT NEW."CodeLocked" OR NEW."Code" <> OLD."Code") THEN
            RAISE EXCEPTION 'Company code is locked' USING ERRCODE = '23514';
          END IF;
          IF OLD."IsActive" AND NOT NEW."IsActive" AND (
            EXISTS (SELECT 1 FROM "DbBranch" WHERE "CompanyId" = NEW."Id" AND "IsActive") OR
            EXISTS (SELECT 1 FROM "SuCompanyAssignment" WHERE "CompanyId" = NEW."Id" AND "IsActive")
          ) THEN RAISE EXCEPTION 'Company has active references' USING ERRCODE = '23514'; END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER company_guard BEFORE UPDATE ON "DbCompany"
          FOR EACH ROW EXECUTE FUNCTION clean_guard_company();
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TRIGGER company_guard ON "DbCompany";
        DROP FUNCTION clean_guard_company();
        DROP TRIGGER company_assignment_reference ON "SuCompanyAssignment";
        DROP TRIGGER company_role_reference ON "SuRole";
        DROP TRIGGER company_branch_reference ON "DbBranch";
        DROP FUNCTION clean_lock_company_reference();
        ALTER TABLE "SuCompanyAssignment" DROP CONSTRAINT "FK_Assignment_CompanyRole";
        ALTER TABLE "SuCompanyAssignment" DROP CONSTRAINT "FK_Assignment_Company";
        ALTER TABLE "SuRole" DROP CONSTRAINT "FK_SuRole_Company";
        """);
}
