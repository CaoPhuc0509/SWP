using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_data.Interfaces;
using eyewearshop_service.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace eyewearshop_service.Return
{
    public class ReturnService : IReturnService
    {
        private readonly IRepository<ReturnRequest> _returnRepository;

        public ReturnService(IRepository<ReturnRequest> returnRepository)
        {
            _returnRepository = returnRepository;
        }

        public async Task ChangeReturnStatusAsync(long returnId, short newStatus, string role)
        {
            var request = await _returnRepository
                .Query()
                .FirstOrDefaultAsync(r => r.ReturnRequestId == returnId);

            if (request == null)
                throw new Exception("Return request not found");

            if (!IsValidTransition(request.Status, newStatus, role))
                throw new Exception("You are not allowed to change this return status");

            request.Status = newStatus;

            await _returnRepository.SaveChangesAsync();
        }

        public async Task<object> GetAllReturnRequestsAsync(
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var query = _returnRepository
                .Query()
                .AsNoTracking();

            var total = await query.CountAsync(ct);

            var requests = await query
                .OrderByDescending(rr => rr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(rr => new
                {
                    rr.ReturnRequestId,
                    rr.RequestNumber,
                    rr.RequestType,
                    rr.Status,
                    rr.Reason,
                    rr.Description,
                    rr.CustomerId,
                    rr.CreatedAt,
                    rr.UpdatedAt,
                    Order = new
                    {
                        rr.Order.OrderId,
                        rr.Order.OrderNumber,
                        rr.Order.OrderType
                    },
                    ExchangeOrder = rr.ExchangeOrder == null ? null : new
                    {
                        rr.ExchangeOrder.OrderId,
                        rr.ExchangeOrder.OrderNumber
                    }
                })
                .ToListAsync(ct);

            return new
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = requests
            };
        }

        private bool IsValidTransition(short currentStatus, short newStatus, string role)
        {
            if (role != RoleNames.Admin)
                return false;
            if (currentStatus == ReturnRequestStatuses.Requested &&
            (newStatus == ReturnRequestStatuses.Approved ||
                 newStatus == ReturnRequestStatuses.Rejected))
                return true;

            if (currentStatus == ReturnRequestStatuses.Approved &&
                newStatus == ReturnRequestStatuses.Completed)
                return true;

            return false;
        }
    }
}
