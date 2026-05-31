using Microsoft.AspNetCore.Mvc;
using QualityDocc.Application.Interfaces;
using QualityDocc.Domain.Entities;
using QualityDocc.MVC.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QualityDocc.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDocumentService _documentService;

        // Inyectamos el servicio core de control de versiones
        public HomeController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        // Acción del Panel Principal de Control
        public IActionResult Index()
        {
            // Mantenemos tu lista simulada para que tu vista no se rompa al cargar
            var documentos = new List<DocumentDto>
            {
                new DocumentDto
                {
                    Id = 42,
                    Title = "Manual de Procedimientos de Auditoría Interna",
                    VersionNumber = "0.1",
// CORRECTO: Esto llama al Enum definido en tu clase
                    CurrentStatus = DocumentStatus.Borrador,                    ChangeDate = DateTime.Now.AddMinutes(-10),
                    CreatedBy = "Admin"
                },
                new DocumentDto
                {
                    Id = 15,
                    Title = "Política de Seguridad y Control Ambiental ANSI",
                    VersionNumber = "1.0",
// CORRECTO: Esto llama al Enum definido en tu clase
                    CurrentStatus = DocumentStatus.Aprobado,                    ChangeDate = DateTime.Now.AddDays(-3),
                    CreatedBy = "Auditor Jefe"
                }
            };

            return View(documentos);
        }

        // ==========================================
        // ACCIÓN PARA EL BOTÓN v++ (Subir Borrador Minor)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> IncrementVersion(int documentId, string changeLog)
        {
            try
            {
                // ID de usuario simulado (reemplazar por el ID real del usuario logueado después)
                int mockUserId = 1;

                // Ejecutamos la matemática del backend que creaste en el servicio
                var newVersion = await _documentService.IncrementMinorVersionAsync(documentId, changeLog, mockUserId);

                // Mensaje temporal de éxito para mostrar en la interfaz
                TempData["SuccessMessage"] = $"¡Borrador incrementado con éxito! Se registró la versión interna {newVersion.VersionNumber}.";
            }
            catch (Exception ex)
            {
                // Captura si el log venía vacío o si ya estaba aprobado
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // ACCIÓN PARA EL BOTÓN APROBAR (Fijar en v1.0)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ApproveDocument(int documentId, string approvalNotes)
        {
            try
            {
                int mockUserId = 1;

                // Ejecutamos la lógica de aprobación normativa con notas obligatorias
                var approvedVersion = await _documentService.ApproveDocumentAsync(documentId, approvalNotes, mockUserId);

                TempData["SuccessMessage"] = $"¡Documento aprobado normativamente! Estado fijado en: {approvedVersion.LifecycleStatus}.";
            }
            catch (Exception ex)
            {
                // Captura si las notas obligatorias venían vacías
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}