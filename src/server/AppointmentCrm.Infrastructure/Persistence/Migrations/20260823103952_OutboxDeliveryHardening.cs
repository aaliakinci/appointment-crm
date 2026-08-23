using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentCrm.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class OutboxDeliveryHardening : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_outbox_messages_pending",
            table: "outbox_messages");

        migrationBuilder.AddColumn<string>(
            name: "correlation_id",
            table: "outbox_messages",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "failed_at_utc",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "lease_id",
            table: "outbox_messages",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "locked_until_utc",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "trace_parent",
            table: "outbox_messages",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "trace_state",
            table: "outbox_messages",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddUniqueConstraint(
            name: "ak_outbox_messages_tenant_id",
            table: "outbox_messages",
            columns: new[] { "tenant_id", "id" });

        migrationBuilder.CreateTable(
            name: "notification_deliveries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                message_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                aggregate_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                delivered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                trace_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_deliveries", x => x.id);
                table.ForeignKey(
                    name: "FK_notification_deliveries_outbox_messages_tenant_id_outbox_me~",
                    columns: x => new { x.tenant_id, x.outbox_message_id },
                    principalTable: "outbox_messages",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_pending",
            table: "outbox_messages",
            columns: new[] { "processed_at_utc", "failed_at_utc", "next_attempt_at_utc", "locked_until_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_notification_deliveries_tenant_aggregate",
            table: "notification_deliveries",
            columns: new[] { "tenant_id", "aggregate_type", "aggregate_id" });

        migrationBuilder.CreateIndex(
            name: "ux_notification_deliveries_tenant_outbox",
            table: "notification_deliveries",
            columns: new[] { "tenant_id", "outbox_message_id" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notification_deliveries");

        migrationBuilder.DropUniqueConstraint(
            name: "ak_outbox_messages_tenant_id",
            table: "outbox_messages");

        migrationBuilder.DropIndex(
            name: "ix_outbox_messages_pending",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "correlation_id",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "failed_at_utc",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "lease_id",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "locked_until_utc",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "trace_parent",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "trace_state",
            table: "outbox_messages");

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_pending",
            table: "outbox_messages",
            columns: new[] { "processed_at_utc", "next_attempt_at_utc", "occurred_at_utc" });
    }
}
