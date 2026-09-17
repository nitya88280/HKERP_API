using System.Diagnostics;
using HKERP.Application.Interfaces;
using HKERP.Domain.Common;
using HKERP.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HKERP.API.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ApiAccessControlAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _apiName;

        public ApiAccessControlAttribute(string apiName)
        {
            _apiName = apiName;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var repo = context.HttpContext.RequestServices.GetService(typeof(IApiAccessRepository)) as IApiAccessRepository;

            var ledgerIdClaim = context.HttpContext.User.FindFirst("LedgerId")?.Value;
            if (string.IsNullOrEmpty(ledgerIdClaim) || !Guid.TryParse(ledgerIdClaim, out Guid ledgerId))
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Invalid or missing token - LedgerId not found" });
                return;
            }

            var nowIst = DateTimeHelper.GetIstNow();

            var access = repo.GetAccessControl(ledgerId, _apiName);
            if (access == null)
            {
                context.Result = new ObjectResult(new { success = false, message = "Access not allowed for this API" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            var currentTime = nowIst.TimeOfDay;
            if (currentTime < access.AllowedFromTime || currentTime > access.AllowedToTime)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = $"API sirf {access.AllowedFromTime} se {access.AllowedToTime} IST ke beech access ho sakti hai"
                })
                { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }

            int todayCount = repo.GetTodaySuccessCallCount(ledgerId, _apiName, nowIst);
            if (todayCount >= access.MaxCallsPerDay)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = $"Aaj ke liye maximum {access.MaxCallsPerDay} calls ki limit khatam ho gayi hai"
                })
                { StatusCode = StatusCodes.Status429TooManyRequests };
                return;
            }

            var log = new ApiAccessLog
            {
                Ledger_ID = ledgerId,
                ApiName = _apiName,
                CallDateIST = nowIst.Date,
                StartTimeIST = nowIst,
                IsSuccess = false
            };
            long logId = repo.LogAccessStart(log);

            var sw = Stopwatch.StartNew();
            var executedContext = await next();
            sw.Stop();

            bool success = executedContext.Exception == null;
            var endTimeIst = DateTimeHelper.GetIstNow();

            repo.LogAccessEnd(
                logId,
                endTimeIst,
                (int)sw.ElapsedMilliseconds,
                success,
                success ? "Success" : executedContext.Exception?.Message
            );
        }
    }
}
