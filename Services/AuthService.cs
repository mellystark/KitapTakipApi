using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using KitapTakipApi.Data;
using KitapTakipApi.Dtos;
using KitapTakipApi.Models.Responses;
using KitapTakipApi.Models;
using KitapTakipApi.Services.Interfaces;

namespace KitapTakipApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> RegisterAsync(RegisterDto registerDto)
    {
        if (string.IsNullOrEmpty(registerDto.UserName) || string.IsNullOrEmpty(registerDto.Email) || string.IsNullOrEmpty(registerDto.Password))
            return new ApiResponse<string> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };

        // Kullanıcı adı veya e-posta zaten var mı?
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == registerDto.UserName || u.Email == registerDto.Email);
        if (existingUser != null)
            return new ApiResponse<string> { Success = false, Message = "Kullanıcı adı veya e-posta zaten kullanılıyor." };

        var user = new User
        {
            Id = Guid.NewGuid().ToString(), // Benzersiz ID oluştur
            UserName = registerDto.UserName,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
        };

        _logger.LogInformation($"Kullanıcı kaydediliyor: UserName={registerDto.UserName}, UserId={user.Id}");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new ApiResponse<string> { Success = true, Message = "Kullanıcı kaydedildi." };
    }

    public async Task<ApiResponse<string>> LoginAsync(LoginDto loginDto)
    {
        if (string.IsNullOrEmpty(loginDto.UserNameOrEmail) || string.IsNullOrEmpty(loginDto.Password))
            return new ApiResponse<string> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == loginDto.UserNameOrEmail || u.Email == loginDto.UserNameOrEmail);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return new ApiResponse<string> { Success = false, Message = "Kullanıcı adı/e-posta veya şifre yanlış." };

        _logger.LogInformation($"Kullanıcı giriş yaptı: UserName={user.UserName}, UserId={user.Id}");

        var token = GenerateJwtToken(user);
        return new ApiResponse<string> { Success = true, Data = token, Message = "Giriş başarılı." };
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(ChangePasswordDto changePasswordDto, string userId)
    {
        if (string.IsNullOrEmpty(changePasswordDto.CurrentPassword) || string.IsNullOrEmpty(changePasswordDto.NewPassword))
            return new ApiResponse<string> { Success = false, Message = "Mevcut ve yeni şifre alanları zorunludur." };

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return new ApiResponse<string> { Success = false, Message = $"Kullanıcı bulunamadı: userId={userId}" };

        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            return new ApiResponse<string> { Success = false, Message = "Mevcut şifre yanlış." };

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Şifre değiştirildi: UserId={userId}");
        return new ApiResponse<string> { Success = true, Message = "Şifre başarıyla değiştirildi." };
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        _logger.LogInformation($"JWT Token oluşturuluyor: UserName={user.UserName}, UserId={user.Id}, ClaimTypes.NameIdentifier={user.Id}");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}