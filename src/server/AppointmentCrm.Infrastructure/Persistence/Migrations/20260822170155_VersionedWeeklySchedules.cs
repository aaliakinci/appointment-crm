using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentCrm.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class VersionedWeeklySchedules : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "current_version_id",
            table: "weekly_schedules",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<long>(
            name: "revision",
            table: "weekly_schedules",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateTable(
            name: "weekly_schedule_versions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                version_number = table.Column<long>(type: "bigint", nullable: false),
                mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                actor_membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                change_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                restored_from_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weekly_schedule_versions", x => x.id);
                table.UniqueConstraint("AK_weekly_schedule_versions_tenant_id_id", x => new { x.tenant_id, x.id });
                table.UniqueConstraint("AK_weekly_schedule_versions_tenant_id_schedule_id_id", x => new { x.tenant_id, x.schedule_id, x.id });
                table.CheckConstraint("ck_weekly_schedule_versions_mode", "mode IN ('Custom', 'Closed', 'Inherited')");
                table.CheckConstraint("ck_weekly_schedule_versions_number", "version_number > 0");
                table.ForeignKey(
                    name: "FK_weekly_schedule_versions_tenant_memberships_tenant_id_actor~",
                    columns: x => new { x.tenant_id, x.actor_membership_id, x.actor_user_id },
                    principalTable: "tenant_memberships",
                    principalColumns: new[] { "tenant_id", "id", "user_id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_weekly_schedule_versions_weekly_schedule_versions_tenant_id~",
                    columns: x => new { x.tenant_id, x.schedule_id, x.restored_from_version_id },
                    principalTable: "weekly_schedule_versions",
                    principalColumns: new[] { "tenant_id", "schedule_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_weekly_schedule_versions_weekly_schedules_tenant_id_schedul~",
                    columns: x => new { x.tenant_id, x.schedule_id },
                    principalTable: "weekly_schedules",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "weekly_schedule_version_periods",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                version_id = table.Column<Guid>(type: "uuid", nullable: false),
                day_of_week = table.Column<int>(type: "integer", nullable: false),
                start_minute = table.Column<int>(type: "integer", nullable: false),
                end_minute = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weekly_schedule_version_periods", x => x.id);
                table.CheckConstraint("ck_weekly_schedule_version_periods_day", "day_of_week BETWEEN 1 AND 7");
                table.CheckConstraint("ck_weekly_schedule_version_periods_minutes", "start_minute >= 0 AND end_minute <= 1440 AND start_minute < end_minute AND start_minute % 5 = 0 AND end_minute % 5 = 0");
                table.ForeignKey(
                    name: "FK_weekly_schedule_version_periods_weekly_schedule_versions_te~",
                    columns: x => new { x.tenant_id, x.version_id },
                    principalTable: "weekly_schedule_versions",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql(
            """
            INSERT INTO weekly_schedule_versions (
                id,
                tenant_id,
                schedule_id,
                version_number,
                mode,
                actor_user_id,
                actor_membership_id,
                change_note,
                restored_from_version_id,
                created_at_utc)
            SELECT
                schedule.id,
                schedule.tenant_id,
                schedule.id,
                1,
                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM weekly_schedule_periods period
                        WHERE period.tenant_id = schedule.tenant_id
                          AND period.schedule_id = schedule.id)
                    THEN 'Custom'
                    ELSE 'Closed'
                END,
                NULL,
                NULL,
                NULL,
                NULL,
                schedule.updated_at_utc
            FROM weekly_schedules schedule;

            INSERT INTO weekly_schedule_version_periods (
                id,
                tenant_id,
                version_id,
                day_of_week,
                start_minute,
                end_minute)
            SELECT
                period.id,
                period.tenant_id,
                period.schedule_id,
                period.day_of_week,
                period.start_minute,
                period.end_minute
            FROM weekly_schedule_periods period;

            UPDATE weekly_schedules
            SET current_version_id = id,
                revision = 1;
            """);

        migrationBuilder.DropTable(
            name: "weekly_schedule_periods");

        migrationBuilder.AddCheckConstraint(
            name: "ck_weekly_schedules_revision",
            table: "weekly_schedules",
            sql: "revision > 0");

        migrationBuilder.CreateIndex(
            name: "ix_weekly_schedule_version_periods_lookup",
            table: "weekly_schedule_version_periods",
            columns: new[] { "tenant_id", "version_id", "day_of_week", "start_minute" });

        migrationBuilder.CreateIndex(
            name: "ix_weekly_schedule_versions_history",
            table: "weekly_schedule_versions",
            columns: new[] { "tenant_id", "schedule_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_weekly_schedule_versions_tenant_id_actor_membership_id_acto~",
            table: "weekly_schedule_versions",
            columns: new[] { "tenant_id", "actor_membership_id", "actor_user_id" });

        migrationBuilder.CreateIndex(
            name: "IX_weekly_schedule_versions_tenant_id_schedule_id_restored_fro~",
            table: "weekly_schedule_versions",
            columns: new[] { "tenant_id", "schedule_id", "restored_from_version_id" });

        migrationBuilder.CreateIndex(
            name: "ux_weekly_schedule_versions_schedule_number",
            table: "weekly_schedule_versions",
            columns: new[] { "tenant_id", "schedule_id", "version_number" },
            unique: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE weekly_schedules
            ADD CONSTRAINT fk_weekly_schedules_current_version
            FOREIGN KEY (tenant_id, id, current_version_id)
            REFERENCES weekly_schedule_versions (tenant_id, schedule_id, id)
            ON DELETE RESTRICT
            DEFERRABLE INITIALLY DEFERRED;

            CREATE FUNCTION prevent_weekly_schedule_history_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $immutable_history$
            BEGIN
                RAISE EXCEPTION 'Published weekly schedule history is immutable.'
                    USING ERRCODE = '55000';
            END;
            $immutable_history$;

            CREATE TRIGGER trg_weekly_schedule_versions_immutable
            BEFORE UPDATE OR DELETE ON weekly_schedule_versions
            FOR EACH ROW EXECUTE FUNCTION prevent_weekly_schedule_history_mutation();

            CREATE TRIGGER trg_weekly_schedule_version_periods_immutable
            BEFORE UPDATE OR DELETE ON weekly_schedule_version_periods
            FOR EACH ROW EXECUTE FUNCTION prevent_weekly_schedule_history_mutation();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "weekly_schedule_periods",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                day_of_week = table.Column<int>(type: "integer", nullable: false),
                end_minute = table.Column<int>(type: "integer", nullable: false),
                start_minute = table.Column<int>(type: "integer", nullable: false)
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

        migrationBuilder.CreateIndex(
            name: "ix_weekly_schedule_periods_lookup",
            table: "weekly_schedule_periods",
            columns: new[] { "tenant_id", "schedule_id", "day_of_week", "start_minute" });

        migrationBuilder.Sql(
            """
            INSERT INTO weekly_schedule_periods (
                id,
                tenant_id,
                schedule_id,
                day_of_week,
                start_minute,
                end_minute)
            SELECT
                period.id,
                period.tenant_id,
                schedule.id,
                period.day_of_week,
                period.start_minute,
                period.end_minute
            FROM weekly_schedules schedule
            JOIN weekly_schedule_version_periods period
              ON period.tenant_id = schedule.tenant_id
             AND period.version_id = schedule.current_version_id;

            ALTER TABLE weekly_schedules
            DROP CONSTRAINT fk_weekly_schedules_current_version;

            DROP TRIGGER trg_weekly_schedule_version_periods_immutable
                ON weekly_schedule_version_periods;
            DROP TRIGGER trg_weekly_schedule_versions_immutable
                ON weekly_schedule_versions;
            DROP FUNCTION prevent_weekly_schedule_history_mutation();
            """);

        migrationBuilder.DropTable(
            name: "weekly_schedule_version_periods");

        migrationBuilder.DropTable(
            name: "weekly_schedule_versions");

        migrationBuilder.DropCheckConstraint(
            name: "ck_weekly_schedules_revision",
            table: "weekly_schedules");

        migrationBuilder.DropColumn(
            name: "current_version_id",
            table: "weekly_schedules");

        migrationBuilder.DropColumn(
            name: "revision",
            table: "weekly_schedules");
    }
}
