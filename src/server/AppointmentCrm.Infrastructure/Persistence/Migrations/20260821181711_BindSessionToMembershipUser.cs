using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentCrm.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class BindSessionToMembershipUser : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_user_sessions_tenant_memberships_tenant_id_membership_id",
            table: "user_sessions");

        migrationBuilder.DropIndex(
            name: "IX_user_sessions_tenant_id_membership_id",
            table: "user_sessions");

        migrationBuilder.DropUniqueConstraint(
            name: "AK_tenant_memberships_tenant_id_id",
            table: "tenant_memberships");

        migrationBuilder.AddUniqueConstraint(
            name: "AK_tenant_memberships_tenant_id_id_user_id",
            table: "tenant_memberships",
            columns: new[] { "tenant_id", "id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "IX_user_sessions_tenant_id_membership_id_user_id",
            table: "user_sessions",
            columns: new[] { "tenant_id", "membership_id", "user_id" });

        migrationBuilder.AddForeignKey(
            name: "FK_user_sessions_tenant_memberships_tenant_id_membership_id_us~",
            table: "user_sessions",
            columns: new[] { "tenant_id", "membership_id", "user_id" },
            principalTable: "tenant_memberships",
            principalColumns: new[] { "tenant_id", "id", "user_id" },
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_user_sessions_tenant_memberships_tenant_id_membership_id_us~",
            table: "user_sessions");

        migrationBuilder.DropIndex(
            name: "IX_user_sessions_tenant_id_membership_id_user_id",
            table: "user_sessions");

        migrationBuilder.DropUniqueConstraint(
            name: "AK_tenant_memberships_tenant_id_id_user_id",
            table: "tenant_memberships");

        migrationBuilder.AddUniqueConstraint(
            name: "AK_tenant_memberships_tenant_id_id",
            table: "tenant_memberships",
            columns: new[] { "tenant_id", "id" });

        migrationBuilder.CreateIndex(
            name: "IX_user_sessions_tenant_id_membership_id",
            table: "user_sessions",
            columns: new[] { "tenant_id", "membership_id" });

        migrationBuilder.AddForeignKey(
            name: "FK_user_sessions_tenant_memberships_tenant_id_membership_id",
            table: "user_sessions",
            columns: new[] { "tenant_id", "membership_id" },
            principalTable: "tenant_memberships",
            principalColumns: new[] { "tenant_id", "id" },
            onDelete: ReferentialAction.Restrict);
    }
}
