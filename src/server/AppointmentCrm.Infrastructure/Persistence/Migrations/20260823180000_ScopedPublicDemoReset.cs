using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentCrm.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppointmentCrmDbContext))]
[Migration("20260823180000_ScopedPublicDemoReset")]
public sealed class ScopedPublicDemoReset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_weekly_schedule_versions_weekly_schedules_tenant_id_schedul~",
            table: "weekly_schedule_versions");
        migrationBuilder.AddForeignKey(
            name: "FK_weekly_schedule_versions_weekly_schedules_tenant_id_schedul~",
            table: "weekly_schedule_versions",
            columns: ["tenant_id", "schedule_id"],
            principalTable: "weekly_schedules",
            principalColumns: ["tenant_id", "id"],
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION prevent_weekly_schedule_history_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $immutable_history$
            BEGIN
                IF TG_OP = 'DELETE'
                   AND OLD.tenant_id = '10000000-0000-0000-0000-000000000001'::uuid
                   AND current_setting('appointment_crm.demo_reset_tenant', true) = OLD.tenant_id::text
                THEN
                    RETURN OLD;
                END IF;

                RAISE EXCEPTION 'Published weekly schedule history is immutable.'
                    USING ERRCODE = '55000';
            END;
            $immutable_history$;

            CREATE TRIGGER trg_weekly_schedules_delete_guard
            BEFORE DELETE ON weekly_schedules
            FOR EACH ROW EXECUTE FUNCTION prevent_weekly_schedule_history_mutation();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER trg_weekly_schedules_delete_guard ON weekly_schedules;

            CREATE OR REPLACE FUNCTION prevent_weekly_schedule_history_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $immutable_history$
            BEGIN
                RAISE EXCEPTION 'Published weekly schedule history is immutable.'
                    USING ERRCODE = '55000';
            END;
            $immutable_history$;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_weekly_schedule_versions_weekly_schedules_tenant_id_schedul~",
            table: "weekly_schedule_versions");
        migrationBuilder.AddForeignKey(
            name: "FK_weekly_schedule_versions_weekly_schedules_tenant_id_schedul~",
            table: "weekly_schedule_versions",
            columns: ["tenant_id", "schedule_id"],
            principalTable: "weekly_schedules",
            principalColumns: ["tenant_id", "id"],
            onDelete: ReferentialAction.Restrict);
    }
}
