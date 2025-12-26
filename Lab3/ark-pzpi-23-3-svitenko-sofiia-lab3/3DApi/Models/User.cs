namespace _3DApi.Models;

public class User : Base
{
    public string Email { get; set; }
    
    public string Role { get; set; } = "user"; // "admin" or "user"
}