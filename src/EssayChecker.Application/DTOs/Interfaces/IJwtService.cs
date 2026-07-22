using EssayChecker.Domain.Entities.Users;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface IJwtService
{
    (string Token, DateTime ExpiresAt) GenerateToken(AppUser user);
}
