using KitapTakipApi.Dtos;
using KitapTakipApi.Models.Responses;

namespace KitapTakipApi.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<string>> RegisterAsync(RegisterDto registerDto);
    Task<ApiResponse<string>> LoginAsync(LoginDto loginDto);
    Task<ApiResponse<string>> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
}