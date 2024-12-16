using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Models.Dto;

public class LoginResponseDto
{
    public string Email { get; set; }
    public string AuthToken { get; set; }
    public string Role { get; set; }
    public int BusinessId { get; set; }
    public string Currency { get; set; }
}