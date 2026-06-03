using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QualityDocc.Domain.Entities; // Asegúrate que este sea el namespace correcto
using QualityDocc.Infrastructure.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QualityDoc.MVC.Controllers
{
    public class AuthorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var misDocumentos = _context.Document.ToList();
            return View(misDocumentos);
        }

        [HttpGet]
        public IActionResult Upload() => View();

        [HttpPost]
        public async Task<IActionResult> Upload(Document model, IFormFile archivo)
        {
            if (archivo != null && archivo.Length > 0)
            {
                // 1. Primero guardamos el Documento (la cabecera)
                _context.Document.Add(model);
                await _context.SaveChangesAsync(); // Esto genera el ID del documento

                // 2. Ahora creamos la versión vinculada usando el ID del documento recién creado
                var version = new DocumentVersion
                {
                    DocumentId = model.Id, // Vinculamos con el documento que acabamos de guardar
                    FileUrl = "/uploads/" + archivo.FileName, // ¡Aquí sí existe la propiedad FileUrl!
                    VersionNumber = 1, // O la lógica que uses para numerar
                                       // Asigna el resto de campos necesarios para tu versión aquí...
                };

                // 3. Guardamos la versión
                _context.DocumentVersion.Add(version);
                await _context.SaveChangesAsync();

                // 4. Guardamos el archivo físico
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", archivo.FileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult Search(string busqueda)
        {
            var documentos = _context.Document.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                // Filtra si el título contiene lo que el usuario escribió
                documentos = documentos.Where(d => d.Title.Contains(busqueda));
            }

            return View(documentos.ToList());
        }
    }
}