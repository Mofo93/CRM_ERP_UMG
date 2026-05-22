using CRM_ERP_UMG.Data;
using CRM_ERP_UMG.Models;
using CRM_ERP_UMG.Services; // Se agrega el espacio de nombres de los servicios
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var constructor = WebApplication.CreateBuilder(args);

// 1. Obtener la cadena de conexión desde appsettings.json
var cadenaConexion = constructor.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión DefaultConnection.");

// 2. Configurar el contexto del sistema para usar SQL Server en lugar de PostgreSQL
constructor.Services.AddDbContext<ContextoSistema>(opciones =>
{
    opciones.UseSqlServer(cadenaConexion);
});

// 3. CONFIGURADO: Se cambió <UsuarioAplicacion, Rol> por <UsuarioAplicacion, IdentityRole>
// Esto soluciona el error del Sembrador de Datos (Seeder) al arrancar.
constructor.Services.AddIdentity<UsuarioAplicacion, IdentityRole>(opciones =>
{
    opciones.Password.RequireDigit = true;
    opciones.Password.RequireLowercase = true;
    opciones.Password.RequireUppercase = true;
    opciones.Password.RequireNonAlphanumeric = true;
    opciones.Password.RequiredLength = 6;
    opciones.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ContextoSistema>() // Se le indica el contexto del sistema
.AddDefaultTokenProviders();

// 4. Configurar las Cookies de inicio de sesión
constructor.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.LoginPath = "/Cuenta/Login";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
});

// 5. Registrar controladores con vistas (MVC)
constructor.Services.AddControllersWithViews();

// REGISTRADO AQUÍ: El contenedor ahora sabe cómo resolver ServicioFormulas antes de armar la app
constructor.Services.AddScoped<ServicioFormulas>();

// El Build debe ir estrictamente DESPUÉS de registrar todos los servicios
var aplicacion = constructor.Build();

// 6. Configurar el entorno de ejecución
if (!aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseExceptionHandler("/Cuenta/Error");
    aplicacion.UseHsts();
}

// 7. Migración automática y sembrado de datos al arrancar el proyecto
using (var alcance = aplicacion.Services.CreateScope())
{
    var servicios = alcance.ServiceProvider;
    var contexto = servicios.GetRequiredService<ContextoSistema>();
    await contexto.Database.MigrateAsync();
    await SembradorDatos.CargarDatosIniciales(servicios);
}

aplicacion.UseHttpsRedirection();
aplicacion.UseStaticFiles();
aplicacion.UseRouting();

// 8. Habilitar autenticación y autorización obligatorias para el Login
aplicacion.UseAuthentication();
aplicacion.UseAuthorization();

// 9. Configurar la ruta principal por defecto (Modulos/Index)
aplicacion.MapControllerRoute(
    name: "rutaPrincipal",
    pattern: "{controller=Modulos}/{action=Index}/{id?}");

aplicacion.Run();