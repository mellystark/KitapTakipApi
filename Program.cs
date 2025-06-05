using KitapTakipApi.Data;
using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;
using KitapTakipApi.Services;
using KitapTakipApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Veritabaný baðlantýsý
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity yapýlandýrmasý
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Yetkilendirme
builder.Services.AddAuthorization();

// Swagger yapýlandýrmasý
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KitapTakipApi",
        Version = "v1",
        Description = "Kitap takip uygulamasý için Minimal API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Token (Örnek: Bearer {token})",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Servis kayýtlarý
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "KitapTakipApi v1");
    options.RoutePrefix = string.Empty;
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/books/all", async (IBookService bookService) =>
{
    var response = await bookService.GetAllBooksAsync();
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).WithName("GetAllBooks").WithTags("Books");
app.MapGet("/api/books/by-author", async (IBookService bookService, string authorName) =>
{
    var response = await bookService.GetBooksByAuthorNameAsync(authorName);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).WithName("GetBooksByAuthorName").WithTags("Books");
app.MapGet("/api/books/by-genre", async (IBookService bookService, string genre) => { var response = await bookService.GetBooksByGenreAsync(genre); return response.Success ? Results.Ok(response) : Results.BadRequest(response); }).WithName("GetBooksByGenre").WithTags("Books");

app.MapGet("/api/books/by-title", async (IBookService bookService, string title) => { var response = await bookService.GetBooksByTitleAsync(title); return response.Success ? Results.Ok(response) : Results.BadRequest(response); }).WithName("GetBooksByTitle").WithTags("Books");

// Minimal API Endpoint'leri
/// <summary>
/// Yeni kullanýcý kaydý yapar.
/// </summary>
app.MapPost("/api/auth/register", async (RegisterDto registerDto, IAuthService authService) =>
{
    var response = await authService.RegisterAsync(registerDto);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).WithName("Register").WithTags("Auth");

/// <summary>
/// Kullanýcý giriþi yapar ve JWT token döndürür.
/// </summary>
app.MapPost("/api/auth/login", async (LoginDto loginDto, IAuthService authService) =>
{
    var response = await authService.LoginAsync(loginDto);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).WithName("Login").WithTags("Auth");

/// <summary>
/// Kullanýcýnýn þifresini deðiþtirir.
/// </summary>
app.MapPost("/api/auth/change-password", async (ChangePasswordDto changePasswordDto, IAuthService authService, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        Console.WriteLine("Unauthorized: userId not found in token.");
        return Results.Unauthorized();
    }

    Console.WriteLine($"Change password request for userId: {userId}");
    var response = await authService.ChangePasswordAsync(changePasswordDto, userId);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("ChangePassword").WithTags("Auth");

/// <summary>
/// Kullanýcýnýn kitaplarýný listeler (isteðe baðlý tür ve yazar filtresiyle).
/// </summary>
app.MapGet("/api/books", async (IBookService bookService, HttpContext context, string? genre = null, string? author = null) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var response = await bookService.GetBooksAsync(userId, genre, author);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("GetBooks").WithTags("Books");

/// <summary>
/// ID'ye göre tek bir kitabý getirir.
/// </summary>
app.MapGet("/api/books/{id}", async (int id, IBookService bookService, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var response = await bookService.GetBookByIdAsync(id, userId);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("GetBookById").WithTags("Books");

app.MapGet("/api/books/details/{id}", async (int id, IBookService bookService, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var response = await bookService.GetBookDetailsByIdAsync(id, userId);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("GetBookDetailsById").WithTags("Books");

/// <summary>
/// Yeni bir kitap ekler.
/// </summary>
app.MapPost("/api/books", async (BookDto bookDto, IBookService bookService, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var response = await bookService.AddBookAsync(bookDto, userId);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("AddBook").WithTags("Books");

/// <summary>
/// Mevcut bir kitabý günceller.
/// </summary>
app.MapPut("/api/books/{id}", async (int id, BookDto bookDto, IBookService bookService, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var response = await bookService.UpdateBookAsync(id, bookDto, userId);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("UpdateBook").WithTags("Books");

/// <summary>
/// Bir kitabý siler.
/// </summary>
app.MapDelete("/api/books/{id}", async (int id, IBookService bookService, HttpContext context) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var response = await bookService.DeleteBookAsync(id, userId);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("DeleteBook").WithTags("Books");


app.Run();