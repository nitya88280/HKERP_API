using HKERP.Application.Interfaces;
using HKERP.Domain.Entities;
using HKERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HKERP.Infrastructure.Repositories
{
    public class ApiAccessRepository : IApiAccessRepository
    {
        private readonly ControlDbContext _context;

        public ApiAccessRepository(ControlDbContext context)
        {
            _context = context;
        }

        public ApiAccessControl GetAccessControl(Guid ledgerId, string apiName)
        {
            return _context.ApiAccessControls
                .FirstOrDefault(x => x.Ledger_ID == ledgerId && x.ApiName == apiName && x.IsActive);
        }

        public int GetTodaySuccessCallCount(Guid ledgerId, string apiName, DateTime istDate)
        {
            var dateOnly = istDate.Date;
            return _context.ApiAccessLogs
                .Count(x => x.Ledger_ID == ledgerId
                         && x.ApiName == apiName
                         && x.CallDateIST == dateOnly
                         && x.IsSuccess);
        }

        public long LogAccessStart(ApiAccessLog log)
        {
            _context.ApiAccessLogs.Add(log);
            _context.SaveChanges();
            return log.Id;
        }

        public void LogAccessEnd(long logId, DateTime endTimeIst, int responseTimeMs, bool isSuccess, string message)
        {
            var log = _context.ApiAccessLogs.FirstOrDefault(x => x.Id == logId);
            if (log != null)
            {
                log.EndTimeIST = endTimeIst;
                log.ResponseTimeMs = responseTimeMs;
                log.IsSuccess = isSuccess;
                log.Message = message;
                _context.SaveChanges();
            }
        }
    }
}
