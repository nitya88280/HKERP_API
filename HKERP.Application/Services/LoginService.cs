using System.Diagnostics;
using HKERP.Application.DTOs;
using HKERP.Application.Interfaces;
using HKERP.Domain.Common;
using HKERP.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HKERP.Application.Services
{
    public class LoginService : ILoginService
    {
        private const string API_NAME = "Login";

        private readonly ILoginRepository _loginRepository;
        private readonly ITokenService _tokenService;
        private readonly IApiAccessRepository _accessRepository;
        private readonly IConfiguration _config;

        public LoginService(
            ILoginRepository loginRepository,
            ITokenService tokenService,
            IApiAccessRepository accessRepository,
            IConfiguration config)
        {
            _loginRepository = loginRepository;
            _tokenService = tokenService;
            _accessRepository = accessRepository;
            _config = config;
        }

        public ApiResponseDto Login(LoginRequestDto request)
        {
            var sw = Stopwatch.StartNew();

            string securityKey = _config["SecurityKey"];
            string encryptedPassword = CommonMethods.Encrypt(request.Password, true, securityKey);

            var ds = _loginRepository.ValidateUser(
                request.UserName, encryptedPassword, request.DeviceType, request.IP);

            if (ds.Tables.Count > 0 &&
                ds.Tables[0].Columns.Contains("Success") &&
                ds.Tables[0].Rows.Count > 0 &&
                Convert.ToBoolean(ds.Tables[0].Rows[0]["Success"]) == false)
            {
                string spMessage = ds.Tables[0].Columns.Contains("Message")
                    ? ds.Tables[0].Rows[0]["Message"]?.ToString()
                    : "Login failed";

                return new ApiResponseDto { Success = false, Message = spMessage };
            }

            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            {
                return new ApiResponseDto { Success = false, Message = "Password Or Username Incorrect !" };
            }

            var resultList = CommonMethods.ConvertDataTableToList(ds.Tables[0]);
            var firstRow = resultList[0];

            string userId = firstRow.ContainsKey("HstLoginID") ? firstRow["HstLoginID"]?.ToString() : null;
            string ledgerIdStr = firstRow.ContainsKey("Ledger_ID") ? firstRow["Ledger_ID"]?.ToString() : null;

            if (!Guid.TryParse(ledgerIdStr, out Guid ledgerId))
            {
                return new ApiResponseDto { Success = false, Message = "User record me valid Ledger_ID nahi mila" };
            }

            var nowIst = DateTimeHelper.GetIstNow();

            var access = _accessRepository.GetAccessControl(ledgerId, API_NAME);
            if (access == null)
            {
                return new ApiResponseDto { Success = false, Message = "Is user ko Login API ka access nahi diya gaya hai" };
            }

            var currentTime = nowIst.TimeOfDay;
            if (currentTime < access.AllowedFromTime || currentTime > access.AllowedToTime)
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = $"Login sirf {access.AllowedFromTime} se {access.AllowedToTime} IST ke beech ho sakta hai"
                };
            }

            int todayCount = _accessRepository.GetTodaySuccessCallCount(ledgerId, API_NAME, nowIst);
            if (todayCount >= access.MaxCallsPerDay)
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = $"Aaj ke liye maximum {access.MaxCallsPerDay} login attempts ki limit khatam ho gayi hai"
                };
            }

            string token = _tokenService.GenerateToken(userId, request.UserName, ledgerId.ToString());

            sw.Stop();

            var log = new ApiAccessLog
            {
                Ledger_ID = ledgerId,
                ApiName = API_NAME,
                CallDateIST = nowIst.Date,
                StartTimeIST = nowIst,
                EndTimeIST = DateTimeHelper.GetIstNow(),
                ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                IsSuccess = true,
                Message = "Login Successful"
            };
            _accessRepository.LogAccessStart(log);

            return new ApiResponseDto
            {
                Success = true,
                Message = "Login Successful",
                Data = new { Token = token, UserDetails = resultList }
            };
        }
    }
}
