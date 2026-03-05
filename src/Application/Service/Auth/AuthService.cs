using Application.DTOs.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Service.Auth;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<bool> LogoutAsync(string userId);
}

public class AuthService : IAuthService
{
    private readonly ILdapAuthService _ldapAuthService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ILdapAuthService ldapAuthService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _ldapAuthService = ldapAuthService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Result<LoginResponseDto>.Failure("Email and password are required");

            // Validate email format
            if (!IsValidEmail(request.Email))
                return Result<LoginResponseDto>.Failure("Invalid email format");

            // LDAP-only authentication for all environments.
            var isAuthenticated = await _ldapAuthService.AuthenticateAsync(request.Email, request.Password);

            if (!isAuthenticated)
                return Result<LoginResponseDto>.Failure("اسم المستخدم / كلمة المرور غير صحيحة"); // Invalid username or password

            // Read user profile and active flag from LDAP only.
            var (firstName, lastName, department, personNumber, mobileNumber, isActive) =
                await _ldapAuthService.GetUserDetailsAsync(request.Email);

            if (!isActive)
            {
                _logger.LogWarning("Login attempt by inactive LDAP user {Email}", request.Email);
                return Result<LoginResponseDto>.Failure("اسم المستخدم غير موجود أو غير نشط");
            }

            var usernameFromEmail = request.Email.Split('@')[0];
            var safePersonNumber = !string.IsNullOrWhiteSpace(personNumber)
                ? personNumber
                : usernameFromEmail;

            var fullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = usernameFromEmail;
            }
            var role = "User";

            // Generate JWT token
            var token = GenerateJwtToken(
                userId: safePersonNumber,
                email: request.Email,
                fullName: fullName,
                role: role,
                department: department,
                personNumber: safePersonNumber,
                mobileNumber: mobileNumber);

            var response = new LoginResponseDto
            {
                UserId = safePersonNumber,
                Email = request.Email,
                FullName = fullName,
                PersonNumber = safePersonNumber,
                Role = role,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _logger.LogInformation("User {Email} logged in successfully", request.Email);
            return Result<LoginResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", request.Email);
            return Result<LoginResponseDto>.Failure("An error occurred during login. Please try again.");
        }
    }

    public async Task<bool> LogoutAsync(string userId)
    {
        try
        {
            _logger.LogInformation("User {UserId} logged out", userId);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error for user {UserId}", userId);
            return false;
        }
    }

    private string GenerateJwtToken(
        string userId,
        string email,
        string fullName,
        string role,
        string? department,
        string? personNumber,
        string? mobileNumber)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role),
            new Claim("Department", department ?? ""),
            new Claim("PersonNumber", personNumber ?? ""),
            new Claim(ClaimTypes.MobilePhone, mobileNumber ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
