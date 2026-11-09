using Microsoft.AspNetCore.Authorization;
using TaskManagement.Application.Common;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Provides user registration and authentication operations.
/// </summary>
/// <remarks>
/// These endpoints are publicly accessible and do not
/// require JWT authentication.
/// </remarks>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <remarks>
    /// Creates a user account using a username,
    /// email address and password.
    ///
    /// The password must satisfy the configured
    /// validation requirements.
    ///
    /// Returns an authentication response
    /// containing a JWT access token.
    /// </remarks>
    /// <param name="request">User registration data.</param>
    /// <param name="cancellationToken">
    /// Request cancellation token.
    /// </param>
    /// <returns>
    /// Authentication information for the registered user.
    /// </returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">Invalid registration data.</response>
    /// <response code="409">Email address already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(
            request,
            cancellationToken);

        if (!result.IsSuccess) return ResultHttpMapper.ToProblem(result.Error!, HttpContext);
        _logger.LogInformation("User registered successfully.");
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Authenticates a registered user.
    /// </summary>
    /// <remarks>
    /// Validates the supplied email address and password.
    ///
    /// Returns a JWT access token when authentication succeeds.
    ///
    /// Use the returned accessToken with the Swagger
    /// Authorize button to access protected endpoints.
    /// </remarks>
    /// <param name="request">User login credentials.</param>
    /// <param name="cancellationToken">
    /// Request cancellation token.
    /// </param>
    /// <returns>
    /// Authentication response containing the JWT access token.
    /// </returns>
    /// <response code="200">Authentication successful.</response>
    /// <response code="400">Invalid login request.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            request,
            cancellationToken);

        if (!result.IsSuccess) return ResultHttpMapper.ToProblem(result.Error!, HttpContext);
        _logger.LogInformation("User authenticated successfully.");
        return Ok(result.Value);
    }
}
