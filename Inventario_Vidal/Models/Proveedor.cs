using System.ComponentModel.DataAnnotations;

namespace Inventario_Vital.Models
{
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La razón social es obligatoria")]
        public string RazonSocial { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUC es obligatorio")]
        [StringLength(11, MinimumLength = 11)]
        public string RUC { get; set; } = string.Empty;

        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }

        public bool Activo { get; set; } = true; // REQ-20, RN-25
    }
}