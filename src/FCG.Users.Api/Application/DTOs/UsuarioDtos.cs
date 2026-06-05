using System.ComponentModel.DataAnnotations;

namespace FCG.Users.Api.Application.DTOs;

public class UsuarioDTO
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}

public class RegisterUsuarioDTO
{
    [Required(ErrorMessage = "Name obrigatório")]
    [StringLength(100, MinimumLength = 3)]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Email obrigatório")]
    [EmailAddress(ErrorMessage = "Email formato inválido")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password obrigatório")]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "A senha deve ter no mínimo 8 caracteres, com letras, números e caracteres especiais.")]
    public string? Password { get; set; }
}

public class LoginDTO
{
    [Required(ErrorMessage = "Email obrigatório")]
    [EmailAddress(ErrorMessage = "Email formato inválido")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password obrigatório")]
    public string? Password { get; set; }
}

public class LoginResponseDTO
{
    public string? Token { get; set; }
    public UsuarioDTO? Usuario { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class ForgotPasswordDTO
{
    [Required(ErrorMessage = "Email obrigatório")]
    [EmailAddress(ErrorMessage = "Email formato inválido")]
    public string? Email { get; set; }
}

public class ResetPasswordDTO
{
    [Required(ErrorMessage = "Email obrigatório")]
    [EmailAddress(ErrorMessage = "Email formato inválido")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Token obrigatório")]
    public string? Token { get; set; }

    [Required(ErrorMessage = "Nova senha obrigatória")]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "A senha deve ter no mínimo 8 caracteres, com letras, números e caracteres especiais.")]
    public string? NewPassword { get; set; }
}

public class ForgotPasswordResponseDTO
{
    public string? Message { get; set; }
    public string? ResetToken { get; set; }
}
