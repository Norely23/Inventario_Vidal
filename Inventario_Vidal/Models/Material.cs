using System.ComponentModel.DataAnnotations;

namespace Inventario_Vital.Models
{
    public class Material
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La unidad de medida es obligatoria")]
        public string UnidadMedida { get; set; } = "UND"; // Ej: UND, KG, MT

        // Código de barras único (REQ-11, REQ-23)
        [Required]
        [StringLength(50)]
        public string CodigoBarras { get; set; } = string.Empty;

        // Para el control de stock (REQ-52, 53)
        public decimal StockActual { get; set; } = 0;

        // Para alertas (REQ-162)
        public decimal? StockMinimo { get; set; }

        public bool Activo { get; set; } = true; // Para desactivar sin borrar (REQ-16)
    }
}