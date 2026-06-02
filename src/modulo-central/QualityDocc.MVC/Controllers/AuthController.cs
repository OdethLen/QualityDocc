using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; //Necesario para LINQ
using Microsoft.EntityFrameworkCore; //  Necesario para operaciones asíncronas
using QualityDocc.Infrastructure.Data; // Tu contexto de base de datos

namespace QualityDocc.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Inyección de dependencias: ASP.NET nos entrega el contexto listo
        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // 1. Buscamos al usuario e INCLUIMOS el objeto Role completo
            // Esto es vital gracias a la relación en tu User.cs
            var user = await _context.User
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Email == email);

            // 2. Validamos si existe y si la contraseña coincide
            if (user != null && user.PasswordHash == password)
            {
                // 3. Creamos la identidad incluyendo el Rol
                // 3. Creamos la identidad incluyendo el Rol
                var claims = new List<System.Security.Claims.Claim>
                    {
    // Usamos 'global::' para asegurarnos de que use la clase oficial de .NET
                    new global::System.Security.Claims.Claim(ClaimTypes.Name, user.Email),
                    new global::System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new global::System.Security.Claims.Claim(ClaimTypes.Role, user.Role.Name)
                       };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }


        [HttpGet]
        public async Task<IActionResult> TestDbConnection()
        {
            try
            {
                bool canConnect = await _context.Database.CanConnectAsync();
                return Content(canConnect ? "¡Conexión establecida!" : "Error: No se pudo conectar.");
            }
            catch (Exception ex)
            {
                return Content($"Error crítico: {ex.Message}");
            }
        }
    }



}