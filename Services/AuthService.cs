using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;
using KitapTakipApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KitapTakipApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<ApiResponse<string>> RegisterAsync(RegisterDto registerDto)
        {
            if (string.IsNullOrEmpty(registerDto.Username) || string.IsNullOrEmpty(registerDto.Email) || string.IsNullOrEmpty(registerDto.Password))
                return new ApiResponse<string> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };

            var user = new IdentityUser { UserName = registerDto.Username, Email = registerDto.Email };
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
                return new ApiResponse<string> { Success = false, Message = string.Join(", ", result.Errors.Select(e => e.Description)) };

            return new ApiResponse<string> { Success = true, Message = "Kullanıcı kaydedildi." };
        }

        public async Task<ApiResponse<string>> LoginAsync(LoginDto loginDto)
        {
            if (string.IsNullOrEmpty(loginDto.Username) || string.IsNullOrEmpty(loginDto.Password))
                return new ApiResponse<string> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };

            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
                return new ApiResponse<string> { Success = false, Message = "Kullanıcı adı veya şifre yanlış." };

            var token = GenerateJwtToken(user);
            return new ApiResponse<string> { Success = true, Data = token, Message = "Giriş başarılı." };
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(ChangePasswordDto changePasswordDto, string userId)
        {
            if (string.IsNullOrEmpty(changePasswordDto.CurrentPassword) || string.IsNullOrEmpty(changePasswordDto.NewPassword))
                return new ApiResponse<string> { Success = false, Message = "Mevcut ve yeni şifre alanları zorunludur." };

            Console.WriteLine($"ChangePasswordAsync called with userId: {userId}");

            // Önce userId ile kullanıcıyı ara
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                // userId bir kullanıcı adı olabilir, o yüzden kullanıcı adına göre de ara
                Console.WriteLine($"User not found by Id: {userId}. Trying to find by username.");
                user = await _userManager.FindByNameAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"User not found by username: {userId}");
                    return new ApiResponse<string> { Success = false, Message = $"Kullanıcı bulunamadı: userId={userId}" };
                }
                Console.WriteLine($"User found by username: {userId}, actual Id: {user.Id}");
            }

            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, changePasswordDto.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                Console.WriteLine("Current password is invalid.");
                return new ApiResponse<string> { Success = false, Message = "Mevcut şifre yanlış." };
            }

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Password change failed: {errors}");
                return new ApiResponse<string> { Success = false, Message = $"Şifre değiştirme işlemi başarısız: {errors}" };
            }

            Console.WriteLine("Password changed successfully.");
            return new ApiResponse<string> { Success = true, Message = "Şifre başarıyla değiştirildi." };
        }

        private string GenerateJwtToken(IdentityUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id) // user.Id, GUID formatında olmalı
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine($"Generated JWT token for user {user.UserName} with Id {user.Id}: {tokenString}");
            return tokenString;
        }
    }
}