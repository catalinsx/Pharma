using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharma.Data;
using pharma.Models;

namespace pharma.Controllers
{
    public class RetetaController : Controller
    {
        private readonly PharmaContext _pharmaContext;

        public RetetaController(PharmaContext pharmaContext)
        {
            _pharmaContext = pharmaContext;
        }

        public IActionResult Index(int? medId)
        {
            var retete = _pharmaContext.Retete
                .Include(r => r.pacient)
                .Include(r => r.medicament)
                .AsQueryable();

            if (medId.HasValue)
            {
                retete = retete.Where(r => r.MedicamentId == medId.Value);
                ViewBag.MedicamentNume = _pharmaContext.Medicamente
                    .FirstOrDefault(m => m.Id == medId.Value)?.Nume;
            }

            return View(retete.OrderByDescending(r => r.Data).ToList());
        }

        [HttpGet]
        public IActionResult Create(int medId)
        {
            var medicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == medId);
            if (medicament == null)
                return NotFound();

            // Get only patients that are linked to this specific medicine
            var pacientiLegati = _pharmaContext.PacientMedicamente
                .Where(pm => pm.MedicamentId == medId)
                .Include(pm => pm.Pacient)
                .Select(pm => pm.Pacient)
                .OrderBy(p => p.Nume)
                .ThenBy(p => p.Prenume)
                .ToList();

            if (!pacientiLegati.Any())
            {
                TempData["Error"] = $"Nu există pacienți asociați cu medicamentul {medicament.Nume}. Vă rugăm să adăugați mai întâi pacienți pentru acest medicament.";
                return RedirectToAction("Details", "Medicament", new { id = medId });
            }

            var viewModel = new RetetaCreateViewModel
            {
                MedicamentId = medId,
                MedicamentNume = medicament.Nume,
                PacientiDisponibili = pacientiLegati,
                Data = DateTime.Now
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Create(RetetaCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var reteta = new Reteta
                {
                    Data = model.Data,
                    Serie = model.Serie,
                    NrReteta = model.NrReteta,
                    Observatii = model.Observatii,
                    PacientId = model.PacientId,
                    MedicamentId = model.MedicamentId,
                    DataUrmatoareiVizite = model.DataUrmatoareiVizite
                };

                _pharmaContext.Retete.Add(reteta);
                _pharmaContext.SaveChanges();

                TempData["Success"] = "Rețeta a fost adăugată cu succes!";
                return RedirectToAction("Details", "Medicament", new { id = model.MedicamentId });
            }

            // If we got this far, something failed, reload the form
            var medicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == model.MedicamentId);
            var pacientiLegati = _pharmaContext.PacientMedicamente
                .Where(pm => pm.MedicamentId == model.MedicamentId)
                .Include(pm => pm.Pacient)
                .Select(pm => pm.Pacient)
                .OrderBy(p => p.Nume)
                .ThenBy(p => p.Prenume)
                .ToList();

            model.MedicamentNume = medicament?.Nume ?? "";
            model.PacientiDisponibili = pacientiLegati;

            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var reteta = _pharmaContext.Retete
                .Include(r => r.pacient)
                .Include(r => r.medicament)
                .FirstOrDefault(r => r.Id == id);

            if (reteta == null)
                return NotFound();

            // Get patients associated with this medicine
            var pacientiLegati = _pharmaContext.PacientMedicamente
                .Where(pm => pm.MedicamentId == reteta.MedicamentId)
                .Include(pm => pm.Pacient)
                .Select(pm => pm.Pacient)
                .OrderBy(p => p.Nume)
                .ThenBy(p => p.Prenume)
                .ToList();

            var viewModel = new RetetaEditViewModel
            {
                Id = reteta.Id,
                Data = reteta.Data,
                Serie = reteta.Serie,
                NrReteta = reteta.NrReteta,
                Observatii = reteta.Observatii,
                PacientId = reteta.PacientId,
                MedicamentId = reteta.MedicamentId,
                DataUrmatoareiVizite = reteta.DataUrmatoareiVizite,
                MedicamentNume = reteta.medicament?.Nume ?? "",
                PacientiDisponibili = pacientiLegati
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Edit(RetetaEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var reteta = _pharmaContext.Retete.FirstOrDefault(r => r.Id == model.Id);
                if (reteta == null)
                    return NotFound();

                reteta.Data = model.Data;
                reteta.Serie = model.Serie;
                reteta.NrReteta = model.NrReteta;
                reteta.Observatii = model.Observatii;
                reteta.PacientId = model.PacientId;
                reteta.DataUrmatoareiVizite = model.DataUrmatoareiVizite;

                _pharmaContext.SaveChanges();

                TempData["Success"] = "Rețeta a fost actualizată cu succes!";
                return RedirectToAction("Index", new { medId = model.MedicamentId });
            }

            // If we got this far, something failed, reload the form
            var medicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == model.MedicamentId);
            var pacientiLegati = _pharmaContext.PacientMedicamente
                .Where(pm => pm.MedicamentId == model.MedicamentId)
                .Include(pm => pm.Pacient)
                .Select(pm => pm.Pacient)
                .OrderBy(p => p.Nume)
                .ThenBy(p => p.Prenume)
                .ToList();

            model.MedicamentNume = medicament?.Nume ?? "";
            model.PacientiDisponibili = pacientiLegati;

            return View(model);
        }

        [HttpPost]
        public IActionResult Delete(int id, int? returnMedId)
        {
            var reteta = _pharmaContext.Retete.FirstOrDefault(r => r.Id == id);
            if (reteta == null)
                return NotFound();

            _pharmaContext.Retete.Remove(reteta);
            _pharmaContext.SaveChanges();

            TempData["Success"] = "Rețeta a fost ștearsă cu succes!";

            // Return to the medicine details if we came from there, otherwise to general index
            if (returnMedId.HasValue)
                return RedirectToAction("Details", "Medicament", new { id = returnMedId.Value });

            return RedirectToAction("Index");
        }
    }
}