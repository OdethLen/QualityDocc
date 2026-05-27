using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Si sale línea roja, abajo te digo el comando para solucionarlo
using QualityDocc.Application.Interfaces;
using QualityDocc.Domain.Entities;

namespace QualityDocc.Application.Services
{
    public class DocumentService : IDocumentService
    {
        // Usamos la abstracción genérica para no depender de Infrastructure
        private readonly DbContext _context;

        public DocumentService(DbContext context)
        {
            _context = context;
        }

        public async Task<DocumentVersion> CreateDocumentAsync(string title, string fileUrl, string extension, int userId)
        {
            var initialVersion = new DocumentVersion
            {
                VersionNumber = 1,
                FileUrl = fileUrl,
                Extension = extension,
                LifecycleStatus = "Draft",
                ChangeLog = "Creación inicial del borrador del documento.",
                IdUserCreate = userId,
                DateCreate = DateTime.Now,
                IsDeleted = false
            };

            _context.Set<DocumentVersion>().Add(initialVersion);
            await _context.SaveChangesAsync();
            return initialVersion;
        }

        public async Task<DocumentVersion> IncrementMinorVersionAsync(int documentId, string changeLog, int userId)
        {
            var lastVersion = await _context.Set<DocumentVersion>()
                .Where(v => v.DocumentId == documentId && !v.IsDeleted)
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();

            if (lastVersion == null)
                throw new Exception("No se encontró el documento especificado.");

            if (lastVersion.LifecycleStatus == "Approved")
                throw new Exception("No se pueden incrementar borradores en un documento ya aprobado.");

            if (string.IsNullOrWhiteSpace(changeLog))
                throw new ArgumentException("Es obligatorio añadir una nota explicando el cambio del borrador.");

            var nextVersion = new DocumentVersion
            {
                DocumentId = documentId,
                VersionNumber = lastVersion.VersionNumber + 1,
                FileUrl = lastVersion.FileUrl,
                Extension = lastVersion.Extension,
                LifecycleStatus = "Draft",
                ChangeLog = changeLog,
                IdUserCreate = userId,
                DateCreate = DateTime.Now
            };

            _context.Set<DocumentVersion>().Add(nextVersion);
            await _context.SaveChangesAsync();
            return nextVersion;
        }

        public async Task<DocumentVersion> ApproveDocumentAsync(int documentId, string approvalNotes, int userId)
        {
            if (string.IsNullOrWhiteSpace(approvalNotes))
            {
                throw new ArgumentException("Error normativo: Las notas de aprobación son obligatorias para pasar a versión v1.0.");
            }

            var approvedVersion = new DocumentVersion
            {
                DocumentId = documentId,
                VersionNumber = 10,
                FileUrl = "URL_del_archivo_final",
                Extension = ".pdf",
                LifecycleStatus = "Approved",
                ChangeLog = "APROBADO: " + approvalNotes,
                IdUserCreate = userId,
                DateCreate = DateTime.Now
            };

            _context.Set<DocumentVersion>().Add(approvedVersion);
            await _context.SaveChangesAsync();
            return approvedVersion;
        }
    }
}