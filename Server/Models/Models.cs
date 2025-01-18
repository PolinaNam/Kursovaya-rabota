public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public string? Token { get; set; }
}

public class RequestHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string RequestData { get; set; }
    public DateTime RequestDate { get; set; }
}

public class UserRegistrationRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class UserLoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class Contact
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class ContactRequest
{
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class ContactSearchRequest
{
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}