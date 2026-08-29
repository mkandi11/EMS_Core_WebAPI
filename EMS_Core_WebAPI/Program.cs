using EMS_Core_WebAPI.Helper;
using EMS_Core_WebAPI.Repositories;
using EMS_Core_WebAPI.Services;
using EMS_Framework_WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Required for JWT
using Microsoft.IdentityModel.Tokens; // Required for JWT
using System.Text; // Required to encode JWT key
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: allow requests from the SPA running at http://localhost:5173
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost5173", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Below are add for JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
    options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Register raw ADO.NET repository
builder.Services.AddScoped<DBConnection>();
builder.Services.AddScoped<EmployeeRepository>();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply CORS policy before authentication/authorization middleware
app.UseCors("AllowLocalhost5173");

// Add Authentication Middleware
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
