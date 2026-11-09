using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
namespace TaskManagement.Application.Interfaces;
public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
}
