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

        public IActionResult Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Convertimos a int (asegúrate de que tu Id sea int)
            int currentUserId = int.Parse(userIdString);

            // FILTRO CRÍTICO: .Where(d => d.AuthorId == currentUserId)
            // Esto asegura que solo traiga lo que le pertenece al usuario actual
            var misDocumentos = _context.Document
                                        .Where(d => d.AuthorId == currentUserId)
                                        .ToList();

            return View(misDocumentos);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            ViewBag.Categories = _context.Category.ToList();
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

            // --- CORRECCIÓN 1: Captura automática de extensión ---
            string extension = Path.GetExtension(archivo.FileName).ToLower();

            // --- CORRECCIÓN 2: Obtener usuario real ---
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int currentUserId = int.Parse(userIdString);

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            // --- TRUCO: Guardamos la ruta en una variable para mostrarla si algo falla ---
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // ... (Tu lógica de guardado de Documento sigue igual) ...
                    model.AuthorId = currentUserId;
                    // ... (resto de tu lógica) ...

                    // --- CORRECCIÓN 3: Guardar Extension e IdUserCreate ---
                    var version = new DocumentVersion
                    {
                        DocumentId = model.Id,
                        VersionNumber = versionNumber,
                        FileUrl = "/uploads/" + uniqueFileName,
                        Extension = extension,           // <--- AHORA SE GUARDA
                        IdUserCreate = currentUserId,    // <--- AHORA SE GUARDA
                        DateCreate = DateTime.Now,
                        ChangeLog = Request.Form["ChangeLog"]
                    };
                    _context.DocumentVersion.Add(version);
                    await _context.SaveChangesAsync();

                    // Guardado físico
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

                    // ESTO NOS DIRÁ EL ERROR REAL DE LA BASE DE DATOS
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