using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HistoryController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult GetHistory()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized("Идентификатор пользователя отсутствует.");
        }

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return BadRequest("Некорректный идентификатор пользователя.");
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, RequestData, RequestDate FROM RequestHistory WHERE UserId = @userId";
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var history = new List<object>();

        while (reader.Read())
        {
            history.Add(new
            {
                Id = reader.GetInt32(0),
                RequestData = reader.GetString(1),
                RequestDate = reader.GetDateTime(2)
            });
        }

        return Ok(history);
    }

    [HttpDelete]
    public IActionResult DeleteHistory()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized("Идентификатор пользователя отсутствует.");
        }

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return BadRequest("Некорректный идентификатор пользователя.");
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM RequestHistory WHERE UserId = @userId";
        command.Parameters.AddWithValue("@userId", userId);
        command.ExecuteNonQuery();

        return NoContent();
    }
}