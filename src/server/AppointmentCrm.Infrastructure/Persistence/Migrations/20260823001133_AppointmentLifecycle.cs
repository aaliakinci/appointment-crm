using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AppointmentCrm.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AppointmentLifecycle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "appointments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                service_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                service_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                service_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                service_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                service_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                revision = table.Column<long>(type: "bigint", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_appointments", x => x.id);
                table.UniqueConstraint("AK_appointments_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_appointments_range", "starts_at_utc < ends_at_utc");
                table.CheckConstraint("ck_appointments_revision", "revision > 0");
                table.CheckConstraint("ck_appointments_snapshot_currency", "service_currency ~ '^[A-Z]{3}$'");
                table.CheckConstraint("ck_appointments_snapshot_duration", "service_duration_minutes BETWEEN 5 AND 480 AND service_duration_minutes % 5 = 0");
                table.CheckConstraint("ck_appointments_snapshot_price", "service_price >= 0 AND service_price <= 1000000");
                table.CheckConstraint("ck_appointments_status", "status IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");
                table.ForeignKey(
                    name: "FK_appointments_customers_tenant_id_customer_id",
                    columns: x => new { x.tenant_id, x.customer_id },
                    principalTable: "customers",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_appointments_employees_tenant_id_employee_id",
                    columns: x => new { x.tenant_id, x.employee_id },
                    principalTable: "employees",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_appointments_services_tenant_id_service_id",
                    columns: x => new { x.tenant_id, x.service_id },
                    principalTable: "services",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                aggregate_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                payload_json = table.Column<string>(type: "jsonb", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                attempts = table.Column<int>(type: "integer", nullable: false),
                next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_messages", x => x.id);
                table.CheckConstraint("ck_outbox_messages_attempts", "attempts >= 0");
                table.ForeignKey(
                    name: "FK_outbox_messages_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "appointment_status_history",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                from_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                to_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_appointment_status_history", x => x.id);
                table.CheckConstraint("ck_appointment_status_history_from_status", "from_status IS NULL OR from_status IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");
                table.CheckConstraint("ck_appointment_status_history_to_status", "to_status IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");
                table.ForeignKey(
                    name: "FK_appointment_status_history_appointments_tenant_id_appointme~",
                    columns: x => new { x.tenant_id, x.appointment_id },
                    principalTable: "appointments",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_appointment_status_history_tenant_memberships_tenant_id_act~",
                    columns: x => new { x.tenant_id, x.actor_membership_id, x.actor_user_id },
                    principalTable: "tenant_memberships",
                    principalColumns: new[] { "tenant_id", "id", "user_id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "permissions",
            columns: new[] { "code", "name" },
            values: new object[,]
            {
                { "appointments.read", "appointments.read" },
                { "appointments.transition-own", "appointments.transition-own" }
            });

        migrationBuilder.InsertData(
            table: "role_permissions",
            columns: new[] { "permission_code", "role_code" },
            values: new object[,]
            {
                { "appointments.transition-own", "Employee" },
                { "appointments.read", "Manager" },
                { "appointments.read", "Owner" },
                { "appointments.transition-own", "Owner" },
                { "appointments.read", "Receptionist" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_appointment_status_history_tenant_id_actor_membership_id_ac~",
            table: "appointment_status_history",
            columns: new[] { "tenant_id", "actor_membership_id", "actor_user_id" });

        migrationBuilder.CreateIndex(
            name: "ix_appointment_status_history_timeline",
            table: "appointment_status_history",
            columns: new[] { "tenant_id", "appointment_id", "occurred_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_appointments_tenant_customer_start",
            table: "appointments",
            columns: new[] { "tenant_id", "customer_id", "starts_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_appointments_tenant_employee_start",
            table: "appointments",
            columns: new[] { "tenant_id", "employee_id", "starts_at_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_appointments_tenant_id_service_id",
            table: "appointments",
            columns: new[] { "tenant_id", "service_id" });

        migrationBuilder.CreateIndex(
            name: "ix_appointments_tenant_status_start",
            table: "appointments",
            columns: new[] { "tenant_id", "status", "starts_at_utc" });

        migrationBuilder.Sql("""
            ALTER TABLE appointments
            ADD CONSTRAINT ex_appointments_no_employee_overlap
            EXCLUDE USING gist (
                tenant_id WITH =,
                employee_id WITH =,
                tstzrange(starts_at_utc, ends_at_utc, '[)') WITH &&
            )
            WHERE (status IN ('Scheduled', 'Confirmed', 'Completed', 'NoShow'));
            """);

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_pending",
            table: "outbox_messages",
            columns: new[] { "processed_at_utc", "next_attempt_at_utc", "occurred_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_tenant_aggregate",
            table: "outbox_messages",
            columns: new[] { "tenant_id", "aggregate_type", "aggregate_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "appointment_status_history");

        migrationBuilder.DropTable(
            name: "outbox_messages");

        migrationBuilder.DropTable(
            name: "appointments");

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "appointments.transition-own", "Employee" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "appointments.read", "Manager" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "appointments.read", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "appointments.transition-own", "Owner" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: new[] { "permission_code", "role_code" },
            keyValues: new object[] { "appointments.read", "Receptionist" });

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "appointments.read");

        migrationBuilder.DeleteData(
            table: "permissions",
            keyColumn: "code",
            keyValue: "appointments.transition-own");
    }
}
