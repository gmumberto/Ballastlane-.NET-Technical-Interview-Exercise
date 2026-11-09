using TaskManagement.Application.DTOs;
using User = TaskManagement.Domain.Entities.User;

namespace TaskManagement.Application.Interfaces;

public interface IJwtTokenGenerator
{
    AuthResponseDto GenerateToken(User user);
}