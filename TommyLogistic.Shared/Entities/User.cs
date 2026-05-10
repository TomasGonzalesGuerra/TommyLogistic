using TommyLogistic.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TommyLogistic.Shared.Entities;

public class User : IdentityUser
{
    [Display(Name = "Apellidos")]
    [MaxLength(50, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Documento")]
    [MaxLength(50, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    public string Document { get; set; } = null!;

    [Display(Name = "Dirección")]
    [MaxLength(50, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    public string Address { get; set; } = null!;

    [Display(Name = "Foto")]
    public string? Photo { get; set; }

    [Display(Name = "Tipo  de  usuario")]
    public UserEnum UserType { get; set; }


    // Navegaciones
    public Driver? Driver { get; set; }
    public ICollection<Company>? Companies  { get; set; }
    public ICollection<OrderEvent>? Events { get; set; }
}
