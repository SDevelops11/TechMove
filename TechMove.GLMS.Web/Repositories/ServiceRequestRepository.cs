using Microsoft.EntityFrameworkCore;
using TechMove.GLMS.Web.Data;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Repositories
{
    public interface IServiceRequestRepository
    {
        Task<List<ServiceRequest>> GetAllAsync();
        Task<List<ServiceRequest>> GetByContractIdAsync(int contractId);
        Task<ServiceRequest?> GetByIdAsync(int id);
        Task AddAsync(ServiceRequest request);
        Task UpdateAsync(ServiceRequest request);
        Task DeleteAsync(int id);
    }

    public class ServiceRequestRepository : IServiceRequestRepository
    {
        private readonly AppDbContext _context;

        public ServiceRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceRequest>> GetAllAsync()
        {
            return await _context.ServiceRequests
                .Include(r => r.Contract)
                    .ThenInclude(c => c!.Client)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<ServiceRequest>> GetByContractIdAsync(int contractId)
        {
            return await _context.ServiceRequests
                .Where(r => r.ContractId == contractId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<ServiceRequest?> GetByIdAsync(int id)
        {
            return await _context.ServiceRequests
                .Include(r => r.Contract)
                    .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task AddAsync(ServiceRequest request)
        {
            await _context.ServiceRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ServiceRequest request)
        {
            _context.ServiceRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.ServiceRequests.FindAsync(id);
            if (entity != null)
            {
                _context.ServiceRequests.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
