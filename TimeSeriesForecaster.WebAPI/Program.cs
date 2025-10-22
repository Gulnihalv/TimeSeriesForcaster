using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TimeSeriesForecaster.Application.Configuration;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Services;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// CORS politikası
var reactAppPolicy = "AllowReactApp";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: reactAppPolicy,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// DbContext kaydı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole<int>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>();
    
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT Secret anahtarı yapılandırılmamış.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                };
            });

//  otomatik kayıt yapan extension metodunu yazıcam ama şimdilik manuel ekliyoruz
builder.Services.AddScoped<IAuthService, AuthService>();
// Buraya diğer repository ve servislerin kayıtları da geliyo

// Controller ve Swagger servisleri
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// --- Middleware Pipeline Konfigürasyonu ---

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(reactAppPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();