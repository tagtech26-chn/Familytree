using FamilyConnect.Application.Contracts;
using FamilyConnect.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FamilyConnect.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        try { return Accepted(await auth.RegisterAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new ApiError("VALIDATION_ERROR", ex.Message)); }
        catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_ALREADY_EXISTS")
        { return Conflict(new ApiError("ACCOUNT_ALREADY_EXISTS", "An account already exists for this mobile number or email.")); }
        catch (InvalidOperationException ex) when (ex.Message == "OTP_RATE_LIMITED")
        { return StatusCode(429, new ApiError("OTP_RATE_LIMITED", "OTP request limit reached. Try later.")); }
    }

    [HttpPost("otp/verify")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request, CancellationToken ct)
    {
        try { return Ok(await auth.VerifyOtpAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new ApiError("VALIDATION_ERROR", ex.Message)); }
        catch (InvalidOperationException) { return Unauthorized(new ApiError("INVALID_OR_EXPIRED_OTP", "OTP is invalid, expired or already used.")); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        try { return Ok(await auth.LoginAsync(request, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(new ApiError("INVALID_CREDENTIALS", "Invalid credentials or unverified account.")); }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        try { return Ok(await auth.RefreshAsync(request.RefreshToken, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(new ApiError("INVALID_REFRESH_TOKEN", "Refresh token is invalid, expired or revoked.")); }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await auth.RevokeRefreshAsync(request.RefreshToken, ct);
        return NoContent();
    }
}
