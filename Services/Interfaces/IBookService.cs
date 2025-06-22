using KitapTakipApi.Dtos;
using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;
using System.Collections.Generic;

namespace KitapTakipApi.Services.Interfaces;

public interface IBookService
{
    Task<ApiResponse<List<BookDto>>> GetAllBooksAsync();
    Task<ApiResponse<List<BookDto>>> GetBooksAsync(string userName);
    Task<ApiResponse<BookDto>> GetBookByIdAsync(int id, string userName);
    Task<ApiResponse<BookDto>> AddBookAsync(BookCreateDto bookDto, string userName);
    Task<ApiResponse<BookDto>> UpdateBookAsync(int id, BookUpdateDto bookDto, string userName);
    Task<ApiResponse<bool>> DeleteBookAsync(int id, string userName);
    Task<ApiResponse<List<BookDto>>> GetBooksByAuthorNameAsync(string userName, string authorName);
    Task<ApiResponse<List<BookDto>>> GetBooksByGenreAsync(string userName, string genre);
    Task<ApiResponse<List<BookDto>>> GetBooksByTitleAsync(string userName, string title);
    Task<ApiResponse<List<BookDto>>> GetReadBooksAsync(string userName, string title = "");
}