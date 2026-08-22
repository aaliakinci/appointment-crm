using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AppointmentCrm.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class WorkingHoursAndAvailability : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "date_schedule_overrides",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                date = table.Column<DateOnly>(type: "date", nullable: false),
                is_closed = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_date_schedule_overrides", x => x.id);
                table.UniqueConstraint("AK_date_schedule_overrides_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "FK_date_schedule_overrides_employees_tenant_id_employee_id",
                    columns: x => new { x.tenant_id, x.employee_id },
                    principalTable: "employees",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "employee_time_offs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employee_time_offs", x => x.id);
                table.CheckConstraint("ck_employee_time_offs_range", "start_utc < end_utc");
                table.ForeignKey(
                    name: "FK_employee_time_offs_employees_tenant_id_employee_id",
                    columns: x => new { x.tenant_id, x.employee_id },
                    principalTable: "employees",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "weekly_schedules",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weekly_schedules", x => x.id);
                table.UniqueConstraint("AK_weekly_schedules_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "FK_weekly_schedules_employees_tenant_id_employee_id",
                    columns: x => new { x.tenant_id, x.employee_id },
                    principalTable: "employees",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "date_schedule_override_periods",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                override_id = table.Column<Guid>(type: "uuid", nullable: false),
                start_minute = table.Column<int>(type: "integer", nullable: false),
                end_minute = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_date_schedule_override_periods", x => x.id);
                table.CheckConstraint("ck_date_override_periods_minutes", "start_minute >= 0 AND end_minute <= 1440 AND start_minute < end_minute AND start_minute % 5 = 0 AND end_minute % 5 = 0");
                table.ForeignKey(
                    name: "FK_date_schedule_override_periods_date_schedule_overrides_tena~",
                    columns: x => new { x.tenant_id, x.override_id },
                    principalTable: "date_schedule_overrides",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "weekly_schedule_periods",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                day_of_week = table.Column<int>(type: "integer", nullable: false),
                start_minute = table.Column<int>(type: "integer", nullable: false),
                end_minute = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weekly_schedule_periods", x => x.id);
                table.CheckConstraint("ck_weekly_schedule_periods_day", "day_of_week BETWEEN 1 AND 7");
                table.CheckConstraint("ck_weekly_schedule_periods_minutes", "start_minute >= 0 AND end_minute <= 1440 AND start_minute < end_minute AND start_minute % 5 = 0 AND end_minute % 5 = 0");
                table.ForeignKey(
                    name: "FK_weekly_schedule_periods_weekly_schedules_tenant_id_schedule~",
                    columns: x => new { x.tenant_id, x.schedule_id },
                    principalTable: "weekly_schedules",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            table: "permissions",
            columns: new[] { "code", "name" },
            values: new object[,]
            {
                { "availability.read", "availability.read" },
                { "scheduling.manage", "scheduling.manage" }
            });

        migrationBuilder.InsertData(
            table: "role_permissions",
            columns: new[] { "permission_code", "role_code" },
            values: new object[,]
            {
                { "availability.read", "Employee" },
                { "availability.read", "Manager" },
                { "scheduling.manage", "Manager" },
                { "availability.read", "Owner" },
                { "scheduling.manage", "Owner" },
                { "availability.read", "Receptionist" }
            });

        migrationBuilder.CreateIndex(
            name: "ix_date_override_periods_lookup",
            table: "date_schedule_override_periods",
            columns: new[] { "tenant_id", "override_id", "start_minute" });

        migrationBuilder.CreateIndex(
            name: "ux_date_overrides_employee_date",
            table: "date_schedule_overrides",
            columns: new[] { "tenant_id", "employee_id", "date" },
            unique: true,
            filter: "employee_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ux_date_overrides_tenant_date",
            table: "date_schedule_overrides",
            columns: new[] { "tenant_id", "date" },
            unique: true,
            filter: "employee_id IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_employee_time_offs_overlap",
            table: "employee_time_offs",
            columns: new[] { "tenant_id", "employee_id", "start_utc", "end_utc" });

        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
        migrationBuilder.Sql(
            """
            ALTER TABLE employee_time_offs
            ADD CONSTRAINT ex_employee_time_offs_no_overlap
            EXCLUDE USING gist (
                tenant_id WITH =,
                employee_id WITH =,
                tstzrange(start_utc, end_utc, '[)') WITH &&
            );
            """);

        migrationBuilder.CreateIndex(
            name: "ix_weekly_schedule_periods_lookup",
            table: "weekly_schedule_periods",
            columns: new[] { "tenant_id", "schedule_id", "day_of_week", "start_minute" });

        migrationBuilder.CreateIndex(
            name: "ux_weekly_schedules_tenant_default",
            table: "weekly_schedules",
            column: "tenant_id",
            unique: true,
            filter: "employee_id IS NULL");

        migrationBuilder.CreateIndex(
            name: "ux_weekly_schedules_tenant_employee",
            table: "weekly_schedules",
            columns: new[] { "tenant_id", "employee_id" },
            unique: true,
            filter: "employee_id IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "date_schedule_override_periods");

        migrationBuilder.DropTable(
            name: "employee_time_offs");

        migrationBuilder.DropTable(
            name: "weekly_schedule_periods");

        migrationBuilder.DropTable(
            name: "date_schedule_overrides");

        migrationBuilder.DropTable(
            name: "weekly_schedules");

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "availability.read", "Employee" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "availability.read", "Manager" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "scheduling.manage", "Manager" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "availability.read", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "scheduling.manage", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "availability.read", "Receptionist" });

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "availability.read");

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "scheduling.manage");
    }
}
