using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusWorkspace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_ReminderRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndUtc",
                table: "Reminders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceFrequency",
                table: "Reminders",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                table: "Reminders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurrenceEndUtc",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "RecurrenceFrequency",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                table: "Reminders");
        }
    }
}
