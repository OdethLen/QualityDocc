using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Necesario para obtener los datos del usuario logueado
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using QualityDocc.Domain.Entities;
using QualityDocc.Infrastructure.Data;
using QualityDocc.MVC.Models.ViewModels;

namespace QualityDoc.MVC.Controllers
{
    public class AuthorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AuthorController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Ejemplo de la acción en tu AuthorController

        public async Task<IActionResult> Index()
        {
            var currentUsername = User.Identity?.Name;

            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction("Login", "Account");
            }

            // 1. CORRECCIÓN: Usar _context.User (en singular) en lugar de _context.Users
            // Modificamos esta línea para que busque por Username O por Email
            var currentUser = _context.User.FirstOrDefault(u => u.Username == currentUsername || u.Email == currentUsername);
            if (currentUser == null)
            {
                // Esto te mostrará en la pantalla blanca exactamente qué texto tiene la sesión
                return NotFound($"Usuario no encontrado. El sistema está buscando: '{currentUsername}'");
            }

            // 2. Consultas usando AuthorId, Status == true y WorkflowState
            var totalBorradores = _context.Document
                .Count(d => d.AuthorId == currentUser.Id && d.Status == true && (int)d.WorkflowState == 0);

            var totalAprobados = _context.Document
                .Count(d => d.AuthorId == currentUser.Id && d.Status == true && (int)d.WorkflowState == 2);

            // ¡NUEVO!: Consulta para contar los documentos Devueltos (Asumimos que WorkflowState == 3)
            var totalDevueltos = _context.Document
                .Count(d => d.AuthorId == currentUser.Id && d.Status == true && (int)d.WorkflowState == 3);

            // 3. CORRECCIÓN: Usar DateCreate en el OrderByDescending
            var ultimosArchivos = _context.Document
                .Where(d => d.AuthorId == currentUser.Id)
                .OrderByDescending(d => d.DateCreate)
                .Take(6)
                .ToList();

            // 4. Pasamos todos los datos, incluyendo TotalDevueltos, a la vista
            var viewModel = new AuthorDashboardViewModel
            {
                TotalBorradores = totalBorradores,
                TotalAprobados = totalAprobados,
                TotalDevueltos = totalDevueltos, // <- Agregado aquí
                UltimosBorradores = ultimosArchivos
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int currentUserId = int.Parse(userIdString);

            // Buscamos al usuario e incluimos su empresa
            var user = await _context.User.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null) return NotFound();

            // Pasamos la información a la vista
            ViewBag.Categories = await _context.Category.ToListAsync();
            ViewBag.CompanyName = user.Company.Name; // Para mostrar el nombre al usuario
            ViewBag.CompanyId = user.CompanyId;       // Para usarlo internamente

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Upload(Document model, IFormFile archivo, string action, double versionNumber)
        {
            ViewBag.Categories = await _context.Category.ToListAsync();

            if (archivo == null || archivo.Length == 0)
            {
                ModelState.AddModelError("", "Por favor selecciona un archivo.");
                return View(model);
            }

            string extension = Path.GetExtension(archivo.FileName).ToLower();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int currentUserId = int.Parse(userIdString);

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. PREPARA EL DOCUMENTO Y LLENA LOS DATOS DE AUDITORÍA
                    model.AuthorId = currentUserId;
                    model.DateCreate = DateTime.Now; // ¡Evita el error de nulos!
                    model.Status = true;             // Usando tu bit de Status activo

                    // Revisamos qué botón presionó el usuario
                    if (action == "save")
                    {
                        // Guardar Borrador
                        model.WorkflowState = DocumentStatus.Borrador; // Asegúrate de que el Enum corresponda (ej. 0)
                    }
                    else
                    {
                        // Enviar al Autorizador
                        model.WorkflowState = DocumentStatus.Revision; // Asegúrate de que el Enum corresponda (ej. 1)
                    }

                    _context.Document.Add(model);
                    await _context.SaveChangesAsync();

                    // 2. GUARDA LA VERSIÓN
                    var version = new DocumentVersion
                    {
                        DocumentId = model.Id,
                        VersionNumber = versionNumber,
                        FileUrl = "/uploads/" + uniqueFileName,
                        Extension = extension,
                        IdUserCreate = currentUserId,
                        DateCreate = DateTime.Now,
                        ChangeLog = Request.Form["ChangeLog"]
                    };

                    _context.DocumentVersion.Add(version);
                    await _context.SaveChangesAsync();

                    // 3. GUARDADO FÍSICO DEL ARCHIVO
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await archivo.CopyToAsync(stream);
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    string errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += " | Inner: " + ex.InnerException.Message;
                    }

                    ModelState.AddModelError("", "Error: " + errorMessage);
                    return View(model);
                }
            }

            TempData["Message"] = "Documento procesado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Search(int? categoryId)
        {
            // 1. Cargamos las categorías para el menú desplegable (filtro)
            ViewBag.Categories = await _context.Category.ToListAsync();

            // 2. Iniciamos la consulta base, incluyendo la tabla Category para evitar nulos
            var query = _context.Document
                                .Include(d => d.Category)
                                .AsQueryable();

            // 3. Si el usuario seleccionó una categoría, filtramos la consulta
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(d => d.CategoryId == categoryId);
            }

            // 4. Ejecutamos la consulta y enviamos la lista a la vista
            var documentos = await query.ToListAsync();
            return View(documentos);
        }

    }
}