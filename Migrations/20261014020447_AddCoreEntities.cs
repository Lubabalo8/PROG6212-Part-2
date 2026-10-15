using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    User_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.User_ID);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Event_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Organiser_ID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Event_Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Event_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Registration_Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Event_ID);
                    table.ForeignKey(
                        name: "FK_Events_Users_Organiser_ID",
                        column: x => x.Organiser_ID,
                        principalTable: "Users",
                        principalColumn: "User_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Category_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Event_ID = table.Column<int>(type: "int", nullable: false),
                    Category_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Distance_km = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Entry_Fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Max_Participants = table.Column<int>(type: "int", nullable: false),
                    Start_Time = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Category_ID);
                    table.ForeignKey(
                        name: "FK_Categories_Events_Event_ID",
                        column: x => x.Event_ID,
                        principalTable: "Events",
                        principalColumn: "Event_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Route_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Event_ID = table.Column<int>(type: "int", nullable: false),
                    Elevation_Gain = table.Column<decimal>(type: "decimal(8,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Route_ID);
                    table.ForeignKey(
                        name: "FK_Routes_Events_Event_ID",
                        column: x => x.Event_ID,
                        principalTable: "Events",
                        principalColumn: "Event_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    Enrollment_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User_ID = table.Column<int>(type: "int", nullable: false),
                    Category_ID = table.Column<int>(type: "int", nullable: false),
                    Enrollment_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Bib_Number = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.Enrollment_ID);
                    table.ForeignKey(
                        name: "FK_Enrolments_Categories_Category_ID",
                        column: x => x.Category_ID,
                        principalTable: "Categories",
                        principalColumn: "Category_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Users_User_ID",
                        column: x => x.User_ID,
                        principalTable: "Users",
                        principalColumn: "User_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Result_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enrollment_ID = table.Column<int>(type: "int", nullable: false),
                    Finish_Time_Seconds = table.Column<int>(type: "int", nullable: false),
                    Position_Overall = table.Column<int>(type: "int", nullable: false),
                    Position_Category = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Result_ID);
                    table.ForeignKey(
                        name: "FK_Results_Enrolments_Enrollment_ID",
                        column: x => x.Enrollment_ID,
                        principalTable: "Enrolments",
                        principalColumn: "Enrollment_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Event_ID",
                table: "Categories",
                column: "Event_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_Category_ID",
                table: "Enrolments",
                column: "Category_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_User_ID_Category_ID",
                table: "Enrolments",
                columns: new[] { "User_ID", "Category_ID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Organiser_ID",
                table: "Events",
                column: "Organiser_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Results_Enrollment_ID",
                table: "Results",
                column: "Enrollment_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Event_ID",
                table: "Routes",
                column: "Event_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
