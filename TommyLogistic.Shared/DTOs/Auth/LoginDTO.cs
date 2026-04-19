using System.ComponentModel.DataAnnotations;

namespace TommyLogistic.Shared.DTOs.Auth;

public class LoginDTO
{
    [Required(ErrorMessage = "El  campo  {0}  es  obligatorio.")]
    [EmailAddress(ErrorMessage = "Debes  ingresar  un  correo  válido.")]
    public string? Email { get; set; }

    [Display(Name = "Contraseña")]
    [Required(ErrorMessage = "El  campo  {0}  es  obligatorio.")]
    [MinLength(6, ErrorMessage = "El  campo  {0}  debe  tener  al  menos  {1}  carácteres.")]
    public string? Password { get; set; }
}
