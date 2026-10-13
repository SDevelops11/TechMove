using Microsoft.EntityFrameworkCore;
using TechMove.GLMS.Web.Data;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Repositories
{
    public class ContractRepository : IContractRepository
    {
        private readonly AppDbContext _context;

        public ContractRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Contract>> GetAllAsync()
        {
            return await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Contract>> FilterContractsAsync(
            DateTime? startDate,
            DateTime? endDate,
            ContractStatus? status,
            int? clientId,
            string? searchTerm)
        {
            // LINQ query pipeline
            var query = _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(c => c.StartDate >= startUtc);
            }

            if (endDate.HasValue)
            {
                var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(c => c.EndDate <= endUtc);
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            if (clientId.HasValue && clientId.Value > 0)
            {
                query = query.Where(c => c.ClientId == clientId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(c =>
                    c.ContractNumber.Contains(term) ||
                    c.Title.Contains(term) ||
                    (c.Client != null && c.Client.Name.Contains(term)) ||
                    c.ContractType.Contains(term));
            }

            return await query.OrderByDescending(c => c.StartDate).ToListAsync();
        }

        public async Task<Contract?> GetByIdAsync(int id, bool includeDetails = false)
        {
            var query = _context.Contracts.AsQueryable();

            if (includeDetails)
            {
                query = query
                    .Include(c => c.Client)
                    .Include(c => c.ServiceRequests)
                    .Include(c => c.StatusLogs.OrderByDescending(l => l.Timestamp));
            }
            else
            {
                query = query.Include(c => c.Client);
            }

            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Contract?> GetByContractNumberAsync(string contractNumber)
        {
            return await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.ContractNumber == contractNumber);
        }

        public async Task AddAsync(Contract contract)
        {
            await _context.Contracts.AddAsync(contract);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Contract contract)
        {
            contract.UpdatedAt = DateTime.UtcNow;
            _context.Contracts.Update(contract);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Contracts.AnyAsync(c => c.Id == id);
        }
    }
}
