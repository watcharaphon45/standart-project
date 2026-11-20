using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace clean.Infrastructure.Persistence.Migrations.System
{
    /// <inheritdoc />
    public partial class SystemOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuApprovalWorkflow",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuApprovalWorkflow", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuDocumentSequence",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ResetPolicy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaddingLength = table.Column<int>(type: "integer", nullable: false),
                    CurrentPeriod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LastNumber = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuDocumentSequence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuMenuItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ModuleCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProgramCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Route = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredPermission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuMenuItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuSystemSetting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuSystemSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuApprovalWorkflowStep",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    WorkflowId = table.Column<long>(type: "bigint", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    StepName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApproverRoleId = table.Column<long>(type: "bigint", nullable: false),
                    RequireDifferentActor = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuApprovalWorkflowStep", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuApprovalWorkflowStep_SuApprovalWorkflow_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "SuApprovalWorkflow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuApprovalWorkflowStep_SuRole_ApproverRoleId",
                        column: x => x.ApproverRoleId,
                        principalTable: "SuRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuApprovalWorkflow_CompanyId_Code",
                table: "SuApprovalWorkflow",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuApprovalWorkflowStep_ApproverRoleId",
                table: "SuApprovalWorkflowStep",
                column: "ApproverRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SuApprovalWorkflowStep_WorkflowId_StepOrder",
                table: "SuApprovalWorkflowStep",
                columns: new[] { "WorkflowId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuDocumentSequence_CompanyId_Code",
                table: "SuDocumentSequence",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuMenuItem_ProgramCode",
                table: "SuMenuItem",
                column: "ProgramCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuSystemSetting_CompanyId_Key",
                table: "SuSystemSetting",
                columns: new[] { "CompanyId", "Key" },
                unique: true,
                filter: " \"CompanyId\" IS NOT NULL ");

            migrationBuilder.CreateIndex(
                name: "IX_SuSystemSetting_Global_Key",
                table: "SuSystemSetting",
                column: "Key",
                unique: true,
                filter: " \"CompanyId\" IS NULL ");

            migrationBuilder.Sql("""
                ALTER TABLE "SuDocumentSequence" ADD CONSTRAINT "FK_DocumentSequence_Company"
                  FOREIGN KEY ("CompanyId") REFERENCES "DbCompany" ("Id") ON DELETE RESTRICT;
                ALTER TABLE "SuApprovalWorkflow" ADD CONSTRAINT "FK_ApprovalWorkflow_Company"
                  FOREIGN KEY ("CompanyId") REFERENCES "DbCompany" ("Id") ON DELETE RESTRICT;
                ALTER TABLE "SuSystemSetting" ADD CONSTRAINT "FK_SystemSetting_Company"
                  FOREIGN KEY ("CompanyId") REFERENCES "DbCompany" ("Id") ON DELETE RESTRICT;

                CREATE TRIGGER company_doc_sequence_reference BEFORE INSERT OR UPDATE ON "SuDocumentSequence"
                  FOR EACH ROW EXECUTE FUNCTION clean_lock_company_reference();
                CREATE TRIGGER company_workflow_reference BEFORE INSERT OR UPDATE ON "SuApprovalWorkflow"
                  FOR EACH ROW EXECUTE FUNCTION clean_lock_company_reference();

                CREATE FUNCTION clean_lock_role_from_workflow_step() RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE active boolean;
                DECLARE role_company bigint;
                DECLARE wf_company bigint;
                DECLARE wf_active boolean;
                BEGIN
                  SELECT "CompanyId", "IsActive" INTO wf_company, wf_active FROM "SuApprovalWorkflow" WHERE "Id" = NEW."WorkflowId";
                  SELECT "IsActive", "CompanyId" INTO active, role_company FROM "SuRole" WHERE "Id" = NEW."ApproverRoleId" FOR UPDATE;
                  IF NOT FOUND OR role_company IS DISTINCT FROM wf_company THEN
                    RAISE EXCEPTION 'Workflow approver role does not match company' USING ERRCODE = '23514';
                  END IF;
                  IF wf_active AND NOT active THEN
                    RAISE EXCEPTION 'Approver role is inactive' USING ERRCODE = '23514';
                  END IF;
                  UPDATE "SuRole" SET "CodeLocked" = true, "Version" = "Version" + 1
                    WHERE "Id" = NEW."ApproverRoleId" AND NOT "CodeLocked";
                  RETURN NEW;
                END $$;
                CREATE TRIGGER workflow_step_role_reference BEFORE INSERT OR UPDATE ON "SuApprovalWorkflowStep"
                  FOR EACH ROW EXECUTE FUNCTION clean_lock_role_from_workflow_step();

                CREATE OR REPLACE FUNCTION clean_guard_company() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  IF OLD."CodeLocked" AND (NOT NEW."CodeLocked" OR NEW."Code" <> OLD."Code") THEN
                    RAISE EXCEPTION 'Company code is locked' USING ERRCODE = '23514';
                  END IF;
                  IF OLD."IsActive" AND NOT NEW."IsActive" AND (
                    EXISTS (SELECT 1 FROM "DbBranch" WHERE "CompanyId" = NEW."Id" AND "IsActive") OR
                    EXISTS (SELECT 1 FROM "DbCostCenter" WHERE "CompanyId" = NEW."Id" AND "IsActive") OR
                    EXISTS (SELECT 1 FROM "SuCompanyAssignment" WHERE "CompanyId" = NEW."Id" AND "IsActive") OR
                    EXISTS (SELECT 1 FROM "SuDocumentSequence" WHERE "CompanyId" = NEW."Id" AND "IsActive") OR
                    EXISTS (SELECT 1 FROM "SuApprovalWorkflow" WHERE "CompanyId" = NEW."Id" AND "IsActive")
                  ) THEN RAISE EXCEPTION 'Company has active references' USING ERRCODE = '23514'; END IF;
                  RETURN NEW;
                END $$;

                CREATE OR REPLACE FUNCTION clean_guard_role() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  IF OLD."CodeLocked" AND (NOT NEW."CodeLocked" OR NEW."NormalizedName" IS DISTINCT FROM OLD."NormalizedName") THEN
                    RAISE EXCEPTION 'Role code is locked' USING ERRCODE = '23514';
                  END IF;
                  IF NEW."CompanyId" IS DISTINCT FROM OLD."CompanyId" THEN
                    RAISE EXCEPTION 'Role company cannot change' USING ERRCODE = '23514';
                  END IF;
                  IF OLD."IsActive" AND NOT NEW."IsActive" AND (
                    EXISTS (SELECT 1 FROM "SuCompanyAssignment" WHERE "RoleId" = NEW."Id" AND "IsActive") OR
                    EXISTS (SELECT 1 FROM "SuUserRole" WHERE "RoleId" = NEW."Id") OR
                    EXISTS (
                      SELECT 1 FROM "SuApprovalWorkflowStep" s
                      JOIN "SuApprovalWorkflow" w ON w."Id" = s."WorkflowId"
                      WHERE s."ApproverRoleId" = NEW."Id" AND w."IsActive"
                    )
                  ) THEN
                    RAISE EXCEPTION 'Role has active assignments' USING ERRCODE = '23514';
                  END IF;
                  RETURN NEW;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER workflow_step_role_reference ON "SuApprovalWorkflowStep";
                DROP FUNCTION clean_lock_role_from_workflow_step();
                DROP TRIGGER company_workflow_reference ON "SuApprovalWorkflow";
                DROP TRIGGER company_doc_sequence_reference ON "SuDocumentSequence";
                ALTER TABLE "SuSystemSetting" DROP CONSTRAINT "FK_SystemSetting_Company";
                ALTER TABLE "SuApprovalWorkflow" DROP CONSTRAINT "FK_ApprovalWorkflow_Company";
                ALTER TABLE "SuDocumentSequence" DROP CONSTRAINT "FK_DocumentSequence_Company";
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
                CREATE OR REPLACE FUNCTION clean_guard_role() RETURNS trigger LANGUAGE plpgsql AS $$
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
                """);

            migrationBuilder.DropTable(
                name: "SuApprovalWorkflowStep");

            migrationBuilder.DropTable(
                name: "SuDocumentSequence");

            migrationBuilder.DropTable(
                name: "SuMenuItem");

            migrationBuilder.DropTable(
                name: "SuSystemSetting");

            migrationBuilder.DropTable(
                name: "SuApprovalWorkflow");
        }
    }
}
