using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;
using System.Collections.Generic;

namespace KitapTakipApi.Services.Interfaces
{
    public interface IBookService
    {
        Task<ApiResponse<List<BookDto>>> GetBooksAsync(string userId, string? genre = null, string? author = null);
        Task<ApiResponse<BookDto>> GetBookByIdAsync(int id, string userId);
        Task<ApiResponse<BookDto>> AddBookAsync(BookDto bookDto, string userId);
        Task<ApiResponse<BookDto>> UpdateBookAsync(int id, BookDto bookDto, string userId);
        Task<ApiResponse<bool>> DeleteBookAsync(int id, string userId);
        Task<ApiResponse<List<BookDto>>> GetAllBooksAsync();
        Task<ApiResponse<List<BookDto>>> GetBooksByAuthorNameAsync(string authorName);
        Task<ApiResponse<List<BookDto>>> GetBooksByGenreAsync(string genre);
        Task<ApiResponse<List<BookDto>>> GetBooksByTitleAsync(string title);
        Task<ApiResponse<BookDetailsDto>> GetBookDetailsByIdAsync(int id, string userId);
    }
}