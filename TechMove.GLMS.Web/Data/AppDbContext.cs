using Microsoft.EntityFrameworkCore;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
        public DbSet<ContractStatusLog> ContractStatusLogs => Set<ContractStatusLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // Client Configuration
            // ==========================================
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
                entity.Property(c => c.ContactPerson).IsRequired().HasMaxLength(100);
                entity.Property(c => c.ContactEmail).IsRequired().HasMaxLength(150);
                entity.Property(c => c.ContactPhone).IsRequired().HasMaxLength(30);
                entity.Property(c => c.Region).IsRequired().HasMaxLength(100);
                entity.HasIndex(c => c.Name);
            });

            // ==========================================
            // Contract Configuration
            // ==========================================
            modelBuilder.Entity<Contract>(entity =>
            {
                entity.ToTable("Contracts");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.ContractNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(c => c.ContractNumber).IsUnique();

                entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Status).IsRequired().HasConversion<string>();
                entity.Property(c => c.ServiceLevel).IsRequired().HasConversion<string>();
                entity.Property(c => c.ContractType).IsRequired().HasMaxLength(50);

                entity.Property(c => c.EstimatedValueUsd).HasColumnType("decimal(18,2)");
                entity.Property(c => c.EstimatedValueZar).HasColumnType("decimal(18,2)");

                entity.Property(c => c.AgreementFileName).HasMaxLength(255);
                entity.Property(c => c.AgreementFilePath).HasMaxLength(500);

                // Relationship: Client -> Contracts (1 to Many)
                entity.HasOne(c => c.Client)
                      .WithMany(cl => cl.Contracts)
                      .HasForeignKey(c => c.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(c => c.Status);
                entity.HasIndex(c => c.StartDate);
                entity.HasIndex(c => c.EndDate);
            });

            // ==========================================
            // ServiceRequest Configuration
            // ==========================================
            modelBuilder.Entity<ServiceRequest>(entity =>
            {
                entity.ToTable("ServiceRequests");
                entity.HasKey(sr => sr.Id);
                entity.Property(sr => sr.RequestNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(sr => sr.RequestNumber).IsUnique();

                entity.Property(sr => sr.Description).IsRequired().HasMaxLength(500);
                entity.Property(sr => sr.OriginalCurrency).IsRequired().HasMaxLength(10);
                entity.Property(sr => sr.OriginalCostUsd).HasColumnType("decimal(18,2)");
                entity.Property(sr => sr.ExchangeRateUsed).HasColumnType("decimal(18,6)");
                entity.Property(sr => sr.Cost).HasColumnType("decimal(18,2)");
                entity.Property(sr => sr.Status).IsRequired().HasConversion<string>();

                // Relationship: Contract -> ServiceRequests (1 to Many)
                entity.HasOne(sr => sr.Contract)
                      .WithMany(c => c.ServiceRequests)
                      .HasForeignKey(sr => sr.ContractId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // ContractStatusLog Configuration
            // ==========================================
            modelBuilder.Entity<ContractStatusLog>(entity =>
            {
                entity.ToTable("ContractStatusLogs");
                entity.HasKey(log => log.Id);
                entity.Property(log => log.OldStatus).IsRequired().HasMaxLength(50);
                entity.Property(log => log.NewStatus).IsRequired().HasMaxLength(50);
                entity.Property(log => log.Department).IsRequired().HasMaxLength(100);
                entity.Property(log => log.ActionTaken).IsRequired().HasMaxLength(500);

                entity.HasOne(log => log.Contract)
                      .WithMany(c => c.StatusLogs)
                      .HasForeignKey(log => log.ContractId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed Initial Data
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Client>().HasData(
                new Client
                {
                    Id = 1,
                    Name = "Maersk Southern Africa",
                    ContactPerson = "Johan Van Der Merwe",
                    ContactEmail = "johan.vandermerwe@maersk-sa.com",
                    ContactPhone = "+27 31 555 0192",
                    Region = "Southern Africa (Durban Hub)",
                    CreatedAt = seedDate
                },
                new Client
                {
                    Id = 2,
                    Name = "Transnet Freight Rail",
                    ContactPerson = "Nolwazi Khumalo",
                    ContactEmail = "nolwazi.khumalo@transnet.co.za",
                    ContactPhone = "+27 11 888 4321",
                    Region = "Gauteng Corridor",
                    CreatedAt = seedDate
                },
                new Client
                {
                    Id = 3,
                    Name = "DHL Global Forwarding",
                    ContactPerson = "Elena Rostova",
                    ContactEmail = "elena.rostova@dhl-global.com",
                    ContactPhone = "+27 21 444 8765",
                    Region = "Western Cape Maritime",
                    CreatedAt = seedDate
                },
                new Client
                {
                    Id = 4,
                    Name = "Mediterranean Shipping Co.",
                    ContactPerson = "Marcus Weber",
                    ContactEmail = "marcus.weber@msc-logistics.com",
                    ContactPhone = "+41 22 703 8888",
                    Region = "International (Europe/Geneva)",
                    CreatedAt = seedDate
                }
            );

            modelBuilder.Entity<Contract>().HasData(
                new Contract
                {
                    Id = 1,
                    ContractNumber = "CON-2026-0001",
                    Title = "Annual Durban-Joburg Intermodal Freight SLA",
                    ClientId = 1,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    Status = ContractStatus.Active,
                    ServiceLevel = ServiceLevel.Premium,
                    ContractType = "Premium",
                    EstimatedValueUsd = 250000.00m,
                    EstimatedValueZar = 4625000.00m,
                    AgreementFileName = "CON-2026-0001_Signed_SLA.pdf",
                    AgreementFilePath = "/uploads/contracts/Sample_Signed_Agreement.pdf",
                    AgreementUploadedAt = seedDate,
                    AgreementFileSizeBytes = 845L,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contract
                {
                    Id = 2,
                    ContractNumber = "CON-2026-0002",
                    Title = "Regional Bulk Cargo Haulage Agreement",
                    ClientId = 2,
                    StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
                    Status = ContractStatus.Active,
                    ServiceLevel = ServiceLevel.Standard,
                    ContractType = "Standard",
                    EstimatedValueUsd = 120000.00m,
                    EstimatedValueZar = 2220000.00m,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contract
                {
                    Id = 3,
                    ContractNumber = "CON-2026-0003",
                    Title = "Cold Chain Pharmaceutical Distribution SLA",
                    ClientId = 3,
                    StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2027, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                    Status = ContractStatus.OnHold,
                    ServiceLevel = ServiceLevel.Express,
                    ContractType = "Express",
                    EstimatedValueUsd = 95000.00m,
                    EstimatedValueZar = 1757500.00m,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contract
                {
                    Id = 4,
                    ContractNumber = "CON-2026-0004",
                    Title = "Cross-Border Maritime Container Clearance",
                    ClientId = 4,
                    StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    Status = ContractStatus.Expired,
                    ServiceLevel = ServiceLevel.International,
                    ContractType = "International",
                    EstimatedValueUsd = 500000.00m,
                    EstimatedValueZar = 9250000.00m,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc)
                },
                new Contract
                {
                    Id = 5,
                    ContractNumber = "CON-2026-0005",
                    Title = "Special Cargo Air Charter Framework",
                    ClientId = 3,
                    StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc),
                    Status = ContractStatus.Draft,
                    ServiceLevel = ServiceLevel.Express,
                    ContractType = "Express",
                    EstimatedValueUsd = 80000.00m,
                    EstimatedValueZar = 1480000.00m,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                }
            );

            modelBuilder.Entity<ServiceRequest>().HasData(
                new ServiceRequest
                {
                    Id = 1,
                    RequestNumber = "SR-2026-0001",
                    ContractId = 1,
                    Description = "Express Container Haulage (40ft Refrigerated - Durban to City Deep)",
                    Origin = "Port of Durban Container Terminal",
                    Destination = "City Deep Container Depot, JHB",
                    OriginalCurrency = "USD",
                    OriginalCostUsd = 3500.00m,
                    ExchangeRateUsed = 18.500000m,
                    Cost = 64750.00m,
                    Status = ServiceRequestStatus.InProgress,
                    CreatedDate = new DateTime(2026, 2, 10, 8, 30, 0, DateTimeKind.Utc)
                },
                new ServiceRequest
                {
                    Id = 2,
                    RequestNumber = "SR-2026-0002",
                    ContractId = 1,
                    Description = "Priority Customs Clearance and Port Transfer",
                    Origin = "Durban Point Pier 2",
                    Destination = "Bayhead Logistics Yard",
                    OriginalCurrency = "USD",
                    OriginalCostUsd = 1200.00m,
                    ExchangeRateUsed = 18.500000m,
                    Cost = 22200.00m,
                    Status = ServiceRequestStatus.Completed,
                    CreatedDate = new DateTime(2026, 2, 15, 11, 0, 0, DateTimeKind.Utc)
                },
                new ServiceRequest
                {
                    Id = 3,
                    RequestNumber = "SR-2026-0003",
                    ContractId = 2,
                    Description = "Bulk Iron Ore Rail Freight Escort & Inspection",
                    Origin = "Sishen Mining Complex",
                    Destination = "Saldanha Bulk Terminal",
                    OriginalCurrency = "USD",
                    OriginalCostUsd = 5800.00m,
                    ExchangeRateUsed = 18.500000m,
                    Cost = 107300.00m,
                    Status = ServiceRequestStatus.Pending,
                    CreatedDate = new DateTime(2026, 2, 20, 14, 15, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<ContractStatusLog>().HasData(
                new ContractStatusLog
                {
                    Id = 1,
                    ContractId = 1,
                    OldStatus = "Draft",
                    NewStatus = "Active",
                    Department = "Operations Department",
                    ActionTaken = "Allocated primary fleet transport corridors and assigned senior dispatch controllers.",
                    Timestamp = seedDate
                },
                new ContractStatusLog
                {
                    Id = 2,
                    ContractId = 1,
                    OldStatus = "Draft",
                    NewStatus = "Active",
                    Department = "Finance Department",
                    ActionTaken = "Opened line of credit, configured billing ledger and currency exchange thresholds.",
                    Timestamp = seedDate
                },
                new ContractStatusLog
                {
                    Id = 3,
                    ContractId = 1,
                    OldStatus = "Draft",
                    NewStatus = "Active",
                    Department = "Client Services",
                    ActionTaken = "Dispatched official activation confirmation and portal credentials to client contact.",
                    Timestamp = seedDate
                },
                new ContractStatusLog
                {
                    Id = 4,
                    ContractId = 1,
                    OldStatus = "Draft",
                    NewStatus = "Active",
                    Department = "Compliance Department",
                    ActionTaken = "Verified legal indemnities, international customs bonds, and SLA adherence criteria.",
                    Timestamp = seedDate
                }
            );
        }
    }
}
