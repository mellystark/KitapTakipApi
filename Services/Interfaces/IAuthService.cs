using KitapTakipApi.Dtos;
using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;

namespace KitapTakipApi.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<string>> RegisterAsync(RegisterDto registerDto);
    Task<ApiResponse<string>> RegisterAdminAsync(RegisterDto registerDto);
    Task<ApiResponse<string>> LoginAsync(LoginDto loginDto);
    Task<ApiResponse<string>> LoginAdminAsync(LoginDto loginDto);
    Task<ApiResponse<string>> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
    Task<ApiResponse<string>> DeleteUserAsync(string userName);
    Task<ApiResponse<string>> UpdateProfileAsync(UpdateProfileDto updateProfileDto, string userId);
    Task<ApiResponse<List<UserDto>>> GetAllUsersAsync();
}