namespace HKERP.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(string userId, string userName, string ledgerId);
    }
}
