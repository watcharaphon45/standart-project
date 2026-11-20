using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace clean.Infrastructure.Persistence.Migrations.Organization
{
    /// <inheritdoc />
    public partial class OrganizationMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CodeLocked",
                table: "DbBranch",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "DbBranch",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "CreatedBy",
                table: "DbBranch",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "DbBranch",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "UpdatedBy",
                table: "DbBranch",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "DbBranch",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DbBranch_Id_CompanyId",
                table: "DbBranch",
                columns: new[] { "Id", "CompanyId" });

            migrationBuilder.CreateTable(
                name: "DbBusinessUnit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbBusinessUnit", x => x.Id);
                    table.CheckConstraint("CK_DbBusinessUnit_Code", " \"Code\" ~ '^[A-Z0-9_-]{1,50}$' ");
                    table.CheckConstraint("CK_DbBusinessUnit_Name", " length(btrim(\"Name\")) > 0 ");
                });

            migrationBuilder.CreateTable(
                name: "DbCostCenter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCostCenter", x => x.Id);
                    table.CheckConstraint("CK_DbCostCenter_Code", " \"Code\" ~ '^[A-Z0-9_-]{1,50}$' ");
                    table.CheckConstraint("CK_DbCostCenter_Name", " length(btrim(\"Name\")) > 0 ");
                    table.ForeignKey(
                        name: "FK_DbCostCenter_DbCompany_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "DbCompany",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DbDivision",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbDivision", x => x.Id);
                    table.UniqueConstraint("AK_DbDivision_Id_CompanyId", x => new { x.Id, x.CompanyId });
                    table.CheckConstraint("CK_DbDivision_Code", " \"Code\" ~ '^[A-Z0-9_-]{1,50}$' ");
                    table.CheckConstraint("CK_DbDivision_Name", " length(btrim(\"Name\")) > 0 ");
                    table.ForeignKey(
                        name: "FK_DbDivision_DbBranch_BranchId_CompanyId",
                        columns: x => new { x.BranchId, x.CompanyId },
                        principalTable: "DbBranch",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DbDivision_DbCompany_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "DbCompany",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DbPosition",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbPosition", x => x.Id);
                    table.CheckConstraint("CK_DbPosition_Code", " \"Code\" ~ '^[A-Z0-9_-]{1,50}$' ");
                    table.CheckConstraint("CK_DbPosition_Name", " length(btrim(\"Name\")) > 0 ");
                });

            migrationBuilder.CreateTable(
                name: "DbDepartment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DivisionId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbDepartment", x => x.Id);
                    table.CheckConstraint("CK_DbDepartment_Code", " \"Code\" ~ '^[A-Z0-9_-]{1,50}$' ");
                    table.CheckConstraint("CK_DbDepartment_Name", " length(btrim(\"Name\")) > 0 ");
                    table.ForeignKey(
                        name: "FK_DbDepartment_DbCompany_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "DbCompany",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DbDepartment_DbDivision_DivisionId_CompanyId",
                        columns: x => new { x.DivisionId, x.CompanyId },
                        principalTable: "DbDivision",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_DbBranch_Code",
                table: "DbBranch",
                sql: " \"Code\" ~ '^[A-Z0-9_-]{1,50}$' ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DbBranch_Name",
                table: "DbBranch",
                sql: " length(btrim(\"Name\")) > 0 ");

            migrationBuilder.CreateIndex(
                name: "IX_DbBusinessUnit_Code",
                table: "DbBusinessUnit",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbCostCenter_CompanyId_Code",
                table: "DbCostCenter",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbDepartment_CompanyId_Code",
                table: "DbDepartment",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbDepartment_DivisionId_CompanyId",
                table: "DbDepartment",
                columns: new[] { "DivisionId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_DbDivision_BranchId_CompanyId",
                table: "DbDivision",
                columns: new[] { "BranchId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_DbDivision_CompanyId_Code",
                table: "DbDivision",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbPosition_Code",
                table: "DbPosition",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbBusinessUnit");

            migrationBuilder.DropTable(
                name: "DbCostCenter");

            migrationBuilder.DropTable(
                name: "DbDepartment");

            migrationBuilder.DropTable(
                name: "DbPosition");

            migrationBuilder.DropTable(
                name: "DbDivision");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DbBranch_Id_CompanyId",
                table: "DbBranch");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DbBranch_Code",
                table: "DbBranch");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DbBranch_Name",
                table: "DbBranch");

            migrationBuilder.DropColumn(
                name: "CodeLocked",
                table: "DbBranch");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "DbBranch");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DbBranch");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "DbBranch");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DbBranch");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DbBranch");
        }
    }
}
