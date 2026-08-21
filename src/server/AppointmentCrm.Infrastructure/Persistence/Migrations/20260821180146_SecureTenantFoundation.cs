using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AppointmentCrm.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SecureTenantFoundation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_permissions", x => x.code);
            });

        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roles", x => x.code);
            });

        migrationBuilder.CreateTable(
            name: "tenants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tenants", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                security_version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                role_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                permission_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_permissions", x => new { x.role_code, x.permission_code });
                table.ForeignKey(
                    name: "FK_role_permissions_permissions_permission_code",
                    column: x => x.permission_code,
                    principalTable: "permissions",
                    principalColumn: "code",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_role_permissions_roles_role_code",
                    column: x => x.role_code,
                    principalTable: "roles",
                    principalColumn: "code",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "tenant_memberships",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                authorization_version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tenant_memberships", x => x.id);
                table.UniqueConstraint("AK_tenant_memberships_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_tenant_memberships_role", "role IN ('Owner', 'Manager', 'Receptionist', 'Employee')");
                table.ForeignKey(
                    name: "FK_tenant_memberships_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_tenant_memberships_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                family_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revocation_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                replaced_by_session_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_sessions", x => x.id);
                table.ForeignKey(
                    name: "FK_user_sessions_tenant_memberships_tenant_id_membership_id",
                    columns: x => new { x.tenant_id, x.membership_id },
                    principalTable: "tenant_memberships",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_user_sessions_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "permissions",
            columns: new[] { "code", "name" },
            values: new object[,]
            {
                { "appointments.manage", "appointments.manage" },
                { "appointments.read-own", "appointments.read-own" },
                { "customers.manage", "customers.manage" },
                { "memberships.manage", "memberships.manage" },
                { "memberships.read", "memberships.read" },
                { "reporting.read", "reporting.read" },
                { "sessions.manage-own", "sessions.manage-own" },
                { "tenant.read", "tenant.read" },
                { "tenant.switch", "tenant.switch" }
            });

        migrationBuilder.InsertData(
            table: "roles",
            columns: new[] { "code", "name" },
            values: new object[,]
            {
                { "Employee", "Employee" },
                { "Manager", "Manager" },
                { "Owner", "Owner" },
                { "Receptionist", "Receptionist" }
            });

        migrationBuilder.InsertData(
            table: "role_permissions",
            columns: new[] { "permission_code", "role_code" },
            values: new object[,]
            {
                { "appointments.read-own", "Employee" },
                { "sessions.manage-own", "Employee" },
                { "tenant.read", "Employee" },
                { "tenant.switch", "Employee" },
                { "appointments.manage", "Manager" },
                { "customers.manage", "Manager" },
                { "memberships.read", "Manager" },
                { "reporting.read", "Manager" },
                { "sessions.manage-own", "Manager" },
                { "tenant.read", "Manager" },
                { "tenant.switch", "Manager" },
                { "appointments.manage", "Owner" },
                { "appointments.read-own", "Owner" },
                { "customers.manage", "Owner" },
                { "memberships.manage", "Owner" },
                { "memberships.read", "Owner" },
                { "reporting.read", "Owner" },
                { "sessions.manage-own", "Owner" },
                { "tenant.read", "Owner" },
                { "tenant.switch", "Owner" },
                { "appointments.manage", "Receptionist" },
                { "customers.manage", "Receptionist" },
                { "sessions.manage-own", "Receptionist" },
                { "tenant.read", "Receptionist" },
                { "tenant.switch", "Receptionist" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_role_permissions_permission_code",
            table: "role_permissions",
            column: "permission_code");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_memberships_tenant_role",
            table: "tenant_memberships",
            columns: new[] { "tenant_id", "role" });

        migrationBuilder.CreateIndex(
            name: "IX_tenant_memberships_user_id",
            table: "tenant_memberships",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ux_tenant_memberships_tenant_user",
            table: "tenant_memberships",
            columns: new[] { "tenant_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_tenants_slug",
            table: "tenants",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_sessions_family_active",
            table: "user_sessions",
            columns: new[] { "family_id", "revoked_at_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_user_sessions_tenant_id_membership_id",
            table: "user_sessions",
            columns: new[] { "tenant_id", "membership_id" });

        migrationBuilder.CreateIndex(
            name: "ix_user_sessions_user_active",
            table: "user_sessions",
            columns: new[] { "user_id", "revoked_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ux_user_sessions_token_hash",
            table: "user_sessions",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_users_normalized_email",
            table: "users",
            column: "normalized_email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "role_permissions");

        migrationBuilder.DropTable(
            name: "user_sessions");

        migrationBuilder.DropTable(
            name: "permissions");

        migrationBuilder.DropTable(
            name: "roles");

        migrationBuilder.DropTable(
            name: "tenant_memberships");

        migrationBuilder.DropTable(
            name: "tenants");

        migrationBuilder.DropTable(
            name: "users");
    }
}
