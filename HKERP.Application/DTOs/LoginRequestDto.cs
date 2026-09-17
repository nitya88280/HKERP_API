namespace HKERP.Application.DTOs
{
    public class LoginRequestDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DeviceType { get; set; }
        public string IP { get; set; }
    }
}
