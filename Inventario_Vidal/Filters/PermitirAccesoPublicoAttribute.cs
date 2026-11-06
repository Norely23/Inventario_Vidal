namespace Inventario_Vidal.Filters
{
    /// <summary>
    /// Atributo "marcador" que permite exceptuar una acción específica de los
    /// filtros LoginRequeridoAttribute y RolRequeridoAttribute, aunque el
    /// controlador los tenga aplicados a nivel de clase.
    ///
    /// Uso: colocarlo sobre la acción que debe quedar accesible sin login
    /// (por ejemplo, el escáner que se abre desde el QR en el celular).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class PermitirAccesoPublicoAttribute : Attribute
    {
    }
}