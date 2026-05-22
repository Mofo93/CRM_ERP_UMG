using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CRM_ERP_UMG.Data
{
    public static class SembradorDatos
    {
        public static async Task CargarDatosIniciales(IServiceProvider servicios)
        {
            var administradorRoles = servicios.GetRequiredService<RoleManager<IdentityRole>>();
            var administradorUsuarios = servicios.GetRequiredService<UserManager<UsuarioAplicacion>>();
            var contexto = servicios.GetRequiredService<ContextoSistema>();

            // 1. Crear Roles si no existen
            string[] roles = { "Admin", "Editor", "Viewer", "Vendedor" };
            foreach (var rol in roles)
            {
                if (!await administradorRoles.RoleExistsAsync(rol))
                {
                    await administradorRoles.CreateAsync(new IdentityRole(rol));
                }
            }

            // 2. Crear Usuario Administrador inicial
            var correoAdmin = "admin@umg.com";
            var usuarioAdmin = await administradorUsuarios.FindByEmailAsync(correoAdmin);
            if (usuarioAdmin == null)
            {
                usuarioAdmin = new UsuarioAplicacion
                {
                    UserName = correoAdmin,
                    Email = correoAdmin,
                    NombreCompleto = "Administrador CRM ERP UMG",
                    EmailConfirmed = true
                };
                await administradorUsuarios.CreateAsync(usuarioAdmin, "Admin123*");
                await administradorUsuarios.AddToRoleAsync(usuarioAdmin, "Admin");
            }

            // 3. Crear Módulo Dinámico Inicial adaptado para SQL Server (Guardando como propiedad serializada)
            if (!await contexto.ModulosDinamicos.AnyAsync())
            {
                var diccionarioInicial = new Dictionary<string, CampoDinamico>
                {
                    {
                        "cliente", new CampoDinamico
                        {
                            NombreCampo = "cliente",
                            Etiqueta = "Cliente",
                            TipoCampo = "texto",
                            Requerido = true
                        }
                    },
                    {
                        "precio_unitario", new CampoDinamico
                        {
                            NombreCampo = "precio_unitario",
                            Etiqueta = "Precio Unitario",
                            TipoCampo = "numero",
                            Requerido = true
                        }
                    },
                    {
                        "cantidad", new CampoDinamico
                        {
                            NombreCampo = "cantidad",
                            Etiqueta = "Cantidad",
                            TipoCampo = "numero",
                            Requerido = true
                        }
                    }
                };

                var moduloVentas = new ModuloDinamico
                {
                    NombreModulo = "Ventas Dinámicas",
                    Descripcion = "Módulo inicial para ventas simples con campos dinámicos.",
                    // CORREGIDO: Asignamos el diccionario a la propiedad, la cual serializará automáticamente a EsquemaCamposJson tras bambalinas
                    EsquemaCampos = diccionarioInicial
                };

                contexto.ModulosDinamicos.Add(moduloVentas);
                await contexto.SaveChangesAsync();
            }

            // 4. Crear Cliente Inicial
            if (!await contexto.Clientes.AnyAsync())
            {
                contexto.Clientes.Add(new Cliente
                {
                    Nit = "CF",
                    Nombre = "Consumidor Final",
                    Telefono = "",
                    Direccion = "Ciudad"
                });
                await contexto.SaveChangesAsync();
            }

            // 5. Crear Productos Iniciales
            if (!await contexto.Productos.AnyAsync())
            {
                contexto.Productos.AddRange(
                    new Producto
                    {
                        Codigo = "P001",
                        Nombre = "Producto de prueba 1",
                        Descripcion = "Producto inicial para pruebas",
                        PrecioVenta = 100,
                        Existencia = 50
                    },
                    new Producto
                    {
                        Codigo = "P002",
                        Nombre = "Producto de prueba 2",
                        Descripcion = "Producto inicial para pruebas",
                        PrecioVenta = 75,
                        Existencia = 30
                    }
                );
                await contexto.SaveChangesAsync();
            }
        }
    }
}