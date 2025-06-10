using KitapTakipApi.Data;
using KitapTakipApi.Dtos;
using KitapTakipApi.Models.Dtos;
using KitapTakipApi.Models.Responses;
using KitapTakipApi.Services;
using KitapTakipApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Veritabaný baðlantýsý
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookService, BookService>();

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
/// Yeni admin kaydý yapar.
/// </summary>
app.MapPost("/api/auth/register-admin", async (RegisterDto registerDto, IAuthService authService) =>
{
	var response = await authService.RegisterAdminAsync(registerDto);
	return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).WithName("RegisterAdmin").WithTags("Auth");

/// <summary>
/// Kullanýcý giriþi yapar ve JWT token döndürür.
/// </summary>
app.MapPost("/api/auth/login", async (LoginDto loginDto, IAuthService authService) =>
{
	var response = await authService.LoginAsync(loginDto);
	return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).WithName("Login").WithTags("Auth");

/// <summary>
/// Admin giriþi yapar ve JWT token döndürür.
/// </summary>
app.MapPost("/api/auth/login-admin", async (LoginDto loginDto, IAuthService authService) =>
{
	var response = await authService.LoginAdminAsync(loginDto);
	return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).WithName("LoginAdmin").WithTags("Auth");

/// <summary>
/// Kullanýcýnýn þifresini deðiþtirir.
/// </summary>
app.MapPost("/api/auth/change-password", async (ChangePasswordDto changePasswordDto, IAuthService authService) =>
{
	var response = await authService.ChangePasswordAsync(changePasswordDto);
	return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("ChangePassword").WithTags("Auth");



/// <summary>
/// Bir kullanýcýyý siler (Admin sadece).
/// </summary>
app.MapDelete("/api/auth/users/{userName}", async (string userName, IAuthService authService) =>
{
	var response = await authService.DeleteUserAsync(userName);
	return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization("AdminOnly").WithName("DeleteUser").WithTags("Auth");

/// <summary>
/// Kullanýcýnýn tüm kitaplarýný listeler.
/// </summary>
app.MapGet("/api/books", async (IBookService bookService, HttpContext context) =>
{
	var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	if (string.IsNullOrEmpty(userId))
	{
		app.Logger.LogError("GetBooks: userId not found in token.");
		return Results.Unauthorized();
	}

	app.Logger.LogInformation($"GetBooks: userId={userId}");
	var response = await bookService.GetBooksAsync(userId);
	return response.Success ? Results.Ok(response) : Results.NotFound(response);
}).RequireAuthorization().WithName("GetBooks").WithTags("Books");

/// <summary>
/// ID'ye göre bir kitabý getirir.
/// </summary>
app.MapGet("/api/books/{id}", async (int id, IBookService bookService, HttpContext context) =>
{
	var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	if (string.IsNullOrEmpty(userId))
	{
		app.Logger.LogError("GetBookById: userId not found in token.");
		return Results.Unauthorized();
	}

	app.Logger.LogInformation($"GetBookById: id={id}, userId={userId}");
	var response = await bookService.GetBookByIdAsync(id, userId);
	return response.Success ? Results.Ok(response) : Results.NotFound(response);
}).RequireAuthorization().WithName("GetBookById").WithTags("Books");

/// <summary>
/// Yazar adýna göre kullanýcýnýn kitaplarýný getirir.
/// </summary>
app.MapGet("/api/books/by-author", async (IBookService bookService, HttpContext context, string authorName) =>
{
	var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	if (string.IsNullOrEmpty(userId))
	{
		app.Logger.LogError("GetBooksByAuthorName: userId not found in token.");
		return Results.Unauthorized();
	}

	app.Logger.LogInformation($"GetBooksByAuthorName: authorName={authorName}, userId={userId}");
	var response = await bookService.GetBooksByAuthorNameAsync(userId, authorName);
	return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("GetBooksByAuthorName").WithTags("Books");

/// <summary>
/// Tür adýna göre kullanýcýnýn kitaplarýný getirir.
/// </summary>
app.MapGet("/api/books/by-genre", async (IBookService bookService, HttpContext context, string genre) =>
{
	var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	if (string.IsNullOrEmpty(userId))
	{
		app.Logger.LogError("GetBooksByGenre: userId not found in token.");
		return Results.Unauthorized();
	}

	app.Logger.LogInformation($"GetBooksByGenre: genre={genre}, userId={userId}");
	var response = await bookService.GetBooksByGenreAsync(userId, genre);
	return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("GetBooksByGenre").WithTags("Books");

/// <summary>
/// Baþlýða göre kullanýcýnýn kitaplarýný getirir.
/// </summary>
app.MapGet("/api/books/by-title", async (IBookService bookService, HttpContext context, string title) =>
{
	var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	if (string.IsNullOrEmpty(userId))
	{
		app.Logger.LogError("GetBooksByTitle: userId not found in token.");
		return Results.Unauthorized();
	}

	app.Logger.LogInformation($"GetBooksByTitle: title={title}, userId={userId}");
	var response = await bookService.GetBooksByTitleAsync(userId, title);
	return response.Success ? Results.Ok(response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("GetBooksByTitle").WithTags("Books");

/// <summary>
/// Yeni bir kitap ekler.
/// </summary>
app.MapPost("/api/books", async (BookCreateDto bookDto, IBookService bookService, HttpContext context) =>
{
	var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	if (string.IsNullOrEmpty(userId))
	{
		app.Logger.LogError("AddBook: userId not found in token.");
		return Results.Unauthorized();
	}

	app.Logger.LogInformation($"AddBook: userId={userId}, bookTitle={bookDto.Title}");
	var response = await bookService.AddBookAsync(bookDto, userId);
	return response.Success ? Results.Created($"/api/books/{response.Data?.Id}", response) : Results.BadRequest(response);
}).RequireAuthorization().WithName("AddBook").WithTags("Books");

/// <summary>
/// Mevcut bir kitabý günceller.
/// </summary>
app.MapPut("/api/books/{id}", async (int id, BookUpdateDto bookDto, IBookService bookService, HttpContext context) =>
{
	var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	if (string.IsNullOrEmpty(userId))
	{
		app.Logger.LogError("UpdateBook: userId not found in token.");
		return Results.Unauthorized();
	}

	app.Logger.LogInformation($"UpdateBook: id={id}, userId={userId}");
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
	{
		app.Logger.LogError("DeleteBook: userId not found in token.");
		return Results.Unauthorized();
	}

	app.Logger.LogInformation($"DeleteBook: id={id}, userId={userId}");
	var response = await bookService.DeleteBookAsync(id, userId);
	return response.Success ? Results.Ok(response) : Results.NotFound(response);
}).RequireAuthorization().WithName("DeleteBook").WithTags("Books");

app.Run();