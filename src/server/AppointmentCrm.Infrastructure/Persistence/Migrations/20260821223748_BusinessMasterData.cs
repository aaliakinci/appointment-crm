using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AppointmentCrm.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class BusinessMasterData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_tenant_memberships_tenant_user",
            table: "tenant_memberships");

        migrationBuilder.AddUniqueConstraint(
            name: "ak_tenant_memberships_tenant_user",
            table: "tenant_memberships",
            columns: new[] { "tenant_id", "user_id" });

        migrationBuilder.CreateTable(
            name: "audit_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                target_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                target_id = table.Column<Guid>(type: "uuid", nullable: false),
                summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_entries", x => x.id);
                table.ForeignKey(
                    name: "FK_audit_entries_tenant_memberships_tenant_id_actor_membership~",
                    columns: x => new { x.tenant_id, x.actor_membership_id, x.actor_user_id },
                    principalTable: "tenant_memberships",
                    principalColumns: new[] { "tenant_id", "id", "user_id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "customers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                normalized_phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                archived_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_customers", x => x.id);
                table.UniqueConstraint("AK_customers_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "FK_customers_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "employees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: true),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                normalized_phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employees", x => x.id);
                table.UniqueConstraint("AK_employees_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "FK_employees_tenant_memberships_tenant_id_user_id",
                    columns: x => new { x.tenant_id, x.user_id },
                    principalTable: "tenant_memberships",
                    principalColumns: new[] { "tenant_id", "user_id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_employees_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "services",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                duration_minutes = table.Column<int>(type: "integer", nullable: false),
                price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_services", x => x.id);
                table.UniqueConstraint("AK_services_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_services_currency", "currency ~ '^[A-Z]{3}$'");
                table.CheckConstraint("ck_services_duration", "duration_minutes BETWEEN 5 AND 480 AND duration_minutes % 5 = 0");
                table.CheckConstraint("ck_services_price", "price >= 0 AND price <= 1000000");
                table.ForeignKey(
                    name: "FK_services_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "employee_services",
            columns: table => new
            {
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                service_id = table.Column<Guid>(type: "uuid", nullable: false),
                assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employee_services", x => new { x.tenant_id, x.employee_id, x.service_id });
                table.ForeignKey(
                    name: "FK_employee_services_employees_tenant_id_employee_id",
                    columns: x => new { x.tenant_id, x.employee_id },
                    principalTable: "employees",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_employee_services_services_tenant_id_service_id",
                    columns: x => new { x.tenant_id, x.service_id },
                    principalTable: "services",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "permissions",
            columns: new[] { "code", "name" },
            values: new object[,]
            {
                { "customers.read", "customers.read" },
                { "employees.manage", "employees.manage" },
                { "employees.read", "employees.read" },
                { "services.manage", "services.manage" },
                { "services.read", "services.read" }
            });

        migrationBuilder.InsertData(
            table: "role_permissions",
            columns: new[] { "permission_code", "role_code" },
            values: new object[,]
            {
                { "services.read", "Employee" },
                { "customers.read", "Manager" },
                { "employees.manage", "Manager" },
                { "employees.read", "Manager" },
                { "services.manage", "Manager" },
                { "services.read", "Manager" },
                { "customers.read", "Owner" },
                { "employees.manage", "Owner" },
                { "employees.read", "Owner" },
                { "services.manage", "Owner" },
                { "services.read", "Owner" },
                { "customers.read", "Receptionist" },
                { "employees.read", "Receptionist" },
                { "services.read", "Receptionist" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_audit_entries_tenant_id_actor_membership_id_actor_user_id",
            table: "audit_entries",
            columns: new[] { "tenant_id", "actor_membership_id", "actor_user_id" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_entries_tenant_occurred",
            table: "audit_entries",
            columns: new[] { "tenant_id", "occurred_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_entries_tenant_target",
            table: "audit_entries",
            columns: new[] { "tenant_id", "target_type", "target_id" });

        migrationBuilder.CreateIndex(
            name: "ix_customers_tenant_active_name",
            table: "customers",
            columns: new[] { "tenant_id", "archived_at_utc", "normalized_name" });

        migrationBuilder.CreateIndex(
            name: "ux_customers_tenant_email",
            table: "customers",
            columns: new[] { "tenant_id", "normalized_email" },
            unique: true,
            filter: "normalized_email IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ux_customers_tenant_phone",
            table: "customers",
            columns: new[] { "tenant_id", "normalized_phone" },
            unique: true,
            filter: "normalized_phone IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_employee_services_tenant_service",
            table: "employee_services",
            columns: new[] { "tenant_id", "service_id" });

        migrationBuilder.CreateIndex(
            name: "ix_employees_tenant_active_name",
            table: "employees",
            columns: new[] { "tenant_id", "is_active", "normalized_name" });

        migrationBuilder.CreateIndex(
            name: "ux_employees_tenant_user",
            table: "employees",
            columns: new[] { "tenant_id", "user_id" },
            unique: true,
            filter: "user_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_services_tenant_active_name",
            table: "services",
            columns: new[] { "tenant_id", "is_active", "normalized_name" });

        migrationBuilder.CreateIndex(
            name: "ux_services_tenant_name",
            table: "services",
            columns: new[] { "tenant_id", "normalized_name" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "audit_entries");

        migrationBuilder.DropTable(
            name: "customers");

        migrationBuilder.DropTable(
            name: "employee_services");

        migrationBuilder.DropTable(
            name: "employees");

        migrationBuilder.DropTable(
            name: "services");

        migrationBuilder.DropUniqueConstraint(
            name: "ak_tenant_memberships_tenant_user",
            table: "tenant_memberships");

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "services.read", "Employee" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "customers.read", "Manager" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "employees.manage", "Manager" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "employees.read", "Manager" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "services.manage", "Manager" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "services.read", "Manager" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "customers.read", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "employees.manage", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "employees.read", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "services.manage", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "services.read", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "customers.read", "Receptionist" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "employees.read", "Receptionist" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "services.read", "Receptionist" });

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "customers.read");

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "employees.manage");

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "employees.read");

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "services.manage");

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "services.read");

        migrationBuilder.CreateIndex(
            name: "ux_tenant_memberships_tenant_user",
            table: "tenant_memberships",
            columns: new[] { "tenant_id", "user_id" },
            unique: true);
    }
}
