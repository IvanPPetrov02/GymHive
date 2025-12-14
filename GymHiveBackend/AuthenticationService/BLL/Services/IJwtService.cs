using BLL.Entities;

namespace BLL.Services;

public interface IJwtService
{
    string GenerateJwtToken(User user);
}
