using HKERP.Domain.Entities;

namespace HKERP.Application.Interfaces
{
    public interface IApiAccessRepository
    {
        ApiAccessControl GetAccessControl(Guid ledgerId, string apiName);
        int GetTodaySuccessCallCount(Guid ledgerId, string apiName, DateTime istDate);
        long LogAccessStart(ApiAccessLog log);
        void LogAccessEnd(long logId, DateTime endTimeIst, int responseTimeMs, bool isSuccess, string message);
    }
}
