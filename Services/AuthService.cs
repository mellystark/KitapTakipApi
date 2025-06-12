using KitapTakipApi.Data;
using KitapTakipApi.Dtos;
using KitapTakipApi.Models;
using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;
using KitapTakipApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
        if (string.IsNullOrEmpty(registerDto.UserName) ||
            string.IsNullOrEmpty(registerDto.Email) ||
            string.IsNullOrEmpty(registerDto.Password))
        {
            return new ApiResponse<string> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == registerDto.UserName || u.Email == registerDto.Email);
        if (existingUser != null)
        {
            return new ApiResponse<string> { Success = false, Message = "Kullanıcı adı veya e-posta zaten kullanılıyor." };
        }

        // Rolü kontrol et, boşsa "User" olarak ata
        string assignedRole = string.IsNullOrEmpty(registerDto.Role) ? "User" : registerDto.Role;

        // Sadece "Admin" ve "User" rollerine izin ver
        if (!string.Equals(assignedRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(assignedRole, "User", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResponse<string> { Success = false, Message = "Geçersiz rol. Sadece 'Admin' veya 'User' rollerine izin verilir." };
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = registerDto.UserName,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            Role = assignedRole
        };

        _logger.LogInformation($"Kullanıcı kaydediliyor: UserName={registerDto.UserName}, UserId={user.Id}, Role={assignedRole}");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new ApiResponse<string> { Success = true, Message = $"{assignedRole} kullanıcı kaydedildi." };
    }





    //public async Task<ApiResponse<string>> RegisterAdminAsync(RegisterDto registerDto)
    //{
    //    if (string.IsNullOrEmpty(registerDto.UserName) || string.IsNullOrEmpty(registerDto.Email) || string.IsNullOrEmpty(registerDto.Password))
    //        return new ApiResponse<string> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };

    //    var existingUser = await _context.Users
    //        .FirstOrDefaultAsync(u => u.UserName == registerDto.UserName || u.Email == registerDto.Email);
    //    if (existingUser != null)
    //        return new ApiResponse<string> { Success = false, Message = "Kullanıcı adı veya e-posta zaten kullanılıyor." };

    //    var user = new User
    //    {
    //        Id = Guid.NewGuid().ToString(),
    //        UserName = registerDto.UserName,
    //        Email = registerDto.Email,
    //        PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
    //        Role = "Admin"
    //    };

    //    _logger.LogInformation($"Admin kaydediliyor: UserName={registerDto.UserName}, UserId={user.Id}");

    //    _context.Users.Add(user);
    //    await _context.SaveChangesAsync();

    //    return new ApiResponse<string> { Success = true, Message = "Admin kaydedildi." };
    //}

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

    public async Task<ApiResponse<string>> LoginAdminAsync(LoginDto loginDto)
    {
        if (string.IsNullOrEmpty(loginDto.UserNameOrEmail) || string.IsNullOrEmpty(loginDto.Password))
            return new ApiResponse<string> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };

        var user = await _context.Users
            .FirstOrDefaultAsync(u => (u.UserName == loginDto.UserNameOrEmail || u.Email == loginDto.UserNameOrEmail) && u.Role == "Admin");
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return new ApiResponse<string> { Success = false, Message = "Admin kullanıcı adı/e-posta veya şifre yanlış." };

        _logger.LogInformation($"Admin giriş yaptı: UserName={user.UserName}, UserId={user.Id}");

        var token = GenerateJwtToken(user);
        return new ApiResponse<string> { Success = true, Data = token, Message = "Admin girişi başarılı." };
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        if (string.IsNullOrEmpty(changePasswordDto.UserName) || string.IsNullOrEmpty(changePasswordDto.CurrentPassword) || string.IsNullOrEmpty(changePasswordDto.NewPassword))
            return new ApiResponse<string> { Success = false, Message = "Kullanıcı adı, mevcut ve yeni şifre alanları zorunludur." };

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == changePasswordDto.UserName);
        if (user == null)
            return new ApiResponse<string> { Success = false, Message = $"Kullanıcı bulunamadı: UserName={changePasswordDto.UserName}" };

        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            return new ApiResponse<string> { Success = false, Message = "Mevcut şifre yanlış." };

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Şifre değiştirildi: UserName={changePasswordDto.UserName}");
        return new ApiResponse<string> { Success = true, Message = "Şifre başarıyla değiştirildi." };
    }

    public async Task<ApiResponse<string>> DeleteUserAsync(string userName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null)
            return new ApiResponse<string> { Success = false, Message = $"Kullanıcı bulunamadı: UserName={userName}" };

        if (user.Role == "Admin")
            return new ApiResponse<string> { Success = false, Message = "Admin kullanıcılar silinemez." };

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Kullanıcı silindi: UserName={userName}, UserId={user.Id}");
        return new ApiResponse<string> { Success = true, Message = "Kullanıcı başarıyla silindi." };
    }

    public async Task<ApiResponse<string>> UpdateProfileAsync(UpdateProfileDto updateProfileDto, string userName)
    {
        if (string.IsNullOrEmpty(updateProfileDto.UserName) || string.IsNullOrEmpty(updateProfileDto.Email))
        {
            return new ApiResponse<string> { Success = false, Message = "Kullanıcı adı ve e-posta alanları zorunludur." };
        }

        // userName ile kullanıcı bul
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null)
        {
            return new ApiResponse<string> { Success = false, Message = $"Kullanıcı bulunamadı: UserName={userName}" };
        }

        // Aynı UserName veya Email başka kullanıcıda var mı kontrol et
        var existingUser = await _context.Users
        .FirstOrDefaultAsync(u =>
            (u.UserName.ToLower() == updateProfileDto.UserName.ToLower() ||
             u.Email.ToLower() == updateProfileDto.Email.ToLower()) &&
            u.Id != user.Id);


        if (existingUser != null)
        {
            return new ApiResponse<string> { Success = false, Message = "Kullanıcı adı veya e-posta zaten kullanılıyor." };
        }

        user.UserName = updateProfileDto.UserName;
        user.Email = updateProfileDto.Email;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Profil güncellendi: UserName={user.UserName}, UserId={user.Id}");

        return new ApiResponse<string> { Success = true, Message = "Profil başarıyla güncellendi." };
    }


    public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Role = u.Role
            })
            .ToListAsync();

        return new ApiResponse<List<UserDto>> { Success = true, Data = users, Message = "Kullanıcılar başarıyla listelendi." };
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, user.Role)
        };

        _logger.LogInformation($"JWT Token oluşturuluyor: UserName={user.UserName}, UserId={user.Id}, ClaimTypes={user.Role}");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}