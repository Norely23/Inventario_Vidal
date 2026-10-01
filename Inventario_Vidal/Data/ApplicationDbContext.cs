using System;
using System.Collections.Generic;
using Inventario_Vidal.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AsignacionMaterialesPersonal> AsignacionMaterialesPersonals { get; set; }

    public virtual DbSet<AsignacionPersonalProyecto> AsignacionPersonalProyectos { get; set; }

    public virtual DbSet<AuditoriaStock> AuditoriaStocks { get; set; }

    public virtual DbSet<Auditorium> Auditoria { get; set; }

    public virtual DbSet<AuthToken> AuthTokens { get; set; }

    public virtual DbSet<BitacoraAcceso> BitacoraAccesos { get; set; }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Compra> Compras { get; set; }

    public virtual DbSet<ConfiguracionEmpresa> ConfiguracionEmpresas { get; set; }

    public virtual DbSet<Contratista> Contratistas { get; set; }

    public virtual DbSet<CostosProyecto> CostosProyectos { get; set; }

    public virtual DbSet<DetalleCompra> DetalleCompras { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<EstadosMaquinarium> EstadosMaquinaria { get; set; }

    public virtual DbSet<EstadosProyecto> EstadosProyectos { get; set; }

    public virtual DbSet<HistorialCodigosBarra> HistorialCodigosBarras { get; set; }

    public virtual DbSet<KardexInventario> KardexInventarios { get; set; }

    public virtual DbSet<MantenimientoMaquinarium> MantenimientoMaquinaria { get; set; }

    public virtual DbSet<Maquinaria> Maquinarias { get; set; }

    public virtual DbSet<Marca> Marcas { get; set; }

    public virtual DbSet<MaterialProveedor> MaterialProveedors { get; set; }

    public virtual DbSet<Materiale> Materiales { get; set; }

    public virtual DbSet<PaquetesInventario> PaquetesInventarios { get; set; }

    public virtual DbSet<PlanosProyecto> PlanosProyectos { get; set; }

    public virtual DbSet<PreciosProveedor> PreciosProveedors { get; set; }

    public virtual DbSet<PresentacionesMaterial> PresentacionesMaterials { get; set; }

    public virtual DbSet<PrestamosMaquinarium> PrestamosMaquinaria { get; set; }

    public virtual DbSet<Proveedore> Proveedores { get; set; }

    public virtual DbSet<Proyecto> Proyectos { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TiposCambio> TiposCambios { get; set; }

    public virtual DbSet<TiposMovimientoInventario> TiposMovimientoInventarios { get; set; }

    public virtual DbSet<TiposProyecto> TiposProyectos { get; set; }

    public virtual DbSet<UnidadesMedidum> UnidadesMedida { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<VwAlertasStockBajo> VwAlertasStockBajos { get; set; }

    public virtual DbSet<VwAsignacionesActiva> VwAsignacionesActivas { get; set; }

    public virtual DbSet<VwComprasDetallada> VwComprasDetalladas { get; set; }

    public virtual DbSet<VwConfiguracionReporte> VwConfiguracionReportes { get; set; }

    public virtual DbSet<VwCostosProyecto> VwCostosProyectos { get; set; }

    public virtual DbSet<VwDashboardIndicadore> VwDashboardIndicadores { get; set; }

    public virtual DbSet<VwDesglosePaquetesMaterial> VwDesglosePaquetesMaterials { get; set; }

    public virtual DbSet<VwHistorialAsignacionesPorPersona> VwHistorialAsignacionesPorPersonas { get; set; }

    public virtual DbSet<VwKardexDetallado> VwKardexDetallados { get; set; }

    public virtual DbSet<VwMantenimientoMaquinarium> VwMantenimientoMaquinaria { get; set; }

    public virtual DbSet<VwMaquinariaDisponible> VwMaquinariaDisponibles { get; set; }

    public virtual DbSet<VwMaterialesConProveedor> VwMaterialesConProveedors { get; set; }

    public virtual DbSet<VwPaquetesDetalle> VwPaquetesDetalles { get; set; }

    public virtual DbSet<VwPaquetesInconsistencia> VwPaquetesInconsistencias { get; set; }

    public virtual DbSet<VwPerfilUsuario> VwPerfilUsuarios { get; set; }

    public virtual DbSet<VwPersonalActivoPorProyecto> VwPersonalActivoPorProyectos { get; set; }

    public virtual DbSet<VwPresentacionesPorMaterial> VwPresentacionesPorMaterials { get; set; }

    public virtual DbSet<VwProyectosResuman> VwProyectosResumen { get; set; }

    public virtual DbSet<VwResumenInventarioPorCategorium> VwResumenInventarioPorCategoria { get; set; }

    public virtual DbSet<VwResumenMaterialesPorProyecto> VwResumenMaterialesPorProyectos { get; set; }

    public virtual DbSet<VwSesionesActiva> VwSesionesActivas { get; set; }

    public virtual DbSet<VwStockActual> VwStockActuals { get; set; }

    public virtual DbSet<VwUltimosMovimientosKardex> VwUltimosMovimientosKardices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // La cadena de conexión se lee desde appsettings.json a través de Program.cs
        // No es necesario configurarla aquí.
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AsignacionMaterialesPersonal>(entity =>
        {
            entity.HasKey(e => e.IdAsignacion);

            entity.ToTable("AsignacionMaterialesPersonal", "Proyectos");

            entity.HasIndex(e => e.IdContratista, "IX_AsigMatPers_Contratista").HasFilter("([IdContratista] IS NOT NULL)");

            entity.HasIndex(e => e.IdEmpleado, "IX_AsigMatPers_Empleado").HasFilter("([IdEmpleado] IS NOT NULL)");

            entity.HasIndex(e => e.Estado, "IX_AsigMatPers_Estado");

            entity.HasIndex(e => e.IdMaterial, "IX_AsigMatPers_Material");

            entity.HasIndex(e => e.IdProyecto, "IX_AsigMatPers_Proyecto");

            entity.Property(e => e.CantidadDanada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadDevuelta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadEntregada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadSolicitada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadUtilizada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CostoUnitarioRef).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Entregado", "DF_AsigMatPers_Estado");
            entity.Property(e => e.FechaAsignacion).HasDefaultValueSql("(sysdatetime())", "DF_AsigMatPers_Fecha");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.IdContratistaNavigation).WithMany(p => p.AsignacionMaterialesPersonals)
                .HasForeignKey(d => d.IdContratista)
                .HasConstraintName("FK_AsigMatPers_Contratista");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.AsignacionMaterialesPersonals)
                .HasForeignKey(d => d.IdEmpleado)
                .HasConstraintName("FK_AsigMatPers_Empleado");

            entity.HasOne(d => d.IdMaterialNavigation).WithMany(p => p.AsignacionMaterialesPersonals)
                .HasForeignKey(d => d.IdMaterial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AsigMatPers_Material");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.AsignacionMaterialesPersonals)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AsigMatPers_Proyecto");

            entity.HasOne(d => d.IdUsuarioRegistroNavigation).WithMany(p => p.AsignacionMaterialesPersonals)
                .HasForeignKey(d => d.IdUsuarioRegistro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AsigMatPers_Usuario");
        });

        modelBuilder.Entity<AsignacionPersonalProyecto>(entity =>
        {
            entity.HasKey(e => e.IdAsignacion);

            entity.ToTable("AsignacionPersonalProyecto", "Proyectos");

            entity.HasIndex(e => e.IdProyecto, "IX_AsigPersProy_Activos").HasFilter("([FechaHoraFin] IS NULL)");

            entity.HasIndex(e => e.IdProyecto, "IX_AsigPersProy_Proyecto");

            entity.HasIndex(e => e.IdContratista, "UQ_AsigPersProy_ContratistaActivo")
                .IsUnique()
                .HasFilter("([IdContratista] IS NOT NULL AND [FechaHoraFin] IS NULL)");

            entity.HasIndex(e => e.IdEmpleado, "UQ_AsigPersProy_EmpleadoActivo")
                .IsUnique()
                .HasFilter("([IdEmpleado] IS NOT NULL AND [FechaHoraFin] IS NULL)");

            entity.Property(e => e.FechaHoraInicio).HasDefaultValueSql("(sysdatetime())", "DF_AsigPersProy_Inicio");
            entity.Property(e => e.Motivo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RolEnProyecto)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdContratistaNavigation).WithOne(p => p.AsignacionPersonalProyecto)
                .HasForeignKey<AsignacionPersonalProyecto>(d => d.IdContratista)
                .HasConstraintName("FK_AsigPersProy_Contratista");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithOne(p => p.AsignacionPersonalProyecto)
                .HasForeignKey<AsignacionPersonalProyecto>(d => d.IdEmpleado)
                .HasConstraintName("FK_AsigPersProy_Empleado");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.AsignacionPersonalProyectos)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AsigPersProy_Proyecto");

            entity.HasOne(d => d.IdUsuarioRegistroNavigation).WithMany(p => p.AsignacionPersonalProyectos)
                .HasForeignKey(d => d.IdUsuarioRegistro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AsigPersProy_Usuario");
        });

        modelBuilder.Entity<AuditoriaStock>(entity =>
        {
            entity.HasKey(e => e.IdAuditoriaStock);

            entity.ToTable("AuditoriaStock", "Inventario");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaHora).HasDefaultValueSql("(sysdatetime())", "DF_AuditoriaStock_Fecha");
            entity.Property(e => e.NombreMaterial)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.StockAnterior).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockPosterior).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.AuditoriaStocks)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditoriaStock_Usuario");
        });

        modelBuilder.Entity<Auditorium>(entity =>
        {
            entity.HasKey(e => e.IdAuditoria);

            entity.HasIndex(e => e.FechaHora, "IX_Auditoria_Fecha").IsDescending();

            entity.HasIndex(e => new { e.NombreTabla, e.FechaHora }, "IX_Auditoria_Tabla_Fecha").IsDescending(false, true);

            entity.Property(e => e.Accion)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FechaHora).HasDefaultValueSql("(sysdatetime())", "DF_Auditoria_FechaHora");
            entity.Property(e => e.IdRegistroAfectado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreTabla)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Auditoria)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_Auditoria_Usuario");
        });

        modelBuilder.Entity<AuthToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.ToTable("AuthTokens", "Seguridad");

            entity.HasIndex(e => new { e.Activo, e.FechaExpiracion }, "IX_AuthTokens_Activo");

            entity.HasIndex(e => e.FechaExpiracion, "IX_AuthTokens_Expiracion");

            entity.HasIndex(e => e.Token, "IX_AuthTokens_Token");

            entity.HasIndex(e => e.UsuarioId, "IX_AuthTokens_Usuario");

            entity.HasIndex(e => e.Token, "UQ_AuthTokens_Token").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_AuthTokens_Activo");
            entity.Property(e => e.Dispositivo).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())", "DF_AuthTokens_FechaCreacion")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaExpiracion).HasColumnType("datetime");
            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .HasColumnName("IP");
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.Property(e => e.UserAgent).HasMaxLength(300);

            entity.HasOne(d => d.Usuario).WithMany(p => p.AuthTokens)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuthTokens_Usuarios");
        });

        modelBuilder.Entity<BitacoraAcceso>(entity =>
        {
            entity.HasKey(e => e.IdBitacora);

            entity.ToTable("BitacoraAccesos", "Seguridad");

            entity.Property(e => e.Accion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaHora).HasDefaultValueSql("(sysdatetime())", "DF_BitacoraAccesos_Fecha");
            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("IP");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserAgent)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.Usuario).WithMany(p => p.BitacoraAccesos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_BitacoraAccesos_Usuario");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.IdCategoria);

            entity.ToTable("Categorias", "Inventario");

            entity.HasIndex(e => e.Nombre, "UQ_Categorias_Nombre").IsUnique();

            entity.HasIndex(e => e.Prefijo, "UQ_Categorias_Prefijo").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Categorias_Activo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Categorias_FechaRegistro");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Prefijo)
                .HasMaxLength(5)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente);

            entity.ToTable("Clientes", "Clientes", tb =>
                {
                    tb.HasTrigger("TR_Clientes_Auditoria");
                    tb.HasTrigger("TR_Clientes_BorradoLogico");
                });

            entity.HasIndex(e => e.NumeroDocumento, "UQ_Clientes_Documento").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Clientes_Activo");
            entity.Property(e => e.ContactoNombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Departamento)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Distrito)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Clientes_FechaRegistro");
            entity.Property(e => e.NombreComercial)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.NombreOrazonSocial)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("NombreORazonSocial");
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Provincia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TipoCliente)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Juridico", "DF_Clientes_TipoCliente");
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.IdCompra);

            entity.ToTable("Compras", "Compras", tb => tb.HasTrigger("TR_Compras_Auditoria"));

            entity.HasIndex(e => e.Estado, "IX_Compras_Estado");

            entity.HasIndex(e => e.FechaCompra, "IX_Compras_Fecha").IsDescending();

            entity.HasIndex(e => new { e.IdProveedor, e.Estado }, "IX_Compras_Proveedor");

            entity.HasIndex(e => new { e.IdProveedor, e.NumeroComprobante }, "UQ_Compras_Comprobante").IsUnique();

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Registrada", "DF_Compras_Estado");
            entity.Property(e => e.FechaCompra).HasDefaultValueSql("(sysdatetime())", "DF_Compras_FechaCompra");
            entity.Property(e => e.Igv)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_Compras_Moneda");
            entity.Property(e => e.NumeroComprobante)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoComprobante)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Factura", "DF_Compras_TipoComprobante");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compras_Proveedor");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compras_Usuario");
        });

        modelBuilder.Entity<ConfiguracionEmpresa>(entity =>
        {
            entity.HasKey(e => e.IdConfiguracion);

            entity.ToTable("ConfiguracionEmpresa", "Configuracion");

            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Igv)
                .HasDefaultValue(18.00m, "DF_ConfigEmpresa_IGV")
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_ConfigEmpresa_Moneda");
            entity.Property(e => e.MonedaSecundaria)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("USD", "DF_ConfigEmpresa_MonedaSec");
            entity.Property(e => e.NombreEmpresa)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PiePagina)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("RUC");
            entity.Property(e => e.SitioWeb)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TipoCambio)
                .HasDefaultValue(3.80m, "DF_ConfigEmpresa_TipoCambio")
                .HasColumnType("decimal(10, 4)");
        });

        modelBuilder.Entity<Contratista>(entity =>
        {
            entity.HasKey(e => e.IdContratista);

            entity.ToTable("Contratistas", "Contratistas", tb => tb.HasTrigger("TR_Contratistas_BorradoLogico"));

            entity.HasIndex(e => e.NumeroDocumento, "UQ_Contratistas_Documento").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Contratistas_Activo");
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Especialidad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Contratistas_FechaRegistro");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("DNI", "DF_Contratistas_TipoDoc");
        });

        modelBuilder.Entity<CostosProyecto>(entity =>
        {
            entity.HasKey(e => e.IdCosto);

            entity.ToTable("CostosProyecto", "Proyectos");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(CONVERT([date],sysdatetime()))", "DF_CostoProy_Fecha");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_CostoProy_Moneda");
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoCosto)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.CostosProyectos)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostoProy_Proyecto");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.CostosProyectos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostoProy_Usuario");
        });

        modelBuilder.Entity<DetalleCompra>(entity =>
        {
            entity.HasKey(e => e.IdDetalleCompra);

            entity.ToTable("DetalleCompras", "Compras");

            entity.HasIndex(e => e.IdCompra, "IX_DetalleCompras_Compra");

            entity.HasIndex(e => e.IdMaterial, "IX_DetalleCompras_Material");

            entity.HasIndex(e => e.IdPresentacion, "IX_DetalleCompras_Presentacion").HasFilter("([IdPresentacion] IS NOT NULL)");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadRecibida).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadUnidadBase)
                .HasComputedColumnSql("([Cantidad]*isnull([FactorConversionUsado],(1)))", true)
                .HasColumnType("decimal(37, 8)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.FactorConversionUsado).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Lote)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_DetCompra_Moneda");
            entity.Property(e => e.Subtotal)
                .HasComputedColumnSql("([Cantidad]*[CostoUnitario])", true)
                .HasColumnType("decimal(37, 8)");

            entity.HasOne(d => d.IdCompraNavigation).WithMany(p => p.DetalleCompras)
                .HasForeignKey(d => d.IdCompra)
                .HasConstraintName("FK_DetCompra_Compra");

            entity.HasOne(d => d.IdMaterialNavigation).WithMany(p => p.DetalleCompras)
                .HasForeignKey(d => d.IdMaterial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetCompra_Material");

            entity.HasOne(d => d.IdPresentacionNavigation).WithMany(p => p.DetalleCompras)
                .HasForeignKey(d => d.IdPresentacion)
                .HasConstraintName("FK_DetCompra_Presentacion");

            entity.HasOne(d => d.IdUnidadCompraNavigation).WithMany(p => p.DetalleCompras)
                .HasForeignKey(d => d.IdUnidadCompra)
                .HasConstraintName("FK_DetCompra_UnidadCompra");
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.IdEmpleado);

            entity.ToTable("Empleados", "Empleados", tb => tb.HasTrigger("TR_Empleados_BorradoLogico"));

            entity.HasIndex(e => e.CorreoElectronico, "UQ_Empleados_Correo").IsUnique();

            entity.HasIndex(e => e.NumeroDocumento, "UQ_Empleados_Documento").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Empleados_Activo");
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Especialidad)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Empleados_FechaRegistro");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EstadosMaquinarium>(entity =>
        {
            entity.HasKey(e => e.IdEstadoMaquinaria);

            entity.ToTable("EstadosMaquinaria", "Maquinaria");

            entity.HasIndex(e => e.Nombre, "UQ_EstadosMaquinaria_Nombre").IsUnique();

            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EstadosProyecto>(entity =>
        {
            entity.HasKey(e => e.IdEstado);

            entity.ToTable("EstadosProyecto", "Proyectos");

            entity.HasIndex(e => e.Nombre, "UQ_EstadosProyecto_Nombre").IsUnique();

            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<HistorialCodigosBarra>(entity =>
        {
            entity.HasKey(e => e.IdHistorial);

            entity.ToTable("HistorialCodigosBarras", "Inventario");

            entity.Property(e => e.CodigoBarrasNuevo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CodigoBarrasViejo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaCambio).HasDefaultValueSql("(sysdatetime())", "DF_HistorialCodigos_Fecha");
            entity.Property(e => e.Motivo)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.IdMaterialNavigation).WithMany(p => p.HistorialCodigosBarras)
                .HasForeignKey(d => d.IdMaterial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistorialCodigos_Material");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HistorialCodigosBarras)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistorialCodigos_Usuario");
        });

        modelBuilder.Entity<KardexInventario>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento);

            entity.ToTable("KardexInventario", "Inventario", tb =>
                {
                    tb.HasTrigger("TR_Kardex_ActualizarPaquetes");
                    tb.HasTrigger("TR_Kardex_ActualizarStock");
                    tb.HasTrigger("TR_Kardex_AuditoriaStock");
                    tb.HasTrigger("TR_Kardex_ImpedirEdicion");
                });

            entity.HasIndex(e => e.IdCompra, "IX_Kardex_Compra").HasFilter("([IdCompra] IS NOT NULL)");

            entity.HasIndex(e => e.IdContratista, "IX_Kardex_Contratista").HasFilter("([IdContratista] IS NOT NULL)");

            entity.HasIndex(e => e.IdEmpleado, "IX_Kardex_Empleado");

            entity.HasIndex(e => e.FechaMovimiento, "IX_Kardex_Fecha").IsDescending();

            entity.HasIndex(e => new { e.IdMaterial, e.FechaMovimiento }, "IX_Kardex_Material_Fecha").IsDescending(false, true);

            entity.HasIndex(e => e.IdProyecto, "IX_Kardex_Proyecto").HasFilter("([IdProyecto] IS NOT NULL)");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadUnidadOrigen).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.DocumentoReferencia)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaMovimiento).HasDefaultValueSql("(sysdatetime())", "DF_Kardex_FechaMovimiento");
            entity.Property(e => e.Observacion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.StockAnterior).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockPosterior).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.IdCompraNavigation).WithMany(p => p.KardexInventarios)
                .HasForeignKey(d => d.IdCompra)
                .HasConstraintName("FK_Kardex_Compra");

            entity.HasOne(d => d.IdContratistaNavigation).WithMany(p => p.KardexInventarios)
                .HasForeignKey(d => d.IdContratista)
                .HasConstraintName("FK_Kardex_Contratista");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.KardexInventarios)
                .HasForeignKey(d => d.IdEmpleado)
                .HasConstraintName("FK_Kardex_Empleado");

            entity.HasOne(d => d.IdMaterialNavigation).WithMany(p => p.KardexInventarios)
                .HasForeignKey(d => d.IdMaterial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Material");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.KardexInventarios)
                .HasForeignKey(d => d.IdProyecto)
                .HasConstraintName("FK_Kardex_Proyecto");

            entity.HasOne(d => d.IdTipoMovimientoNavigation).WithMany(p => p.KardexInventarios)
                .HasForeignKey(d => d.IdTipoMovimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Tipo");

            entity.HasOne(d => d.IdUnidadOrigenNavigation).WithMany(p => p.KardexInventarios)
                .HasForeignKey(d => d.IdUnidadOrigen)
                .HasConstraintName("FK_Kardex_UnidadOrigen");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.KardexInventarios)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Usuario");
        });

        modelBuilder.Entity<MantenimientoMaquinarium>(entity =>
        {
            entity.HasKey(e => e.IdMantenimiento);

            entity.ToTable("MantenimientoMaquinaria", "Maquinaria");

            entity.Property(e => e.Costo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_MantMaq_Moneda");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TipoMantenimiento)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Preventivo", "DF_MantMaq_Tipo");

            entity.HasOne(d => d.IdMaquinariaNavigation).WithMany(p => p.MantenimientoMaquinaria)
                .HasForeignKey(d => d.IdMaquinaria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MantMaq_Maquinaria");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.MantenimientoMaquinaria)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MantMaq_Usuario");
        });

        modelBuilder.Entity<Maquinaria>(entity =>
        {
            entity.HasKey(e => e.IdMaquinaria);

            entity.ToTable("Maquinarias", "Maquinaria", tb => tb.HasTrigger("TR_Maquinarias_BorradoLogico"));

            entity.HasIndex(e => e.Codigo, "IX_Maquinarias_Codigo");

            entity.HasIndex(e => e.IdEstadoMaquinaria, "IX_Maquinarias_Estado").HasFilter("([Activo]=(1))");

            entity.HasIndex(e => e.Codigo, "UQ_Maquinarias_Codigo").IsUnique();

            entity.HasIndex(e => e.NumeroSerie, "UQ_Maquinarias_NumeroSerie").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Maquinarias_Activo");
            entity.Property(e => e.Codigo)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CostoAdquisicion).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Maquinarias_FechaRegistro");
            entity.Property(e => e.HorasUso).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_Maquinarias_Moneda");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NumeroSerie)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdEstadoMaquinariaNavigation).WithMany(p => p.Maquinaria)
                .HasForeignKey(d => d.IdEstadoMaquinaria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maquinarias_Estado");
        });

        modelBuilder.Entity<Marca>(entity =>
        {
            entity.HasKey(e => e.IdMarca);

            entity.ToTable("Marcas", "Inventario");

            entity.HasIndex(e => e.Nombre, "UQ_Marcas_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Marcas_Activo");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Marcas_FechaRegistro");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MaterialProveedor>(entity =>
        {
            entity.HasKey(e => e.IdMaterialProveedor);

            entity.ToTable("MaterialProveedor", "Inventario");

            entity.HasIndex(e => new { e.IdMaterial, e.IdProveedor }, "UQ_MaterialProveedor").IsUnique();

            entity.Property(e => e.CodigoProveedor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(sysdatetime())", "DF_MatProv_FechaActualizacion");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_MatProv_Moneda");
            entity.Property(e => e.PrecioMayorRef).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.PrecioMenorRef).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UltimoPrecioCompra).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.IdMaterialNavigation).WithMany(p => p.MaterialProveedors)
                .HasForeignKey(d => d.IdMaterial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MatProv_Material");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.MaterialProveedors)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MatProv_Proveedor");
        });

        modelBuilder.Entity<Materiale>(entity =>
        {
            entity.HasKey(e => e.IdMaterial);

            entity.ToTable("Materiales", "Inventario", tb =>
                {
                    tb.HasTrigger("TR_Materiales_Auditoria");
                    tb.HasTrigger("TR_Materiales_BorradoLogico");
                    tb.HasTrigger("TR_Materiales_HistorialCodigoBarras");
                });

            entity.HasIndex(e => e.Activo, "IX_Materiales_Activo");

            entity.HasIndex(e => new { e.CodigoBarras, e.Nombre }, "IX_Materiales_Busqueda").HasFilter("([Activo]=(1))");

            entity.HasIndex(e => e.IdCategoria, "IX_Materiales_Categoria");

            entity.HasIndex(e => e.CodigoBarras, "IX_Materiales_CodigoBarras");

            entity.HasIndex(e => e.Nombre, "IX_Materiales_Nombre");

            entity.HasIndex(e => new { e.StockActual, e.StockMinimo }, "IX_Materiales_StockBajo");

            entity.HasIndex(e => e.CodigoBarras, "UQ_Materiales_CodigoBarras").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Materiales_Activo");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Dimensiones)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FactorConversion)
                .HasDefaultValue(1m, "DF_Materiales_FactorConversion")
                .HasColumnType("decimal(18, 4)");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Materiales_FechaRegistro");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PesoUnitario).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockActual).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockMaximo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockMinimo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockReservado).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Materiales)
                .HasForeignKey(d => d.IdCategoria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Materiales_Categorias");

            entity.HasOne(d => d.IdMarcaNavigation).WithMany(p => p.Materiales)
                .HasForeignKey(d => d.IdMarca)
                .HasConstraintName("FK_Materiales_Marcas");

            entity.HasOne(d => d.IdUnidadCompraNavigation).WithMany(p => p.MaterialeIdUnidadCompraNavigations)
                .HasForeignKey(d => d.IdUnidadCompra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Materiales_UnidadCompra");

            entity.HasOne(d => d.IdUnidadConsumoNavigation).WithMany(p => p.MaterialeIdUnidadConsumoNavigations)
                .HasForeignKey(d => d.IdUnidadConsumo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Materiales_UnidadConsumo");
        });

        modelBuilder.Entity<PaquetesInventario>(entity =>
        {
            entity.HasKey(e => e.IdPaquete);

            entity.ToTable("PaquetesInventario", "Inventario");

            entity.HasIndex(e => e.IdDetalleCompra, "IX_Paquetes_DetalleCompra").HasFilter("([IdDetalleCompra] IS NOT NULL)");

            entity.HasIndex(e => new { e.IdMaterial, e.Estado }, "IX_Paquetes_Material_Estado");

            entity.Property(e => e.CantidadActualUnidadBase).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CapacidadUnidadBase).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Sellado", "DF_Paquetes_Estado");
            entity.Property(e => e.FechaIngreso).HasDefaultValueSql("(sysdatetime())", "DF_Paquetes_FechaIngreso");
            entity.Property(e => e.NombrePresentacion)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdDetalleCompraNavigation).WithMany(p => p.PaquetesInventarios)
                .HasForeignKey(d => d.IdDetalleCompra)
                .HasConstraintName("FK_Paquetes_DetalleCompra");

            entity.HasOne(d => d.IdMaterialNavigation).WithMany(p => p.PaquetesInventarios)
                .HasForeignKey(d => d.IdMaterial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Paquetes_Material");

            entity.HasOne(d => d.IdPresentacionNavigation).WithMany(p => p.PaquetesInventarios)
                .HasForeignKey(d => d.IdPresentacion)
                .HasConstraintName("FK_Paquetes_Presentacion");
        });

        modelBuilder.Entity<PlanosProyecto>(entity =>
        {
            entity.HasKey(e => e.IdPlano);

            entity.ToTable("PlanosProyecto", "Proyectos");

            entity.Property(e => e.EsVersionActual).HasDefaultValue(true, "DF_Planos_EsVersionActual");
            entity.Property(e => e.FechaSubida).HasDefaultValueSql("(sysdatetime())", "DF_Planos_FechaSubida");
            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RutaArchivo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1, "DF_Planos_Version");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.PlanosProyectos)
                .HasForeignKey(d => d.IdProyecto)
                .HasConstraintName("FK_Planos_Proyecto");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.PlanosProyectos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Planos_Usuario");
        });

        modelBuilder.Entity<PreciosProveedor>(entity =>
        {
            entity.HasKey(e => e.IdPrecioProveedor);

            entity.ToTable("PreciosProveedor", "Compras", tb => tb.HasTrigger("TR_PreciosProveedor_ActualizarReferencia"));

            entity.HasIndex(e => e.FechaInicio, "IX_PreciosProveedor_Fecha").IsDescending();

            entity.HasIndex(e => e.IdMaterialProveedor, "IX_PreciosProveedor_Material");

            entity.HasIndex(e => new { e.EsVigente, e.FechaInicio }, "IX_PreciosProveedor_Vigente");

            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.EsVigente).HasDefaultValue(true, "DF_PreciosProveedor_Vigente");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_PreciosProveedor_FechaRegistro");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_PreciosProveedor_Moneda");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TipoPrecio)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Normal", "DF_PreciosProveedor_Tipo");

            entity.HasOne(d => d.IdMaterialProveedorNavigation).WithMany(p => p.PreciosProveedors)
                .HasForeignKey(d => d.IdMaterialProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PreciosProveedor_MaterialProveedor");

            entity.HasOne(d => d.UsuarioRegistroNavigation).WithMany(p => p.PreciosProveedors)
                .HasForeignKey(d => d.UsuarioRegistro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PreciosProveedor_Usuario");
        });

        modelBuilder.Entity<PresentacionesMaterial>(entity =>
        {
            entity.HasKey(e => e.IdPresentacion);

            entity.ToTable("PresentacionesMaterial", "Inventario");

            entity.HasIndex(e => e.IdMaterial, "IX_PresentacionesMaterial_Material").HasFilter("([Activo]=(1))");

            entity.HasIndex(e => new { e.IdMaterial, e.Nombre }, "UQ_PresentMat_MaterialNombre").IsUnique();

            entity.HasIndex(e => e.IdMaterial, "UQ_PresentacionesMaterial_Predeterminada")
                .IsUnique()
                .HasFilter("([EsPredeterminada]=(1) AND [Activo]=(1))");

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_PresentMat_Activo");
            entity.Property(e => e.FactorAunidadBase)
                .HasColumnType("decimal(18, 4)")
                .HasColumnName("FactorAUnidadBase");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_PresentMat_FechaRegistro");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdMaterialNavigation).WithOne(p => p.PresentacionesMaterial)
                .HasForeignKey<PresentacionesMaterial>(d => d.IdMaterial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PresentMat_Material");

            entity.HasOne(d => d.IdUnidadPresentacionNavigation).WithMany(p => p.PresentacionesMaterials)
                .HasForeignKey(d => d.IdUnidadPresentacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PresentMat_Unidad");
        });

        modelBuilder.Entity<PrestamosMaquinarium>(entity =>
        {
            entity.HasKey(e => e.IdPrestamo);

            entity.ToTable("PrestamosMaquinaria", "Maquinaria");

            entity.HasIndex(e => e.IdEmpleado, "IX_PrestMaq_Empleado");

            entity.HasIndex(e => new { e.Estado, e.FechaDevolucionEstimada }, "IX_PrestMaq_Estado");

            entity.HasIndex(e => e.IdMaquinaria, "IX_PrestMaq_Maquinaria");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Prestado", "DF_PrestMaq_Estado");
            entity.Property(e => e.FechaPrestamo).HasDefaultValueSql("(sysdatetime())", "DF_PrestMaq_Fecha");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.IdContratistaNavigation).WithMany(p => p.PrestamosMaquinaria)
                .HasForeignKey(d => d.IdContratista)
                .HasConstraintName("FK_PrestMaq_Contratista");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.PrestamosMaquinaria)
                .HasForeignKey(d => d.IdEmpleado)
                .HasConstraintName("FK_PrestMaq_Empleado");

            entity.HasOne(d => d.IdMaquinariaNavigation).WithMany(p => p.PrestamosMaquinaria)
                .HasForeignKey(d => d.IdMaquinaria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrestMaq_Maquinaria");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.PrestamosMaquinaria)
                .HasForeignKey(d => d.IdProyecto)
                .HasConstraintName("FK_PrestMaq_Proyecto");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.PrestamosMaquinaria)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrestMaq_Usuario");
        });

        modelBuilder.Entity<Proveedore>(entity =>
        {
            entity.HasKey(e => e.IdProveedor);

            entity.ToTable("Proveedores", "Compras", tb => tb.HasTrigger("TR_Proveedores_BorradoLogico"));

            entity.HasIndex(e => e.Ruc, "UQ_Proveedores_RUC").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Proveedores_Activo");
            entity.Property(e => e.Clasificacion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CondicionesPago)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ContactoNombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Proveedores_FechaRegistro");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("RUC");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Proyecto>(entity =>
        {
            entity.HasKey(e => e.IdProyecto);

            entity.ToTable("Proyectos", "Proyectos", tb => tb.HasTrigger("TR_Proyectos_Auditoria"));

            entity.HasIndex(e => e.IdCliente, "IX_Proyectos_Cliente");

            entity.HasIndex(e => e.CodigoProyecto, "IX_Proyectos_Codigo");

            entity.HasIndex(e => e.IdEstado, "IX_Proyectos_Estado");

            entity.HasIndex(e => e.FechaEntregaEstimada, "IX_Proyectos_FechaEntrega").HasFilter("([IdEstado] IN ((1), (2), (3)))");

            entity.HasIndex(e => e.CodigoProyecto, "UQ_Proyectos_Codigo").IsUnique();

            entity.Property(e => e.CodigoProyecto)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CostoEstimado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostoReal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_Proyectos_FechaRegistro");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_Proyectos_Moneda");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Prioridad)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Media", "DF_Proyectos_Prioridad");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proyectos_Cliente");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proyectos_Estado");

            entity.HasOne(d => d.IdTipoProyectoNavigation).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.IdTipoProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proyectos_Tipo");

            entity.HasOne(d => d.IdUsuarioResponsableNavigation).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.IdUsuarioResponsable)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proyectos_Usuario");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol);

            entity.ToTable("Roles", "Seguridad");

            entity.HasIndex(e => e.NombreRol, "UQ_Roles_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Roles_Activo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_Roles_FechaCreacion");
            entity.Property(e => e.NombreRol)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TiposCambio>(entity =>
        {
            entity.HasKey(e => e.IdTipoCambio);

            entity.ToTable("TiposCambio", "Configuracion");

            entity.HasIndex(e => e.Fecha, "IX_TiposCambio_Fecha").IsDescending();

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())", "DF_TiposCambio_FechaRegistro");
            entity.Property(e => e.MonedaDestino)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PEN", "DF_TiposCambio_MonedaDestino");
            entity.Property(e => e.MonedaOrigen)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("USD", "DF_TiposCambio_MonedaOrigen");
            entity.Property(e => e.Valor).HasColumnType("decimal(10, 4)");

            entity.HasOne(d => d.Usuario).WithMany(p => p.TiposCambios)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_TiposCambio_Usuario");
        });

        modelBuilder.Entity<TiposMovimientoInventario>(entity =>
        {
            entity.HasKey(e => e.IdTipoMovimiento);

            entity.ToTable("TiposMovimientoInventario", "Inventario");

            entity.HasIndex(e => e.Nombre, "UQ_TiposMovimiento_Nombre").IsUnique();

            entity.Property(e => e.Clasificacion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TiposProyecto>(entity =>
        {
            entity.HasKey(e => e.IdTipoProyecto);

            entity.ToTable("TiposProyecto", "Proyectos");

            entity.HasIndex(e => e.Nombre, "UQ_TiposProyecto_Nombre").IsUnique();

            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UnidadesMedidum>(entity =>
        {
            entity.HasKey(e => e.IdUnidad);

            entity.ToTable("UnidadesMedida", "Inventario");

            entity.HasIndex(e => e.Abreviatura, "UQ_UnidadesMedida_Abreviatura").IsUnique();

            entity.HasIndex(e => e.Nombre, "UQ_UnidadesMedida_Nombre").IsUnique();

            entity.Property(e => e.Abreviatura)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("Usuarios", "Seguridad");

            entity.HasIndex(e => e.Activo, "IX_Usuarios_Activo");

            entity.HasIndex(e => e.CorreoElectronico, "IX_Usuarios_Correo").HasFilter("([CorreoElectronico] IS NOT NULL)");

            entity.HasIndex(e => e.NombreUsuario, "IX_Usuarios_NombreUsuario");

            entity.HasIndex(e => e.CorreoElectronico, "UQ_Usuarios_Correo").IsUnique();

            entity.HasIndex(e => e.NombreUsuario, "UQ_Usuarios_NombreUsuario").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Usuarios_Activo");
            entity.Property(e => e.ContrasenaHash)
                .HasMaxLength(256)
                .IsUnicode(false);
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_Usuarios_FechaCreacion");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Roles");
        });

        modelBuilder.Entity<VwAlertasStockBajo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AlertasStockBajo");

            entity.Property(e => e.CantidadSueltaEnUnidadBase).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.Categoria)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EstadoStock)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.FactorConversionVigente).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PresentacionPredeterminada)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PresentacionesCompletas).HasColumnType("decimal(38, 0)");
            entity.Property(e => e.StockActual).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockDisponible).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.StockMaximo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockMinimo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockReservado).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnidadConsumo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnidadPresentacionPredeterminada)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwAsignacionesActiva>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AsignacionesActivas");

            entity.Property(e => e.CantidadDanada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadDevuelta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadEntregada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadSolicitada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadUtilizada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CodigoProyecto)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CostoUnitarioRef).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.DocumentoPersona)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.NombrePersona)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Pendiente).HasColumnType("decimal(21, 4)");
            entity.Property(e => e.ProyectoDescripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RegistradoPor)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.TipoPersona)
                .HasMaxLength(11)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwComprasDetallada>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ComprasDetalladas");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadUnidadBase).HasColumnType("decimal(37, 8)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CostoUnitarioPen)
                .HasColumnType("decimal(29, 8)")
                .HasColumnName("CostoUnitario_PEN");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FactorConversionUsado).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Material)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NumeroComprobante)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Presentacion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Proveedor)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(37, 8)");
            entity.Property(e => e.TipoComprobante)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwConfiguracionReporte>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ConfiguracionReporte");

            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Igv)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MonedaSecundaria)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NombreEmpresa)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PiePagina)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("RUC");
            entity.Property(e => e.SitioWeb)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(10, 4)");
        });

        modelBuilder.Entity<VwCostosProyecto>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CostosProyecto");

            entity.Property(e => e.CodigoProyecto)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MontoPen)
                .HasColumnType("decimal(29, 6)")
                .HasColumnName("Monto_PEN");
            entity.Property(e => e.RegistradoPor)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.TipoCosto)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwDashboardIndicadore>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DashboardIndicadores");

            entity.Property(e => e.ComprasUltimoMes).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.FechaActualizacion).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwDesglosePaquetesMaterial>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DesglosePaquetesMaterial");

            entity.Property(e => e.CapacidadUnidadBase).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Presentacion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.StockActualMaterial).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TotalUnidadesEnPaquetes).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.UnidadesEnAbiertos).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.UnidadesEnSellados).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<VwHistorialAsignacionesPorPersona>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_HistorialAsignacionesPorPersona");

            entity.Property(e => e.CantidadDanada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadDevuelta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadEntregada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadSolicitada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadUtilizada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CodigoProyecto)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CostoUnitarioRef).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.DocumentoPersona)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.NombrePersona)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Pendiente).HasColumnType("decimal(21, 4)");
            entity.Property(e => e.ProyectoDescripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RegistradoPor)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.TipoPersona)
                .HasMaxLength(11)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwKardexDetallado>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_KardexDetallado");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CantidadUnidadOrigen).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CodigoProyecto)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Comprobante)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Contratista)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Empleado)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Observacion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RegistradoPor)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.StockAnterior).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockPosterior).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnidadOrigen)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwMantenimientoMaquinarium>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MantenimientoMaquinaria");

            entity.Property(e => e.CodigoMaquinaria)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Costo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostoPen)
                .HasColumnType("decimal(29, 6)")
                .HasColumnName("Costo_PEN");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Maquinaria)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RegistradoPor)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.TipoMantenimiento)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwMaquinariaDisponible>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MaquinariaDisponible");

            entity.Property(e => e.Codigo)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwMaterialesConProveedor>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MaterialesConProveedor");

            entity.Property(e => e.Categoria)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.EstadoStock)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MonedaCosto)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ProveedorPrincipal)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ProveedorRuc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ProveedorRUC");
            entity.Property(e => e.StockActual).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockDisponible).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.StockMinimo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockReservado).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UltimoCosto).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UltimoPrecioCompra).HasColumnType("decimal(18, 4)");
        });

        modelBuilder.Entity<VwPaquetesDetalle>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PaquetesDetalle");

            entity.Property(e => e.CantidadActualUnidadBase).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CapacidadUnidadBase).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Presentacion)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwPaquetesInconsistencia>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PaquetesInconsistencias");

            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Diferencia).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.StockActual).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TotalEnPaquetes).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<VwPerfilUsuario>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PerfilUsuario");

            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NombreRol)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RolDescripcion)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwPersonalActivoPorProyecto>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PersonalActivoPorProyecto");

            entity.Property(e => e.CodigoProyecto)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Motivo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.NombrePersonal)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RegistradoPor)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.RolEnProyecto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TipoPersonal)
                .HasMaxLength(11)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwPresentacionesPorMaterial>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PresentacionesPorMaterial");

            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FactorAunidadBase)
                .HasColumnType("decimal(18, 4)")
                .HasColumnName("FactorAUnidadBase");
            entity.Property(e => e.Material)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Presentacion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PresentacionesCompletasEquivalentes).HasColumnType("decimal(38, 0)");
            entity.Property(e => e.UnidadPresentacion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwProyectosResuman>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ProyectosResumen");

            entity.Property(e => e.Cliente)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CodigoProyecto)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CostoEstimado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostoReal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EstadoColor)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.EstadoEntrega)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.MargenPorcentual).HasColumnType("decimal(38, 15)");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Prioridad)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Responsable)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.TipoProyecto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UtilidadEstimada).HasColumnType("decimal(19, 2)");
        });

        modelBuilder.Entity<VwResumenInventarioPorCategorium>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ResumenInventarioPorCategoria");

            entity.Property(e => e.Categoria)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.StockTotal).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.StockTotalFormateado).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VwResumenMaterialesPorProyecto>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ResumenMaterialesPorProyecto");

            entity.Property(e => e.CodigoProyecto)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ProyectoDescripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TotalDanado).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TotalDevuelto).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TotalEntregado).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TotalPendiente).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TotalUtilizado).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<VwSesionesActiva>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SesionesActivas");

            entity.Property(e => e.Dispositivo).HasMaxLength(100);
            entity.Property(e => e.EstadoSesion)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasColumnType("datetime");
            entity.Property(e => e.FechaExpiracion).HasColumnType("datetime");
            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .HasColumnName("IP");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NombreRol)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.Property(e => e.UserAgent).HasMaxLength(300);
        });

        modelBuilder.Entity<VwStockActual>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StockActual");

            entity.Property(e => e.CantidadSueltaEnUnidadBase).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.Categoria)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EstadoStock)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.FactorConversionVigente).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PresentacionPredeterminada)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PresentacionesCompletas).HasColumnType("decimal(38, 0)");
            entity.Property(e => e.StockActual).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockDisponible).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.StockMaximo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockMinimo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockReservado).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnidadConsumo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnidadPresentacionPredeterminada)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwUltimosMovimientosKardex>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_UltimosMovimientosKardex");

            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Movimiento)
                .HasMaxLength(31)
                .IsUnicode(false);
            entity.Property(e => e.Observacion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.StockAnterior).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StockPosterior).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Usuario)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
