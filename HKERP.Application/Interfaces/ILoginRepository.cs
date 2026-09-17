using System.Data;

namespace HKERP.Application.Interfaces
{
    public interface ILoginRepository
    {
        DataSet ValidateUser(string username, string encryptedPassword, string deviceType, string ip);
    }
}
