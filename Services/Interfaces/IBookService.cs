using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;

namespace KitapTakipApi.Services.Interfaces
{
    public interface IBookService
    {
        Task<ApiResponse<List<BookDto>>> GetBooksAsync(string userId, string? genre = null, string? author = null);
        Task<ApiResponse<BookDto>> GetBookByIdAsync(int id, string userId);
        Task<ApiResponse<BookDto>> AddBookAsync(BookDto bookDto, string userId);
        Task<ApiResponse<BookDto>> UpdateBookAsync(int id, BookDto bookDto, string userId);
        Task<ApiResponse<bool>> DeleteBookAsync(int id, string userId);
    }
}