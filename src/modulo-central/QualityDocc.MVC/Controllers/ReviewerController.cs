using Microsoft.AspNetCore.Authorization; // 👈 1. Nueva librería para la seguridad
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDocc.Domain.Entities;
using QualityDocc.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QualityDocc.MVC.Controllers
{
    // 👇 2. El candado que protege todo el controlador
    [Authorize(Roles = "Reviewer")]
    public class ReviewerController : Controller
    {
        // Nota: Asegúrate de que "ApplicationDbContext" sea el nombre correcto 
        // de tu contexto. Si en AuthorController usas otro nombre, cámbialo aquí también.
        private readonly ApplicationDbContext _context;

        public ReviewerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- DASHBOARD (Las 3 tarjetas) ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Contamos los documentos según el Enum DocumentStatus
            // Asumiendo que: 1 = EnRevisión, 2 = Aprobado, 3 = Devuelto
            ViewBag.Pendientes = await _context.Document.CountAsync(d => (int)d.WorkflowState == 1);
            ViewBag.Aprobados = await _context.Document.CountAsync(d => (int)d.WorkflowState == 2);
            ViewBag.Devueltos = await _context.Document.CountAsync(d => (int)d.WorkflowState == 3);

            return View();
        }

        // --- LISTA DE PENDIENTES ---
        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            // Traemos solo los documentos en estado 1 (En Revisión)
            var docsEnRevision = await _context.Document
                .Include(d => d.Category)
                .Include(d => d.Versions)
                .Where(d => (int)d.WorkflowState == 1)
                .ToListAsync();

            return View(docsEnRevision);
        }

        // --- VISTA DETALLADA PARA APROBAR/RECHAZAR ---
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var doc = await _context.Document
                .Include(d => d.Versions)
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null)
            {
                return NotFound();
            }

            return View(doc);
        }

        // --- PROCESAMIENTO DE LA DECISIÓN ---
        [HttpPost]
        public async Task<IActionResult> ProcessReview(int id, string actionType, string notes)
        {
            var doc = await _context.Document.FindAsync(id);
            if (doc == null)
            {
                return NotFound();
            }

            if (actionType == "Aprobar")
            {
                // Cambia estado a Aprobado (2)
                doc.WorkflowState = (DocumentStatus)2;
                doc.RejectionNotes = null; // Limpiamos notas si lo aprueba
            }
            else if (actionType == "Rechazar")
            {
                // Validación: Si rechaza, DEBE escribir notas
                if (string.IsNullOrWhiteSpace(notes))
                {
                    ModelState.AddModelError("", "Debes dejar una nota explicando los cambios requeridos.");
                    // Si falla, regresamos a la misma vista con el error
                    return View("Review", doc);
                }

                // Cambia estado a Devuelto (3)
                doc.WorkflowState = (DocumentStatus)3;
                doc.RejectionNotes = notes;
            }

            await _context.SaveChangesAsync();

            // Lo regresamos al panel principal (Dashboard)
            return RedirectToAction(nameof(Index));
        }
    }
}