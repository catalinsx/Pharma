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

        public IActionResult Index(int? medId, string? search, DateTime? dateFrom, DateTime? dateTo,
                          string? visitFilter, string sortBy = "data-desc", int page = 1, int pageSize = 20)
        {
            var retete = _pharmaContext.Retete
                .Include(r => r.pacient)
                .Include(r => r.medicament)
                .AsQueryable();

            // Filtrare după medicament
            if (medId.HasValue)
            {
                retete = retete.Where(r => r.MedicamentId == medId.Value);
                ViewBag.MedicamentNume = _pharmaContext.Medicamente
                    .FirstOrDefault(m => m.Id == medId.Value)?.Nume;
            }

            // Căutare text
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.ToLower();
                retete = retete.Where(r =>
                    (r.pacient!.Nume + " " + r.pacient.Prenume).ToLower().Contains(searchTerm) ||
                    (r.medicament!.Nume).ToLower().Contains(searchTerm) ||
                    (r.Serie != null && r.Serie.ToLower().Contains(searchTerm)) ||
                    (r.NrReteta != null && r.NrReteta.ToLower().Contains(searchTerm)));
            }

            // Filtrare după dată
            if (dateFrom.HasValue)
            {
                retete = retete.Where(r => r.Data >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                retete = retete.Where(r => r.Data <= dateTo.Value);
            }

            // Filtrare vizite
            if (!string.IsNullOrWhiteSpace(visitFilter))
            {
                var today = DateTime.Today;
                var weekEnd = today.AddDays(7);
                var monthEnd = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                var nextMonthStart = today.AddMonths(1).AddDays(1 - today.Day);
                var nextMonthEnd = nextMonthStart.AddMonths(1).AddDays(-1);

                switch (visitFilter)
                {
                    case "cu-vizite":
                        retete = retete.Where(r => r.DataUrmatoareiVizite.HasValue);
                        break;
                    case "fara-vizite":
                        retete = retete.Where(r => !r.DataUrmatoareiVizite.HasValue);
                        break;
                    case "saptamana":
                        retete = retete.Where(r => r.DataUrmatoareiVizite.HasValue &&
                                                 r.DataUrmatoareiVizite.Value >= today &&
                                                 r.DataUrmatoareiVizite.Value <= weekEnd);
                        break;
                    case "luna":
                        retete = retete.Where(r => r.DataUrmatoareiVizite.HasValue &&
                                                 r.DataUrmatoareiVizite.Value >= today &&
                                                 r.DataUrmatoareiVizite.Value <= monthEnd);
                        break;
                    case "luna-viitoare":
                        retete = retete.Where(r => r.DataUrmatoareiVizite.HasValue &&
                                                 r.DataUrmatoareiVizite.Value >= nextMonthStart &&
                                                 r.DataUrmatoareiVizite.Value <= nextMonthEnd);
                        break;
                }
            }

            // Sortare
            switch (sortBy)
            {
                case "data-asc":
                    retete = retete.OrderBy(r => r.Data);
                    break;
                case "pacient-asc":
                    retete = retete.OrderBy(r => r.pacient!.Nume).ThenBy(r => r.pacient!.Prenume);
                    break;
                case "pacient-desc":
                    retete = retete.OrderByDescending(r => r.pacient!.Nume).ThenByDescending(r => r.pacient!.Prenume);
                    break;
                case "vizita-asc":
                    retete = retete.OrderBy(r => r.DataUrmatoareiVizite ?? DateTime.MaxValue);
                    break;
                case "vizita-desc":
                    retete = retete.OrderByDescending(r => r.DataUrmatoareiVizite ?? DateTime.MinValue);
                    break;
                default: // "data-desc"
                    retete = retete.OrderByDescending(r => r.Data);
                    break;
            }

            // Paginare
            var totalItems = retete.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var pagedItems = retete.Skip((page - 1) * pageSize).Take(pageSize);

            // Convertire la listă
            var rezultate = pagedItems.ToList();

            // Adaugă informații de paginare la ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            // Păstrează parametrii de filtrare pentru paginare
            ViewBag.Search = search;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.VisitFilter = visitFilter;
            ViewBag.SortBy = sortBy;

            return View(rezultate);
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
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int? returnMedId)
        {
            try
            {
                var reteta = _pharmaContext.Retete.FirstOrDefault(r => r.Id == id);
                if (reteta == null)
                {
                    TempData["Error"] = "Rețeta nu a fost găsită!";
                    return RedirectToAction("Index");
                }

                _pharmaContext.Retete.Remove(reteta);
                _pharmaContext.SaveChanges();

                TempData["Success"] = "Rețeta a fost ștearsă cu succes!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "A apărut o eroare la ștergerea rețetei!";
                // Log error if you have logging set up
            }

            // Return to the medicine details if we came from there, otherwise to general index
            if (returnMedId.HasValue)
                return RedirectToAction("Details", "Medicament", new { id = returnMedId.Value });

            return RedirectToAction("Index");
        }
    }
}