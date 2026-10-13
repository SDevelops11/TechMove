using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TechMove.GLMS.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "glms");

            migrationBuilder.CreateTable(
                name: "Clients",
                schema: "glms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                schema: "glms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ServiceLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContractType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstimatedValueUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedValueZar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AgreementFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AgreementFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AgreementUploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgreementFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "glms",
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractStatusLogs",
                schema: "glms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    OldStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractStatusLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractStatusLogs_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "glms",
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                schema: "glms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OriginalCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OriginalCostUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExchangeRateUsed = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "glms",
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "glms",
                table: "Clients",
                columns: new[] { "Id", "ContactEmail", "ContactPerson", "ContactPhone", "CreatedAt", "Name", "Region" },
                values: new object[,]
                {
                    { 1, "johan.vandermerwe@maersk-sa.com", "Johan Van Der Merwe", "+27 31 555 0192", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Maersk Southern Africa", "Southern Africa (Durban Hub)" },
                    { 2, "nolwazi.khumalo@transnet.co.za", "Nolwazi Khumalo", "+27 11 888 4321", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Transnet Freight Rail", "Gauteng Corridor" },
                    { 3, "elena.rostova@dhl-global.com", "Elena Rostova", "+27 21 444 8765", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "DHL Global Forwarding", "Western Cape Maritime" },
                    { 4, "marcus.weber@msc-logistics.com", "Marcus Weber", "+41 22 703 8888", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Mediterranean Shipping Co.", "International (Europe/Geneva)" }
                });

            migrationBuilder.InsertData(
                schema: "glms",
                table: "Contracts",
                columns: new[] { "Id", "AgreementFileName", "AgreementFilePath", "AgreementFileSizeBytes", "AgreementUploadedAt", "ClientId", "ContractNumber", "ContractType", "CreatedAt", "EndDate", "EstimatedValueUsd", "EstimatedValueZar", "ServiceLevel", "StartDate", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "CON-2026-0001_Signed_SLA.pdf", "/uploads/contracts/Sample_Signed_Agreement.pdf", 845L, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1, "CON-2026-0001", "Premium", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 250000.00m, 4625000.00m, "Premium", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active", "Annual Durban-Joburg Intermodal Freight SLA", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, null, null, null, 2, "CON-2026-0002", "Standard", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), 120000.00m, 2220000.00m, "Standard", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active", "Regional Bulk Cargo Haulage Agreement", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, null, null, null, null, 3, "CON-2026-0003", "Express", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 95000.00m, 1757500.00m, "Express", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OnHold", "Cold Chain Pharmaceutical Distribution SLA", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, null, null, null, null, 4, "CON-2026-0004", "International", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 500000.00m, 9250000.00m, "International", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired", "Cross-Border Maritime Container Clearance", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, null, null, null, null, 3, "CON-2026-0005", "Express", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 30, 0, 0, 0, 0, DateTimeKind.Utc), 80000.00m, 1480000.00m, "Express", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Draft", "Special Cargo Air Charter Framework", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "glms",
                table: "ContractStatusLogs",
                columns: new[] { "Id", "ActionTaken", "ContractId", "Department", "NewStatus", "OldStatus", "Timestamp" },
                values: new object[,]
                {
                    { 1, "Allocated primary fleet transport corridors and assigned senior dispatch controllers.", 1, "Operations Department", "Active", "Draft", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Opened line of credit, configured billing ledger and currency exchange thresholds.", 1, "Finance Department", "Active", "Draft", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Dispatched official activation confirmation and portal credentials to client contact.", 1, "Client Services", "Active", "Draft", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Verified legal indemnities, international customs bonds, and SLA adherence criteria.", 1, "Compliance Department", "Active", "Draft", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "glms",
                table: "ServiceRequests",
                columns: new[] { "Id", "ContractId", "Cost", "CreatedDate", "Description", "Destination", "ExchangeRateUsed", "Origin", "OriginalCostUsd", "OriginalCurrency", "RequestNumber", "Status" },
                values: new object[,]
                {
                    { 1, 1, 64750.00m, new DateTime(2026, 2, 10, 8, 30, 0, 0, DateTimeKind.Utc), "Express Container Haulage (40ft Refrigerated - Durban to City Deep)", "City Deep Container Depot, JHB", 18.500000m, "Port of Durban Container Terminal", 3500.00m, "USD", "SR-2026-0001", "InProgress" },
                    { 2, 1, 22200.00m, new DateTime(2026, 2, 15, 11, 0, 0, 0, DateTimeKind.Utc), "Priority Customs Clearance and Port Transfer", "Bayhead Logistics Yard", 18.500000m, "Durban Point Pier 2", 1200.00m, "USD", "SR-2026-0002", "Completed" },
                    { 3, 2, 107300.00m, new DateTime(2026, 2, 20, 14, 15, 0, 0, DateTimeKind.Utc), "Bulk Iron Ore Rail Freight Escort & Inspection", "Saldanha Bulk Terminal", 18.500000m, "Sishen Mining Complex", 5800.00m, "USD", "SR-2026-0003", "Pending" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Name",
                schema: "glms",
                table: "Clients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ClientId",
                schema: "glms",
                table: "Contracts",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractNumber",
                schema: "glms",
                table: "Contracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_EndDate",
                schema: "glms",
                table: "Contracts",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_StartDate",
                schema: "glms",
                table: "Contracts",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_Status",
                schema: "glms",
                table: "Contracts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContractStatusLogs_ContractId",
                schema: "glms",
                table: "ContractStatusLogs",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ContractId",
                schema: "glms",
                table: "ServiceRequests",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_RequestNumber",
                schema: "glms",
                table: "ServiceRequests",
                column: "RequestNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractStatusLogs",
                schema: "glms");

            migrationBuilder.DropTable(
                name: "ServiceRequests",
                schema: "glms");

            migrationBuilder.DropTable(
                name: "Contracts",
                schema: "glms");

            migrationBuilder.DropTable(
                name: "Clients",
                schema: "glms");
        }
    }
}
