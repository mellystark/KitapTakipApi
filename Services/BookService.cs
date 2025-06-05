using KitapTakipApi.Data;
using KitapTakipApi.Data.Entities;
using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;
using KitapTakipApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KitapTakipApi.Services
{
    public class BookService : IBookService
    {
        private readonly ApplicationDbContext _context;

        public BookService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<BookDto>>> GetBooksAsync(string userId, string? genre = null, string? author = null)
        {
            var query = _context.Books.Where(b => b.UserId == userId);
            if (!string.IsNullOrEmpty(genre))
                query = query.Where(b => b.Genre == genre);
            if (!string.IsNullOrEmpty(author))
                query = query.Where(b => b.Author == author);

            var books = await query.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Genre = b.Genre,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Notes = b.Notes,
                Description = b.Description,
                PageCount = b.PageCount
            }).ToListAsync();

            return new ApiResponse<List<BookDto>> { Success = true, Data = books };
        }

        public async Task<ApiResponse<BookDto>> GetBookByIdAsync(int id, string userId)
        {
            var book = await _context.Books
                .Where(b => b.Id == id && b.UserId == userId)
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Genre = b.Genre,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Notes = b.Notes,
                    Description = b.Description,
                    PageCount = b.PageCount
                }).FirstOrDefaultAsync();

            if (book == null)
                return new ApiResponse<BookDto> { Success = false, Message = "Kitap bulunamadı." };

            return new ApiResponse<BookDto> { Success = true, Data = book };
        }

        public async Task<ApiResponse<BookDto>> AddBookAsync(BookDto bookDto, string userId)
        {
            if (string.IsNullOrEmpty(bookDto.Title) || string.IsNullOrEmpty(bookDto.Author) || string.IsNullOrEmpty(bookDto.Genre))
                return new ApiResponse<BookDto> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };

            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                Genre = bookDto.Genre,
                StartDate = bookDto.StartDate,
                EndDate = bookDto.EndDate,
                Notes = bookDto.Notes,
                Description = bookDto.Description,
                PageCount = bookDto.PageCount,
                UserId = userId
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            bookDto.Id = book.Id;
            return new ApiResponse<BookDto> { Success = true, Data = bookDto, Message = "Kitap eklendi." };
        }

        public async Task<ApiResponse<BookDto>> UpdateBookAsync(int id, BookDto bookDto, string userId)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (book == null)
                return new ApiResponse<BookDto> { Success = false, Message = "Kitap bulunamadı." };

            if (string.IsNullOrEmpty(bookDto.Title) || string.IsNullOrEmpty(bookDto.Author) || string.IsNullOrEmpty(bookDto.Genre))
                return new ApiResponse<BookDto> { Success = false, Message = "Zorunlu alanlar doldurulmalıdır." };

            book.Title = bookDto.Title;
            book.Author = bookDto.Author;
            book.Genre = bookDto.Genre;
            book.StartDate = bookDto.StartDate;
            book.EndDate = bookDto.EndDate;
            book.Notes = bookDto.Notes;
            book.Description = bookDto.Description;
            book.PageCount = bookDto.PageCount;

            await _context.SaveChangesAsync();
            return new ApiResponse<BookDto> { Success = true, Data = bookDto, Message = "Kitap güncellendi." };
        }

        public async Task<ApiResponse<bool>> DeleteBookAsync(int id, string userId)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (book == null)
                return new ApiResponse<bool> { Success = false, Message = "Kitap bulunamadı." };

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return new ApiResponse<bool> { Success = true, Data = true, Message = "Kitap silindi." };
        }

        public async Task<ApiResponse<List<BookDto>>> GetAllBooksAsync()
        {
            var books = await _context.Books
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Genre = b.Genre,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Notes = b.Notes,
                    Description = b.Description,
                    PageCount = b.PageCount
                })
                .ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Data = books,
                Message = books.Any() ? "Tüm kitaplar başarıyla getirildi." : "Kayıtlı kitap bulunamadı."
            };
        }

        public async Task<ApiResponse<List<BookDto>>> GetBooksByAuthorNameAsync(string authorName)
        {
            if (string.IsNullOrEmpty(authorName))
                return new ApiResponse<List<BookDto>> { Success = false, Message = "Yazar adı belirtilmelidir." };

            var books = await _context.Books
                .Where(b => b.Author.ToLower().Contains(authorName.ToLower()))
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Genre = b.Genre,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Notes = b.Notes,
                    Description = b.Description,
                    PageCount = b.PageCount
                })
                .ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Data = books,
                Message = books.Any() ? $"Yazar adına göre kitaplar getirildi: {authorName}" : $"Yazar adına uygun kitap bulunamadı: {authorName}"
            };
        }

        public async Task<ApiResponse<List<BookDto>>> GetBooksByGenreAsync(string genre)
        {
            if (string.IsNullOrEmpty(genre))
                return new ApiResponse<List<BookDto>> { Success = false, Message = "Tür adı belirtilmelidir." };

            var books = await _context.Books
                .Where(b => b.Genre.ToLower().Contains(genre.ToLower()))
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Genre = b.Genre,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Notes = b.Notes,
                    Description = b.Description,
                    PageCount = b.PageCount
                })
                .ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Data = books,
                Message = books.Any() ? $"Tür adına göre kitaplar getirildi: {genre}" : $"Tür adına uygun kitap bulunamadı: {genre}"
            };
        }

        public async Task<ApiResponse<List<BookDto>>> GetBooksByTitleAsync(string title)
        {
            if (string.IsNullOrEmpty(title))
                return new ApiResponse<List<BookDto>> { Success = false, Message = "Kitap başlığı belirtilmelidir." };

            var books = await _context.Books
                .Where(b => b.Title.ToLower().Contains(title.ToLower()))
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Genre = b.Genre,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Notes = b.Notes,
                    Description = b.Description,
                    PageCount = b.PageCount
                })
                .ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Data = books,
                Message = books.Any() ? $"Başlığa göre kitaplar getirildi: {title}" : $"Başlığa uygun kitap bulunamadı: {title}"
            };
        }

        public async Task<ApiResponse<BookDetailsDto>> GetBookDetailsByIdAsync(int id, string userId)
        {
            var book = await _context.Books
                .Where(b => b.Id == id && b.UserId == userId)
                .Select(b => new BookDetailsDto
                {
                    Title = b.Title,
                    Author = b.Author,
                    Notes = b.Notes,
                    Description = b.Description,
                    PageCount = b.PageCount
                })
                .FirstOrDefaultAsync();

            if (book == null)
                return new ApiResponse<BookDetailsDto> { Success = false, Message = "Kitap bulunamadı." };

            return new ApiResponse<BookDetailsDto> { Success = true, Data = book };
        }
    }
}