using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace QualityDocc.MVC.Controllers
{
    public class AuthController : Controller
    {
        // Acción para mostrar la pantalla de Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Acción que recibe los datos al presionar "Iniciar Sesión"
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Aquí puedes conectar tu lógica con la base de datos de Docker más adelante.
            // Por ahora, validamos un acceso de prueba para comprobar el flujo:
            if (username == "admin" && password == "1234")
            {
                // Creamos la identidad del usuario (gafete de autenticación)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, "1") // Mapea el ID de usuario que requiere tu HomeController
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Iniciamos la sesión mediante cookies en el navegador
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // 🎯 ¡AQUÍ ESTÁ EL PUENTE DE REDIRECCIÓN!
                // Le decimos al sistema que viaje al método Index del HomeController
                return RedirectToAction("Index", "Home");
            }

            // Si falla, regresamos a la vista con un mensaje de error
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        // Acción para cerrar sesión
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}