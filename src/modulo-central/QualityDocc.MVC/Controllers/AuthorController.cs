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
using Microsoft.AspNetCore.Authorization; // 👇 1. Nueva librería de seguridad

namespace QualityDocc.MVC.Controllers
{
    // 👇 2. El candado que protege el controlador completo
    [Authorize(Roles = "Author")]
    public class AuthorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AuthorController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUsername = User.Identity?.Name;

            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = _context.User.FirstOrDefault(u => u.Username == currentUsername || u.Email == currentUsername);
            if (currentUser == null)
            {
                return NotFound($"Usuario no encontrado. El sistema está buscando: '{currentUsername}'");
            }

            var totalBorradores = _context.Document
                .Count(d => d.AuthorId == currentUser.Id && d.Status == true && (int)d.WorkflowState == 0);

            var totalAprobados = _context.Document
                .Count(d => d.AuthorId == currentUser.Id && d.Status == true && (int)d.WorkflowState == 2);

            var totalDevueltos = _context.Document
                .Count(d => d.AuthorId == currentUser.Id && d.Status == true && (int)d.WorkflowState == 3);

            var ultimosArchivos = _context.Document
                .Where(d => d.AuthorId == currentUser.Id)
                .OrderByDescending(d => d.DateCreate)
                .Take(6)
                .ToList();

            var viewModel = new AuthorDashboardViewModel
            {
                TotalBorradores = totalBorradores,
                TotalAprobados = totalAprobados,
                TotalDevueltos = totalDevueltos,
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
            ViewBag.CompanyName = user.Company.Name;
            ViewBag.CompanyId = user.CompanyId;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(Document model, IFormFile archivo, string action, double versionNumber)
        {
            ViewBag.Categories = await _context.Category.ToListAsync();

            if (archivo == null || archivo.Length == 0)
            {
                ModelState.AddModelError("", "Por favor selecciona un archivo.");
                // Como recargamos la vista, necesitamos los datos de la empresa de nuevo
                var userError = await _context.User.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == model.AuthorId);
                ViewBag.CompanyName = userError?.Company?.Name;
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
                    model.Status = true;

                    // Revisamos qué botón presionó el usuario
                    if (action == "save")
                    {
                        model.WorkflowState = DocumentStatus.Borrador;
                    }
                    else
                    {
                        model.WorkflowState = DocumentStatus.Revision;
                    }

                    // 👇 3. LÓGICA DE AGREGAR VS ACTUALIZAR (Para no duplicar borradores)
                    if (model.Id == 0)
                    {
                        model.DateCreate = DateTime.Now;
                        _context.Document.Add(model);
                    }
                    else
                    {
                        // Si el ID ya existe, actualizamos el registro
                        _context.Document.Update(model);
                    }

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

                    var userEx = await _context.User.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == currentUserId);
                    ViewBag.CompanyName = userEx?.Company?.Name;

                    return View(model);
                }
            }

            // 👇 4. LA DECISIÓN DE REDIRECCIÓN
            if (action == "save")
            {
                // Volvemos a cargar la información de la empresa porque recargaremos la misma pantalla
                var user = await _context.User.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == currentUserId);
                ViewBag.CompanyName = user?.Company?.Name;
                ViewBag.CompanyId = user?.CompanyId;

                TempData["Message"] = "Borrador guardado. Puedes seguir editando y luego enviarlo.";
                return View(model);
            }

            // Si presionó Enviar
            TempData["Message"] = "Documento enviado al Autorizador correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Search(int? categoryId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int currentUserId = string.IsNullOrEmpty(userIdString) ? 0 : int.Parse(userIdString);

            ViewBag.Categories = await _context.Category.ToListAsync();

            var query = _context.Document
                                .Include(d => d.Category)
                                .AsQueryable();

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(d => d.CategoryId == categoryId);
            }

            query = query.Where(d =>
                (int)d.WorkflowState == 2 ||
                d.AuthorId == currentUserId
            );

            var documentos = await query.ToListAsync();
            return View(documentos);
        }

        [HttpPost]
        public async Task<IActionResult> SendToReview(int documentId)
        {
            var doc = await _context.Document.FindAsync(documentId);

            if (doc != null && (int)doc.WorkflowState == 0)
            {
                doc.WorkflowState = (DocumentStatus)1;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}