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
            // 1. Buscamos al usuario en la base de datos
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsDeleted == false);

            // 2. Validamos si existe y si la contraseña coincide
            if (user != null && user.PasswordHash == password)
            {
                // 3. Creamos la identidad usando el nombre completo para evitar conflictos
                // Usamos System.Security.Claims.Claim para forzar al compilador a usar la clase correcta
                var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(ClaimTypes.Name, user.Email), // Aquí usamos Email
            new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            // Si falla, enviamos el error
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}