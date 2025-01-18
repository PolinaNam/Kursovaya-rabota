using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings["Key"];

if (string.IsNullOrEmpty(key))
{
    throw new InvalidOperationException("Ключ JWT не настроен в appsettings.json.");
}

var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = securityKey,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

InitializeDatabase(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

void InitializeDatabase(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    var createUserTableCommand = connection.CreateCommand();
    createUserTableCommand.CommandText = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL,
            Token TEXT
        )";
    createUserTableCommand.ExecuteNonQuery();

    var createHistoryTableCommand = connection.CreateCommand();
    createHistoryTableCommand.CommandText = @"
        CREATE TABLE IF NOT EXISTS RequestHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            RequestData TEXT NOT NULL,
            RequestDate DATETIME NOT NULL,
            FOREIGN KEY(UserId) REFERENCES Users(Id)
        )";
    createHistoryTableCommand.ExecuteNonQuery();

    var createContactsTableCommand = connection.CreateCommand();
    createContactsTableCommand.CommandText = @"
        CREATE TABLE IF NOT EXISTS Contacts (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Name TEXT NOT NULL,
            PhoneNumber TEXT NOT NULL,
            Email TEXT,
            Address TEXT,
            FOREIGN KEY(UserId) REFERENCES Users(Id)
        )";
    createContactsTableCommand.ExecuteNonQuery();
}