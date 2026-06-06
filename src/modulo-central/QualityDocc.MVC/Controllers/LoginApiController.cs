using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDocc.Infrastructure.Data;
using QualityDocc.MVC.Models; // Asegúrate de tener tu APILoginRequest en esta carpeta
using System.Threading.Tasks;

namespace QualityDocc.MVC.Controllers
{
    // Esta ruta es la que usarán PHP y Node: "tu-dominio.com/api/login"
    [Route("api/login")]
    [ApiController]
    public class LoginApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LoginApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Authenticate([FromBody] APILoginRequest request)
        {
            // 1. Validar que no vengan vacíos
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Unauthorized(new { error = "Credenciales incorrectas" });
            }

            // 2. Buscar en la base de datos
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.PasswordHash == request.Password);

            // 3. Respuesta Fallida (401 Unauthorized)
            if (user == null)
            {
                return Unauthorized(new { error = "Credenciales incorrectas" });
            }

            // 4. Respuesta Exitosa (200 OK)
            return Ok(new
            {
                idusuario = user.Id,
                nombre = user.Username,
                rol = "Administrador"
            });
        }
    }
}