using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eyewearshop_service.Interfaces
{
    public interface IReturnService
    {
        Task ChangeReturnStatusAsync(long returnId, short newStatus, string role);
        
        Task<object> GetAllReturnRequestsAsync(
            int page,
            int pageSize,
            CancellationToken ct = default);
    }
}
