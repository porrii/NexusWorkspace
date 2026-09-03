using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusWorkspace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TargetKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    QuickAction = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ActorLabel = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TargetKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TargetKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorLabel = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    RawText = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ParsedHint = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ConvertedToKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ConvertedToId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                    table.ForeignKey(
                        name: "FK_People_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DueDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OwnerPersonId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastOpenedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_People_OwnerPersonId",
                        column: x => x.OwnerPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCompanies",
                columns: table => new
                {
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    LinkedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCompanies", x => new { x.ProjectId, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_ProjectCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectCompanies_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPeople",
                columns: table => new
                {
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LinkedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPeople", x => new { x.ProjectId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_ProjectPeople_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectPeople_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTags",
                columns: table => new
                {
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TagId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTags", x => new { x.ProjectId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ProjectTags_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DueDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssigneePersonId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    RelatedCompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    SortKey = table.Column<double>(type: "REAL", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Companies_RelatedCompanyId",
                        column: x => x.RelatedCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkTasks_People_AssigneePersonId",
                        column: x => x.AssigneePersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    WorkTaskId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsChecked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SortKey = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistItems_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    WorkTaskId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ParentSubTaskId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    IsDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SortKey = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubTasks_SubTasks_ParentSubTaskId",
                        column: x => x.ParentSubTaskId,
                        principalTable: "SubTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubTasks_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskDependencies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    WorkTaskId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    DependsOnWorkTaskId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_WorkTasks_DependsOnWorkTaskId",
                        column: x => x.DependsOnWorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkTaskPeople",
                columns: table => new
                {
                    WorkTaskId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    LinkedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTaskPeople", x => new { x.WorkTaskId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_WorkTaskPeople_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkTaskPeople_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkTaskTags",
                columns: table => new
                {
                    WorkTaskId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TagId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTaskTags", x => new { x.WorkTaskId, x.TagId });
                    table.ForeignKey(
                        name: "FK_WorkTaskTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkTaskTags_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_OccurredAtUtc",
                table: "ActivityEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_ProjectId_OccurredAtUtc",
                table: "ActivityEvents",
                columns: new[] { "ProjectId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_TargetKind_TargetId_OccurredAtUtc",
                table: "ActivityEvents",
                columns: new[] { "TargetKind", "TargetId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_Type",
                table: "ActivityEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ContentHash",
                table: "Attachments",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_IsDeleted",
                table: "Attachments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ProjectId",
                table: "Attachments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TargetKind_TargetId",
                table: "Attachments",
                columns: new[] { "TargetKind", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_WorkTaskId_SortKey",
                table: "ChecklistItems",
                columns: new[] { "WorkTaskId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_IsDeleted",
                table: "Comments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ProjectId",
                table: "Comments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TargetKind_TargetId",
                table: "Comments",
                columns: new[] { "TargetKind", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsDeleted",
                table: "Companies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_CreatedAtUtc",
                table: "InboxItems",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_IsDeleted",
                table: "InboxItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_State",
                table: "InboxItems",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_People_CompanyId",
                table: "People",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_People_IsDeleted",
                table: "People",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_People_Name",
                table: "People",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCompanies_CompanyId",
                table: "ProjectCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPeople_PersonId",
                table: "ProjectPeople",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_IsArchived",
                table: "Projects",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_IsDeleted",
                table: "Projects",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_LastOpenedAtUtc",
                table: "Projects",
                column: "LastOpenedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerPersonId",
                table: "Projects",
                column: "OwnerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTags_TagId",
                table: "ProjectTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTasks_ParentSubTaskId",
                table: "SubTasks",
                column: "ParentSubTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTasks_WorkTaskId_ParentSubTaskId_SortKey",
                table: "SubTasks",
                columns: new[] { "WorkTaskId", "ParentSubTaskId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_DependsOnWorkTaskId",
                table: "TaskDependencies",
                column: "DependsOnWorkTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_WorkTaskId_DependsOnWorkTaskId",
                table: "TaskDependencies",
                columns: new[] { "WorkTaskId", "DependsOnWorkTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkTaskPeople_PersonId",
                table: "WorkTaskPeople",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_AssigneePersonId",
                table: "WorkTasks",
                column: "AssigneePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_DueDateUtc",
                table: "WorkTasks",
                column: "DueDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_IsArchived",
                table: "WorkTasks",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_IsDeleted",
                table: "WorkTasks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_ProjectId",
                table: "WorkTasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_ProjectId_SortKey",
                table: "WorkTasks",
                columns: new[] { "ProjectId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_RelatedCompanyId",
                table: "WorkTasks",
                column: "RelatedCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_Status",
                table: "WorkTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTaskTags_TagId",
                table: "WorkTaskTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityEvents");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "ChecklistItems");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "InboxItems");

            migrationBuilder.DropTable(
                name: "ProjectCompanies");

            migrationBuilder.DropTable(
                name: "ProjectPeople");

            migrationBuilder.DropTable(
                name: "ProjectTags");

            migrationBuilder.DropTable(
                name: "SubTasks");

            migrationBuilder.DropTable(
                name: "TaskDependencies");

            migrationBuilder.DropTable(
                name: "WorkTaskPeople");

            migrationBuilder.DropTable(
                name: "WorkTaskTags");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "WorkTasks");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
