using HKERP.Application.DTOs;

namespace HKERP.Application.Interfaces
{
    public interface ILoginService
    {
        ApiResponseDto Login(LoginRequestDto request);
    }
}
