using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QualityDocc.Application.Interfaces;
using QualityDocc.Domain.Entities;
using QualityDocc.Infrastructure.Data;
using QualityDocc.Application.DTOs;

namespace QualityDocc.Application.Services
{
    public class DocumentService : IDocumentService
    {
        // CAMBIO 1: Usamos el contexto específico (ApplicationDbContext)
        private readonly ApplicationDbContext _context;

        public DocumentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // CAMBIO 2: Crear el contenedor "Documento" primero
        public async Task<DocumentVersion> CreateDocumentAsync(string title, string fileUrl, string extension, int userId)
        {
            // Primero creamos el documento (la "cabecera")
            var newDocument = new Document
            {
                Title = title,
                CurrentStatus = DocumentStatus.Borrador,
                AuthorId = userId
            };
            _context.Document.Add(newDocument);
            await _context.SaveChangesAsync(); // Guardar para obtener el Id

            // Ahora creamos la versión vinculada
            var initialVersion = new DocumentVersion
            {
                DocumentId = newDocument.Id, // Vinculamos con el nuevo documento
                VersionNumber = 1,
                FileUrl = fileUrl,
                Extension = extension,
                LifecycleStatus = "Draft",
                ChangeLog = "Creación inicial.",
                IdUserCreate = userId,
                DateCreate = DateTime.Now
            };

            _context.Set<DocumentVersion>().Add(initialVersion);
            await _context.SaveChangesAsync();
            return initialVersion;
        }

        public async Task<DocumentVersion> ApproveDocumentAsync(int documentId, string approvalNotes, int userId)
        {
            // CAMBIO 3: Buscar la versión actual para calcular la siguiente correctamente
            var lastVersion = await _context.Set<DocumentVersion>()
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();

            var nextVersionNumber = (lastVersion != null) ? lastVersion.VersionNumber + 1 : 1;

            var approvedVersion = new DocumentVersion
            {
                DocumentId = documentId,
                VersionNumber = nextVersionNumber,
                FileUrl = lastVersion?.FileUrl ?? "URL_default",
                Extension = lastVersion?.Extension ?? ".pdf",
                LifecycleStatus = "Approved",
                ChangeLog = "APROBADO: " + approvalNotes,
                IdUserCreate = userId,
                DateCreate = DateTime.Now
            };

            _context.Set<DocumentVersion>().Add(approvedVersion);

            // Actualizamos también el estatus del documento padre
            var doc = await _context.Document.FindAsync(documentId);
            if (doc != null) doc.CurrentStatus = DocumentStatus.Aprobado;

            await _context.SaveChangesAsync();
            return approvedVersion;
        }

        public async Task UpdateStatusAsync(int id, DocumentStatus newStatus)
        {
            var document = await _context.Document.FindAsync(id);
            if (document != null)
            {
                document.CurrentStatus = newStatus;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RejectDocumentAsync(int id, string reason)
        {
            var document = await _context.Document.FindAsync(id);
            if (document != null)
            {
                document.CurrentStatus = DocumentStatus.Rechazado;
                document.RejectionNotes = reason;
                await _context.SaveChangesAsync();
            }
        }


        public async Task<List<DocumentDto>> GetAllDocumentsAsync()
        {
            return await _context.Document
                .Include(d => d.Versions) // Asegúrate de tener la relación configurada
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    Title = d.Title,
                    // Obtenemos la última versión disponible
                    ChangeDate = d.Versions
              .OrderByDescending(v => v.VersionNumber)
              .Select(v => v.DateCreate) // Seleccionamos solo la fecha
              .FirstOrDefault() ?? DateTime.Now, // Si es null, usa la fecha actual
                })
                .ToListAsync();
        }

        // ... Mantén IncrementMinorVersionAsync igual pero asegúrate de usar _context.Documents ...
        public async Task<DocumentVersion> IncrementMinorVersionAsync(int documentId, string changeLog, int userId)
        {
            // 1. Buscamos la versión más reciente para obtener el número de versión
            var lastVersion = await _context.Set<DocumentVersion>()
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();

            // 2. Definimos el nuevo número (asumiendo que es una versión menor, ej: 0.1 -> 0.2)
            // O puedes usar lógica de decimales si tu VersionNumber es double, aquí lo manejo como int+1
            var newVersionNumber = (lastVersion != null) ? lastVersion.VersionNumber + 1 : 1;

            // 3. Creamos la nueva versión
            var newVersion = new DocumentVersion
            {
                DocumentId = documentId,
                VersionNumber = newVersionNumber,
                FileUrl = lastVersion?.FileUrl ?? "URL_default", // Mantenemos la URL o lógica que necesites
                Extension = lastVersion?.Extension ?? ".pdf",
                LifecycleStatus = "Draft",
                ChangeLog = changeLog,
                IdUserCreate = userId,
                DateCreate = DateTime.Now
            };

            _context.Set<DocumentVersion>().Add(newVersion);
            await _context.SaveChangesAsync();

            return newVersion;
        }
    }
}