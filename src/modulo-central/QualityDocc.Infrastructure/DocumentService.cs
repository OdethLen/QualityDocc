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
    var newDocument = new Document
    {
        Title = title,
        WorkflowState = DocumentStatus.Borrador, // Usamos esto, no LifecycleStatus
        AuthorId = userId
    };
    _context.Document.Add(newDocument);
    await _context.SaveChangesAsync();

    var initialVersion = new DocumentVersion
    {
        DocumentId = newDocument.Id,
        VersionNumber = 1,
        FileUrl = fileUrl,
        Extension = extension,
        // ELIMINADA LA LÍNEA LifecycleStatus AQUÍ
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
                // ELIMINADA LA LÍNEA LifecycleStatus AQUÍ
                ChangeLog = "APROBADO: " + approvalNotes,
                IdUserCreate = userId,
                DateCreate = DateTime.Now
            };

            _context.Set<DocumentVersion>().Add(approvedVersion);

            var doc = await _context.Document.FindAsync(documentId);
            if (doc != null) doc.WorkflowState = DocumentStatus.Aprobado; // Aquí gestionas el estado

            await _context.SaveChangesAsync();
            return approvedVersion;
        }

        public async Task UpdateStatusAsync(int id, DocumentStatus newStatus)
        {
            var document = await _context.Document.FindAsync(id);
            if (document != null)
            {
                // 1. Usa la propiedad real de tu modelo (ej: WorkflowState)
                // 2. Asigna 'newStatus' directamente, sin .ToString()
                document.WorkflowState = newStatus;

                await _context.SaveChangesAsync();
            }
        }
        public async Task RejectDocumentAsync(int id, string reason)
        {
            var document = await _context.Document.FindAsync(id);
            if (document != null)
            {
                document.WorkflowState = DocumentStatus.Rechazado;
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
            var lastVersion = await _context.Set<DocumentVersion>()
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();

            var newVersionNumber = (lastVersion != null) ? lastVersion.VersionNumber + 1 : 1;

            var newVersion = new DocumentVersion
            {
                DocumentId = documentId,
                VersionNumber = newVersionNumber,
                FileUrl = lastVersion?.FileUrl ?? "URL_default",
                Extension = lastVersion?.Extension ?? ".pdf",
                // ELIMINADA LA LÍNEA LifecycleStatus AQUÍ
                ChangeLog = changeLog,
                IdUserCreate = userId,
                DateCreate = DateTime.Now
            };

            _context.Set<DocumentVersion>().Add(newVersion);
            await _context.SaveChangesAsync();

            return newVersion;
        }


        public double GetNextVersionNumber(int documentId)
        {
            var lastVersion = _context.DocumentVersion
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            if (lastVersion == null) return 0.1;

            // Incrementa y redondea a 1 decimal
            return Math.Round(lastVersion.VersionNumber + 0.1, 1);
        }
    }
}