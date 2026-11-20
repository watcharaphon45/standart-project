using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace clean.Infrastructure.Persistence.Migrations.System;

[DbContext(typeof(SystemDbContext))]
[Migration("20261118999999_OrganizationIntegrity")]
public sealed class OrganizationIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TRIGGER company_cost_center_reference BEFORE INSERT OR UPDATE ON "DbCostCenter"
          FOR EACH ROW EXECUTE FUNCTION clean_lock_company_reference();

        CREATE OR REPLACE FUNCTION clean_guard_company() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF OLD."CodeLocked" AND (NOT NEW."CodeLocked" OR NEW."Code" <> OLD."Code") THEN
            RAISE EXCEPTION 'Company code is locked' USING ERRCODE = '23514';
          END IF;
          IF OLD."IsActive" AND NOT NEW."IsActive" AND (
            EXISTS (SELECT 1 FROM "DbBranch" WHERE "CompanyId" = NEW."Id" AND "IsActive") OR
            EXISTS (SELECT 1 FROM "DbCostCenter" WHERE "CompanyId" = NEW."Id" AND "IsActive") OR
            EXISTS (SELECT 1 FROM "SuCompanyAssignment" WHERE "CompanyId" = NEW."Id" AND "IsActive")
          ) THEN RAISE EXCEPTION 'Company has active references' USING ERRCODE = '23514'; END IF;
          RETURN NEW;
        END $$;

        CREATE FUNCTION clean_lock_branch_from_division() RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE active boolean;
        DECLARE branch_company bigint;
        BEGIN
          SELECT "IsActive", "CompanyId" INTO active, branch_company FROM "DbBranch" WHERE "Id" = NEW."BranchId" FOR UPDATE;
          IF NOT FOUND OR branch_company <> NEW."CompanyId" THEN
            RAISE EXCEPTION 'Branch does not match company' USING ERRCODE = '23514';
          END IF;
          IF NEW."IsActive" AND NOT active THEN
            RAISE EXCEPTION 'Branch is inactive' USING ERRCODE = '23514';
          END IF;
          UPDATE "DbBranch" SET "CodeLocked" = true, "Version" = "Version" + 1
            WHERE "Id" = NEW."BranchId" AND NOT "CodeLocked";
          RETURN NEW;
        END $$;
        CREATE TRIGGER division_branch_reference BEFORE INSERT OR UPDATE ON "DbDivision"
          FOR EACH ROW EXECUTE FUNCTION clean_lock_branch_from_division();

        CREATE FUNCTION clean_lock_division_from_department() RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE active boolean;
        DECLARE division_company bigint;
        BEGIN
          SELECT "IsActive", "CompanyId" INTO active, division_company FROM "DbDivision" WHERE "Id" = NEW."DivisionId" FOR UPDATE;
          IF NOT FOUND OR division_company <> NEW."CompanyId" THEN
            RAISE EXCEPTION 'Division does not match company' USING ERRCODE = '23514';
          END IF;
          IF NEW."IsActive" AND NOT active THEN
            RAISE EXCEPTION 'Division is inactive' USING ERRCODE = '23514';
          END IF;
          UPDATE "DbDivision" SET "CodeLocked" = true, "Version" = "Version" + 1
            WHERE "Id" = NEW."DivisionId" AND NOT "CodeLocked";
          RETURN NEW;
        END $$;
        CREATE TRIGGER department_division_reference BEFORE INSERT OR UPDATE ON "DbDepartment"
          FOR EACH ROW EXECUTE FUNCTION clean_lock_division_from_department();

        CREATE FUNCTION clean_lock_scope_from_assignment() RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE active boolean;
        DECLARE ref_company bigint;
        BEGIN
          IF NEW."ScopeType" = 'Branch' AND NEW."ScopeId" IS NOT NULL THEN
            SELECT "IsActive", "CompanyId" INTO active, ref_company FROM "DbBranch" WHERE "Id" = NEW."ScopeId" FOR UPDATE;
            IF NOT FOUND OR ref_company <> NEW."CompanyId" THEN
              RAISE EXCEPTION 'Branch scope does not match company' USING ERRCODE = '23514';
            END IF;
            IF NEW."IsActive" AND NOT active THEN
              RAISE EXCEPTION 'Branch is inactive' USING ERRCODE = '23514';
            END IF;
            UPDATE "DbBranch" SET "CodeLocked" = true, "Version" = "Version" + 1
              WHERE "Id" = NEW."ScopeId" AND NOT "CodeLocked";
          ELSIF NEW."ScopeType" = 'Department' AND NEW."ScopeId" IS NOT NULL THEN
            SELECT "IsActive", "CompanyId" INTO active, ref_company FROM "DbDepartment" WHERE "Id" = NEW."ScopeId" FOR UPDATE;
            IF NOT FOUND OR ref_company <> NEW."CompanyId" THEN
              RAISE EXCEPTION 'Department scope does not match company' USING ERRCODE = '23514';
            END IF;
            IF NEW."IsActive" AND NOT active THEN
              RAISE EXCEPTION 'Department is inactive' USING ERRCODE = '23514';
            END IF;
            UPDATE "DbDepartment" SET "CodeLocked" = true, "Version" = "Version" + 1
              WHERE "Id" = NEW."ScopeId" AND NOT "CodeLocked";
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER assignment_scope_reference BEFORE INSERT OR UPDATE ON "SuCompanyAssignment"
          FOR EACH ROW EXECUTE FUNCTION clean_lock_scope_from_assignment();

        CREATE FUNCTION clean_guard_branch() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF OLD."CodeLocked" AND (NOT NEW."CodeLocked" OR NEW."Code" <> OLD."Code") THEN
            RAISE EXCEPTION 'Branch code is locked' USING ERRCODE = '23514';
          END IF;
          IF OLD."IsActive" AND NOT NEW."IsActive" AND (
            EXISTS (SELECT 1 FROM "DbDivision" WHERE "BranchId" = NEW."Id" AND "IsActive") OR
            EXISTS (SELECT 1 FROM "SuCompanyAssignment" WHERE "CompanyId" = NEW."CompanyId" AND "ScopeType" = 'Branch' AND "ScopeId" = NEW."Id" AND "IsActive")
          ) THEN RAISE EXCEPTION 'Branch has active references' USING ERRCODE = '23514'; END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER branch_guard BEFORE UPDATE ON "DbBranch"
          FOR EACH ROW EXECUTE FUNCTION clean_guard_branch();

        CREATE FUNCTION clean_guard_division() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF OLD."CodeLocked" AND (NOT NEW."CodeLocked" OR NEW."Code" <> OLD."Code" OR NEW."BranchId" <> OLD."BranchId") THEN
            RAISE EXCEPTION 'Division code or parent is locked' USING ERRCODE = '23514';
          END IF;
          IF OLD."IsActive" AND NOT NEW."IsActive" AND (
            EXISTS (SELECT 1 FROM "DbDepartment" WHERE "DivisionId" = NEW."Id" AND "IsActive")
          ) THEN RAISE EXCEPTION 'Division has active references' USING ERRCODE = '23514'; END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER division_guard BEFORE UPDATE ON "DbDivision"
          FOR EACH ROW EXECUTE FUNCTION clean_guard_division();

        CREATE FUNCTION clean_guard_department() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          IF OLD."CodeLocked" AND (NOT NEW."CodeLocked" OR NEW."Code" <> OLD."Code" OR NEW."DivisionId" <> OLD."DivisionId") THEN
            RAISE EXCEPTION 'Department code or parent is locked' USING ERRCODE = '23514';
          END IF;
          IF OLD."IsActive" AND NOT NEW."IsActive" AND (
            EXISTS (SELECT 1 FROM "SuCompanyAssignment" WHERE "CompanyId" = NEW."CompanyId" AND "ScopeType" = 'Department' AND "ScopeId" = NEW."Id" AND "IsActive")
          ) THEN RAISE EXCEPTION 'Department has active references' USING ERRCODE = '23514'; END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER department_guard BEFORE UPDATE ON "DbDepartment"
          FOR EACH ROW EXECUTE FUNCTION clean_guard_department();
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TRIGGER department_guard ON "DbDepartment";
        DROP FUNCTION clean_guard_department();
        DROP TRIGGER division_guard ON "DbDivision";
        DROP FUNCTION clean_guard_division();
        DROP TRIGGER branch_guard ON "DbBranch";
        DROP FUNCTION clean_guard_branch();
        DROP TRIGGER assignment_scope_reference ON "SuCompanyAssignment";
        DROP FUNCTION clean_lock_scope_from_assignment();
        DROP TRIGGER department_division_reference ON "DbDepartment";
        DROP FUNCTION clean_lock_division_from_department();
        DROP TRIGGER division_branch_reference ON "DbDivision";
        DROP FUNCTION clean_lock_branch_from_division();
        DROP TRIGGER company_cost_center_reference ON "DbCostCenter";
        CREATE OR REPLACE FUNCTION clean_guard_company() RETURNS trigger LANGUAGE plpgsql AS $$
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
        """);
}

