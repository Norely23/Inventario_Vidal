using System.ComponentModel.DataAnnotations;

namespace Inventario_Vital.Models
{
    public class Proyecto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty; // REQ-25, REQ-26

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; } = DateTime.Now;

        public string Estado { get; set; } = "Activo"; // Activo, Cerrado (REQ-29, REQ-31)
    }
}