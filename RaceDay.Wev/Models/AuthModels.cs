using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class SessionUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
}

public class LoginResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
}

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}

public class RegisterViewModel
{
    [Required, Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [Display(Name = "Phone number")] public string? PhoneNumber { get; set; }
}