using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusWorkspace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Update_202609041201_PeopleContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tags",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastContactedUtc",
                table: "People",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastContactedUtc",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Companies",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Communications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    WorkTaskId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    ContactLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Communications_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Communications_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Communications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Communications_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CompanyTags",
                columns: table => new
                {
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TagId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTags", x => new { x.CompanyId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CompanyTags_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityRelations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    FromKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    FromId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ToKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ToId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityRelations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Agenda = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meetings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PersonTags",
                columns: table => new
                {
                    PersonId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TagId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonTags", x => new { x.PersonId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PersonTags_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedSearches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    QueryText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FiltersJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    IsPinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortKey = table.Column<double>(type: "REAL", nullable: false),
                    LastRunUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeetingParticipants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    MeetingId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    ExternalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Attended = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_IsPinned",
                table: "Tags",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_People_IsArchived",
                table: "People",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_People_IsFavorite",
                table: "People",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_People_LastContactedUtc",
                table: "People",
                column: "LastContactedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsArchived",
                table: "Companies",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsFavorite",
                table: "Companies",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_CompanyId",
                table: "Communications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_IsDeleted",
                table: "Communications",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_OccurredAtUtc",
                table: "Communications",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_PersonId",
                table: "Communications",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_ProjectId",
                table: "Communications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Communications_WorkTaskId",
                table: "Communications",
                column: "WorkTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTags_TagId",
                table: "CompanyTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelations_FromKind_FromId",
                table: "EntityRelations",
                columns: new[] { "FromKind", "FromId" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelations_IsDeleted",
                table: "EntityRelations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelations_ToKind_ToId",
                table: "EntityRelations",
                columns: new[] { "ToKind", "ToId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_MeetingId_PersonId",
                table: "MeetingParticipants",
                columns: new[] { "MeetingId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_PersonId",
                table: "MeetingParticipants",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_IsDeleted",
                table: "Meetings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_ProjectId",
                table: "Meetings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_StartUtc",
                table: "Meetings",
                column: "StartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_Status",
                table: "Meetings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PersonTags_TagId",
                table: "PersonTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_IsPinned",
                table: "SavedSearches",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_Kind_SortKey",
                table: "SavedSearches",
                columns: new[] { "Kind", "SortKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Communications");

            migrationBuilder.DropTable(
                name: "CompanyTags");

            migrationBuilder.DropTable(
                name: "EntityRelations");

            migrationBuilder.DropTable(
                name: "MeetingParticipants");

            migrationBuilder.DropTable(
                name: "PersonTags");

            migrationBuilder.DropTable(
                name: "SavedSearches");

            migrationBuilder.DropTable(
                name: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_Tags_IsPinned",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_People_IsArchived",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_IsFavorite",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_LastContactedUtc",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Companies_IsArchived",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_IsFavorite",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "LastContactedUtc",
                table: "People");

            migrationBuilder.DropColumn(
                name: "LastContactedUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Companies");
        }
    }
}
