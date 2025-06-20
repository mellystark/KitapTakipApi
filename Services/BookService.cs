using KitapTakipApi.Data;
using KitapTakipApi.Dtos;
using KitapTakipApi.Models;
using KitapTakipApi.Models.Responses;
using KitapTakipApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using KitapTakipApi.Models.Dtos;

namespace KitapTakipApi.Services;

public class BookService : IBookService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookService> _logger;

    public BookService(ApplicationDbContext context, ILogger<BookService> logger)
    {
        _context = context;
        _logger = logger;
    }


    public async Task<ApiResponse<List<BookDto>>> GetAllBooksAsync()
    {
        try
        {
            var books = await _context.Books
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Genre = b.Genre,
                    CoverImage = b.CoverImage,
                    Description = b.Description
                }).ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Message = "Tüm kitaplar başarıyla getirildi.",
                Data = books
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<BookDto>>
            {
                Success = false,
                Message = $"Hata oluştu: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ApiResponse<List<BookDto>>> GetBooksAsync(string userName)
    {
        try
        {
            _logger.LogInformation($"GetBooksAsync: userName={userName}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogError($"GetBooksAsync: Kullanıcı bulunamadı, userName={userName}");
                return new ApiResponse<List<BookDto>> { Success = false, Message = $"Kullanıcı bulunamadı: {userName}" };
            }

            var books = await _context.Books
                .Where(b => b.UserId == user.Id)
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
                    PageCount = b.PageCount,
                    CoverImage = b.CoverImage,
                    UserId = b.UserId
                })
                .ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Data = books,
                Message = books.Any() ? "Kitaplar başarıyla getirildi." : "Kullanıcıya ait kitap bulunamadı."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetBooksAsync: userName={userName}, hata={ex.Message}");
            return new ApiResponse<List<BookDto>>
            {
                Success = false,
                Message = $"Kitaplar getirilirken bir hata oluştu: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<BookDto>> GetBookByIdAsync(int id, string userName)
    {
        try
        {
            _logger.LogInformation($"GetBookByIdAsync: id={id}, userName={userName}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogError($"GetBookByIdAsync: Kullanıcı bulunamadı, userName={userName}");
                return new ApiResponse<BookDto> { Success = false, Message = $"Kullanıcı bulunamadı: {userName}" };
            }

            var book = await _context.Books
                .Where(b => b.Id == id && b.UserId == user.Id)
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
                    PageCount = b.PageCount,
                    CoverImage = b.CoverImage,
                    UserId = b.UserId
                })
                .FirstOrDefaultAsync();

            if (book == null)
                return new ApiResponse<BookDto> { Success = false, Message = "Kitap bulunamadı." };

            return new ApiResponse<BookDto> { Success = true, Data = book, Message = "Kitap başarıyla getirildi." };
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetBookByIdAsync: id={id}, userName={userName}, hata={ex.Message}");
            return new ApiResponse<BookDto>
            {
                Success = false,
                Message = $"Kitap getirilirken bir hata oluştu: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<BookDto>> AddBookAsync(BookCreateDto bookDto, string userName)
    {
        try
        {
            _logger.LogInformation($"AddBookAsync: userName={userName}, bookTitle={bookDto.Title}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogError($"AddBookAsync: Kullanıcı bulunamadı, userName={userName}");
                return new ApiResponse<BookDto>
                {
                    Success = false,
                    Message = $"Kullanıcı bulunamadı: {userName}. Lütfen geçerli bir kullanıcı ile giriş yapın."
                };
            }

            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                Genre = bookDto.Genre,
                Notes = bookDto.Notes,
                Description = bookDto.Description,
                PageCount = bookDto.PageCount,
                CoverImage = bookDto.CoverImage,
                UserId = user.Id
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var resultDto = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                Notes = book.Notes,
                Description = book.Description,
                PageCount = book.PageCount,
                CoverImage = book.CoverImage,
                UserId = book.UserId
            };

            _logger.LogInformation($"AddBookAsync: Kitap eklendi, Id={book.Id}, userName={userName}, UserId={user.Id}");
            return new ApiResponse<BookDto>
            {
                Success = true,
                Data = resultDto,
                Message = "Kitap başarıyla eklendi."
            };
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 547)
        {
            _logger.LogError($"AddBookAsync: Yabancı anahtar hatası, userName={userName}, hata={ex.Message}");
            return new ApiResponse<BookDto>
            {
                Success = false,
                Message = $"Yabancı anahtar hatası: Kullanıcı ({userName}) ile ilişkili UserId veritabanında mevcut değil."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"AddBookAsync: userName={userName}, hata={ex.Message}");
            return new ApiResponse<BookDto>
            {
                Success = false,
                Message = $"Kitap eklenirken bir hata oluştu: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<BookDto>> UpdateBookAsync(int id, BookUpdateDto bookDto, string userName)
    {
        try
        {
            _logger.LogInformation($"UpdateBookAsync: id={id}, userName={userName}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogError($"UpdateBookAsync: Kullanıcı bulunamadı, userName={userName}");
                return new ApiResponse<BookDto> { Success = false, Message = $"Kullanıcı bulunamadı: {userName}" };
            }

            if (id != bookDto.Id)
                return new ApiResponse<BookDto> { Success = false, Message = "Geçersiz kitap ID'si." };

            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (book == null)
                return new ApiResponse<BookDto> { Success = false, Message = "Kitap bulunamadı." };

            book.Title = bookDto.Title;
            book.Author = bookDto.Author;
            book.Genre = bookDto.Genre;
            book.Notes = bookDto.Notes;
            book.Description = bookDto.Description;
            book.PageCount = bookDto.PageCount;
            book.CoverImage = bookDto.CoverImage;

            await _context.SaveChangesAsync();

            var resultDto = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                StartDate = book.StartDate,
                EndDate = book.EndDate,
                Notes = book.Notes,
                Description = book.Description,
                PageCount = book.PageCount,
                CoverImage = book.CoverImage,
                UserId = book.UserId
            };

            _logger.LogInformation($"UpdateBookAsync: Kitap güncellendi, Id={id}, userName={userName}");
            return new ApiResponse<BookDto>
            {
                Success = true,
                Data = resultDto,
                Message = "Kitap başarıyla güncellendi."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"UpdateBookAsync: id={id}, userName={userName}, hata={ex.Message}");
            return new ApiResponse<BookDto>
            {
                Success = false,
                Message = $"Kitap güncellenirken bir hata oluştu: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteBookAsync(int id, string userName)
    {
        try
        {
            _logger.LogInformation($"DeleteBookAsync: id={id}, userName={userName}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogError($"DeleteBookAsync: Kullanıcı bulunamadı, userName={userName}");
                return new ApiResponse<bool> { Success = false, Message = $"Kullanıcı bulunamadı: {userName}" };
            }

            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (book == null)
                return new ApiResponse<bool> { Success = false, Message = "Kitap bulunamadı." };

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"DeleteBookAsync: Kitap silindi, Id={id}, userName={userName}");
            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Kitap başarıyla silindi."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"DeleteBookAsync: id={id}, userName={userName}, hata={ex.Message}");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Kitap silinirken bir hata oluştu: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BookDto>>> GetBooksByAuthorNameAsync(string userName, string authorName)
    {
        try
        {
            _logger.LogInformation($"GetBooksByAuthorNameAsync: userName={userName}, authorName={authorName}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogError($"GetBooksByAuthorNameAsync: Kullanıcı bulunamadı, userName={userName}");
                return new ApiResponse<List<BookDto>> { Success = false, Message = $"Kullanıcı bulunamadı: {userName}" };
            }

            if (string.IsNullOrEmpty(authorName))
                return new ApiResponse<List<BookDto>> { Success = false, Message = "Yazar adı belirtilmelidir." };

            var books = await _context.Books
                .Where(b => b.UserId == user.Id && b.Author.ToLower().Contains(authorName.ToLower()))
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
                    PageCount = b.PageCount,
                    CoverImage = b.CoverImage,
                    UserId = b.UserId
                })
                .ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Data = books,
                Message = books.Any() ? $"Yazar adına göre kitaplar getirildi: {authorName}" : $"Yazar adına uygun kitap bulunamadı: {authorName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetBooksByAuthorNameAsync: userName={userName}, authorName={authorName}, hata={ex.Message}");
            return new ApiResponse<List<BookDto>>
            {
                Success = false,
                Message = $"Yazar adına göre kitaplar getirilirken bir hata oluştu: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BookDto>>> GetBooksByGenreAsync(string userName, string genre)
    {
        try
        {
            _logger.LogInformation($"GetBooksByGenreAsync: userName={userName}, genre={genre}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogError($"GetBooksByGenreAsync: Kullanıcı bulunamadı, userName={userName}");
                return new ApiResponse<List<BookDto>> { Success = false, Message = $"Kullanıcı bulunamadı: {userName}" };
            }

            if (string.IsNullOrEmpty(genre))
                return new ApiResponse<List<BookDto>> { Success = false, Message = "Tür adı belirtilmelidir." };

            var books = await _context.Books
                .Where(b => b.UserId == user.Id && b.Genre.ToLower().Contains(genre.ToLower()))
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
                    PageCount = b.PageCount,
                    CoverImage = b.CoverImage,
                    UserId = b.UserId
                })
                .ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Data = books,
                Message = books.Any() ? $"Tür adına göre kitaplar getirildi: {genre}" : $"Tür adına uygun kitap bulunamadı: {genre}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetBooksByGenreAsync: userName={userName}, genre={genre}, hata={ex.Message}");
            return new ApiResponse<List<BookDto>>
            {
                Success = false,
                Message = $"Tür adına göre kitaplar getirilirken bir hata oluştu: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<BookDto>>> GetBooksByTitleAsync(string userName, string title)
    {
        try
        {
            _logger.LogInformation($"GetBooksByTitleAsync: userName={userName}, title={title}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogError($"GetBooksByTitleAsync: Kullanıcı bulunamadı, userName={userName}");
                return new ApiResponse<List<BookDto>> { Success = false, Message = $"Kullanıcı bulunamadı: {userName}" };
            }

            if (string.IsNullOrEmpty(title))
                return new ApiResponse<List<BookDto>> { Success = false, Message = "Kitap başlığı belirtilmelidir." };

            var books = await _context.Books
                .Where(b => b.UserId == user.Id && b.Title.ToLower().Contains(title.ToLower()))
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
                    PageCount = b.PageCount,
                    CoverImage = b.CoverImage,
                    UserId = b.UserId
                })
                .ToListAsync();

            return new ApiResponse<List<BookDto>>
            {
                Success = true,
                Data = books,
                Message = books.Any() ? $"Başlığa göre kitaplar getirildi: {title}" : $"Başlığa uygun kitap bulunamadı: {title}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetBooksByTitleAsync: userName={userName}, title={title}, hata={ex.Message}");
            return new ApiResponse<List<BookDto>>
            {
                Success = false,
                Message = $"Başlığa göre kitaplar getirilirken bir hata oluştu: {ex.Message}"
            };
        }
    }
}