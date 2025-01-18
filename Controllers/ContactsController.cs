using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ContactsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    public IActionResult AddContact([FromBody] ContactRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Некорректный идентификатор пользователя." });
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Contacts (UserId, Name, PhoneNumber, Email, Address)
            VALUES (@userId, @name, @phoneNumber, @email, @address);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@phoneNumber", request.PhoneNumber);
        command.Parameters.AddWithValue("@email", request.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@address", request.Address ?? (object)DBNull.Value);

        var contactId = command.ExecuteScalar() as long?;
        if (contactId == null)
        {
            return StatusCode(500, new { Message = "Ошибка при добавлении контакта." });
        }

        SaveRequestHistory(userId, $"AddContact: {request.Name}, {request.PhoneNumber}");

        return Ok(new { Id = contactId, Message = "Контакт успешно добавлен." });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteContact(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Некорректный идентификатор пользователя." });
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Contacts WHERE Id = @id AND UserId = @userId";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@userId", userId);

        var rowsAffected = command.ExecuteNonQuery();

        if (rowsAffected == 0)
        {
            return NotFound(new { Message = "Контакт не найден или у вас нет прав на его удаление." });
        }

        SaveRequestHistory(userId, $"DeleteContact: ID={id}");

        return Ok(new { Message = "Контакт успешно удален." });
    }

    [HttpPatch("{id}")]
    public IActionResult UpdateContact(int id, [FromBody] ContactRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Некорректный идентификатор пользователя." });
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Contacts
            SET Name = @name, PhoneNumber = @phoneNumber, Email = @email, Address = @address
            WHERE Id = @id AND UserId = @userId";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@phoneNumber", request.PhoneNumber);
        command.Parameters.AddWithValue("@email", request.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@address", request.Address ?? (object)DBNull.Value);

        var rowsAffected = command.ExecuteNonQuery();

        if (rowsAffected == 0)
        {
            return NotFound(new { Message = "Контакт не найден или у вас нет прав на его обновление." });
        }

        SaveRequestHistory(userId, $"UpdateContact: ID={id}, Name={request.Name}, PhoneNumber={request.PhoneNumber}");

        return Ok(new { Message = "Контакт успешно обновлен." });
    }

    [HttpGet]
    public IActionResult GetAllContacts()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Некорректный идентификатор пользователя." });
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, PhoneNumber, Email, Address FROM Contacts WHERE UserId = @userId";
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var contacts = new List<Contact>();

        while (reader.Read())
        {
            contacts.Add(new Contact
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                PhoneNumber = reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                Address = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        SaveRequestHistory(userId, "GetAllContacts");

        return Ok(new { Contacts = contacts });
    }

    [HttpGet("{id}")]
    public IActionResult GetContact(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Некорректный идентификатор пользователя." });
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, PhoneNumber, Email, Address FROM Contacts WHERE Id = @id AND UserId = @userId";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@userId", userId);

        SaveRequestHistory(userId, $"GetContact: ID={id}");

        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            var contact = new Contact
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                PhoneNumber = reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                Address = reader.IsDBNull(4) ? null : reader.GetString(4)
            };

            return Ok(new { Contact = contact });
        }

        return NotFound(new { Message = "Контакт не найден или у вас нет прав на его просмотр." });
    }

    [HttpPost("search")]
    public IActionResult SearchContacts([FromBody] ContactSearchRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Некорректный идентификатор пользователя." });
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, PhoneNumber, Email, Address 
            FROM Contacts 
            WHERE UserId = @userId
            AND (Name LIKE @name OR @name IS NULL)
            AND (PhoneNumber LIKE @phoneNumber OR @phoneNumber IS NULL)
            AND (Email LIKE @email OR @email IS NULL)
            AND (Address LIKE @address OR @address IS NULL)";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@name", request.Name != null ? $"%{request.Name}%" : (object)DBNull.Value);
        command.Parameters.AddWithValue("@phoneNumber", request.PhoneNumber != null ? $"%{request.PhoneNumber}%" : (object)DBNull.Value);
        command.Parameters.AddWithValue("@email", request.Email != null ? $"%{request.Email}%" : (object)DBNull.Value);
        command.Parameters.AddWithValue("@address", request.Address != null ? $"%{request.Address}%" : (object)DBNull.Value);

        using var reader = command.ExecuteReader();
        var contacts = new List<Contact>();

        while (reader.Read())
        {
            contacts.Add(new Contact
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                PhoneNumber = reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                Address = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        SaveRequestHistory(userId, $"SearchContacts: Name={request.Name}, PhoneNumber={request.PhoneNumber}, Email={request.Email}, Address={request.Address}");

        return Ok(new { Contacts = contacts });
    }

    private void SaveRequestHistory(int userId, string requestData)
    {
        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO RequestHistory (UserId, RequestData, RequestDate)
            VALUES (@userId, @requestData, @requestDate)";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@requestData", requestData);
        command.Parameters.AddWithValue("@requestDate", DateTime.UtcNow);
        command.ExecuteNonQuery();
    }
}