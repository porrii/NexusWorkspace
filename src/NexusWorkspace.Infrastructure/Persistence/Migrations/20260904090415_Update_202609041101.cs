using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusWorkspace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Update_202609041101 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FollowUps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TargetKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    WaitingOnPersonId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    WaitingOnCompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    WaitingOnLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    WaitingSinceUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastContactUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextFollowUpUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReminderCount = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Resolution = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUps_Companies_WaitingOnCompanyId",
                        column: x => x.WaitingOnCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FollowUps_People_WaitingOnPersonId",
                        column: x => x.WaitingOnPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FollowUps_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDismissed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TargetKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RemindAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notified = table.Column<bool>(type: "INTEGER", nullable: false),
                    TargetKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reminders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_IsDeleted",
                table: "FollowUps",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_NextFollowUpUtc",
                table: "FollowUps",
                column: "NextFollowUpUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_ProjectId",
                table: "FollowUps",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_State",
                table: "FollowUps",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_TargetKind_TargetId",
                table: "FollowUps",
                columns: new[] { "TargetKind", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_WaitingOnCompanyId",
                table: "FollowUps",
                column: "WaitingOnCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_WaitingOnPersonId",
                table: "FollowUps",
                column: "WaitingOnPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_WaitingSinceUtc",
                table: "FollowUps",
                column: "WaitingSinceUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAtUtc",
                table: "Notifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead_IsDismissed",
                table: "Notifications",
                columns: new[] { "IsRead", "IsDismissed" });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_IsDeleted",
                table: "Reminders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_ProjectId",
                table: "Reminders",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_RemindAtUtc",
                table: "Reminders",
                column: "RemindAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_Status",
                table: "Reminders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_Status_Notified_RemindAtUtc",
                table: "Reminders",
                columns: new[] { "Status", "Notified", "RemindAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_TargetKind_TargetId",
                table: "Reminders",
                columns: new[] { "TargetKind", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FollowUps");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Reminders");
        }
    }
}
