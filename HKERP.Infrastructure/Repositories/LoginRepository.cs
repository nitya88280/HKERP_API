using HKERP.Application.Interfaces;
using HKERP.Infrastructure.Common;
using System.Data;

namespace HKERP.Infrastructure.Repositories
{
    public class LoginRepository : ILoginRepository
    {
        private readonly ISqlHelper _sqlHelper;

        public LoginRepository(ISqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public DataSet ValidateUser(string username, string encryptedPassword, string deviceType, string ip)
        {
            string xml = "<DocumentElement>" +
                            "<username>" + username + "</username>" +
                            "<password>" + encryptedPassword + "</password>" +
                            "<DeviceType>" + deviceType + "</DeviceType>" +
                            "<IP>" + ip + "</IP>" +
                         "</DocumentElement>";

            return _sqlHelper.ExecuteSpWithXml(xml, "HKERP_USER_LOGIN");
        }
    }
}
