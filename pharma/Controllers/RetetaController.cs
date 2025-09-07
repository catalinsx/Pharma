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

        public async Task<IActionResult> Index(
    int? medId,
    string? search,
    DateTime? dateFrom,
    DateTime? dateTo,
    string? visitFilter,
    string sortBy = "data-desc",
    int page = 1,
    int pageSize = 20)
        {
            try
            {
                // Validare parametri
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var retete = _pharmaContext.Retete
                    .Include(r => r.pacient)
                    .Include(r => r.medicament)
                    .AsQueryable();

                // Filtrare după medicament
                if (medId.HasValue && medId.Value > 0)
                {
                    retete = retete.Where(r => r.MedicamentId == medId.Value);
                    try
                    {
                        ViewBag.MedicamentNume = await _pharmaContext.Medicamente
                            .Where(m => m.Id == medId.Value)
                            .Select(m => m.Nume)
                            .FirstOrDefaultAsync();
                    }
                    catch (Exception)
                    {
                        ViewBag.MedicamentNume = null;
                    }
                }

                // Căutare text cu protecție împotriva null
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.Trim().ToLower();
                    retete = retete.Where(r =>
                        (r.pacient != null && (r.pacient.Nume + " " + r.pacient.Prenume).ToLower().Contains(searchTerm)) ||
                        (r.medicament != null && r.medicament.Nume.ToLower().Contains(searchTerm)) ||
                        (r.Serie != null && r.Serie.ToLower().Contains(searchTerm)) ||
                        (r.NrReteta != null && r.NrReteta.ToLower().Contains(searchTerm)));
                }

                // Filtrare după dată cu validare
                if (dateFrom.HasValue)
                {
                    if (dateFrom.Value > DateTime.Now.Date.AddYears(1))
                        dateFrom = DateTime.Now.Date;
                    retete = retete.Where(r => r.Data.Date >= dateFrom.Value.Date);
                }

                if (dateTo.HasValue)
                {
                    if (dateTo.Value < new DateTime(2000, 1, 1))
                        dateTo = DateTime.Now.Date;
                    retete = retete.Where(r => r.Data.Date <= dateTo.Value.Date);
                }

                // Validare că dateFrom <= dateTo
                if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
                {
                    var temp = dateFrom;
                    dateFrom = dateTo;
                    dateTo = temp;
                }

                // Filtrare vizite cu calcule corecte
                if (!string.IsNullOrWhiteSpace(visitFilter))
                {
                    var today = DateTime.Today;
                    var weekEnd = today.AddDays(7 - (int)today.DayOfWeek); // Până duminica
                    var monthEnd = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                    var nextMonthStart = monthEnd.AddDays(1);
                    var nextMonthEnd = nextMonthStart.AddMonths(1).AddDays(-1);

                    retete = visitFilter.ToLower() switch
                    {
                        "cu-vizite" => retete.Where(r => r.DataUrmatoareiVizite.HasValue),
                        "fara-vizite" => retete.Where(r => !r.DataUrmatoareiVizite.HasValue),
                        "saptamana" => retete.Where(r => r.DataUrmatoareiVizite.HasValue &&
                                                        r.DataUrmatoareiVizite.Value.Date >= today &&
                                                        r.DataUrmatoareiVizite.Value.Date <= weekEnd),
                        "luna" => retete.Where(r => r.DataUrmatoareiVizite.HasValue &&
                                                   r.DataUrmatoareiVizite.Value.Date >= today &&
                                                   r.DataUrmatoareiVizite.Value.Date <= monthEnd),
                        "luna-viitoare" => retete.Where(r => r.DataUrmatoareiVizite.HasValue &&
                                                            r.DataUrmatoareiVizite.Value.Date >= nextMonthStart &&
                                                            r.DataUrmatoareiVizite.Value.Date <= nextMonthEnd),
                        _ => retete
                    };
                }

                // Sortare cu protecție împotriva null
                retete = sortBy?.ToLower() switch
                {
                    "data-asc" => retete.OrderBy(r => r.Data),
                    "pacient-asc" => retete.OrderBy(r => r.pacient != null ? r.pacient.Nume : "").ThenBy(r => r.pacient != null ? r.pacient.Prenume : ""),
                    "pacient-desc" => retete.OrderByDescending(r => r.pacient != null ? r.pacient.Nume : "").ThenByDescending(r => r.pacient != null ? r.pacient.Prenume : ""),
                    "vizita-asc" => retete.OrderBy(r => r.DataUrmatoareiVizite ?? DateTime.MaxValue),
                    "vizita-desc" => retete.OrderByDescending(r => r.DataUrmatoareiVizite ?? DateTime.MinValue),
                    _ => retete.OrderByDescending(r => r.Data) // default: data-desc
                };

                // Paginare cu protecție împotriva erorilor
                var totalItems = 0;
                try
                {
                    totalItems = await retete.CountAsync();
                }
                catch (Exception)
                {
                    totalItems = 0;
                }

                var totalPages = totalItems > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 1;

                // Ajustare pagină dacă este prea mare
                if (page > totalPages && totalPages > 0)
                    page = totalPages;

                List<Reteta> rezultate;
                try
                {
                    rezultate = await retete
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                }
                catch (Exception)
                {
                    rezultate = new List<Reteta>();
                }

                // Informații pentru View
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                ViewBag.PageSize = pageSize;
                ViewBag.MedId = medId;

                ViewBag.Search = search;
                ViewBag.DateFrom = dateFrom?.ToString("dd.MM.yyyy");
                ViewBag.DateTo = dateTo?.ToString("dd.MM.yyyy");
                ViewBag.VisitFilter = visitFilter;
                ViewBag.SortBy = sortBy;

                return View(rezultate);
            }
            catch (Exception ex)
            {
                // Log error if logging is available
                TempData["Error"] = "A apărut o eroare la încărcarea rețetelor. Vă rugăm să încercați din nou.";
                return View(new List<Reteta>());
            }
        }

        [HttpGet]
        public IActionResult Create(int medId)
        {
            try
            {
                if (medId <= 0)
                {
                    TempData["Error"] = "ID medicament invalid!";
                    return RedirectToAction("Index", "Medicament");
                }

                var medicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == medId);
                if (medicament == null)
                {
                    TempData["Error"] = "Medicamentul nu a fost găsit!";
                    return RedirectToAction("Index", "Medicament");
                }

                // Get only patients that are linked to this specific medicine
                var pacientiLegati = _pharmaContext.PacientMedicamente
                    .Where(pm => pm.MedicamentId == medId)
                    .Include(pm => pm.Pacient)
                    .Select(pm => pm.Pacient)
                    .Where(p => p != null)
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
                    Data = DateTime.Today // Doar data, fără ora
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "A apărut o eroare la încărcarea formularului. Vă rugăm să încercați din nou.";
                return RedirectToAction("Index", "Medicament");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RetetaCreateViewModel model)
        {
            try
            {
                // Validări suplimentare
                if (model.MedicamentId <= 0)
                {
                    ModelState.AddModelError("", "ID medicament invalid!");
                }

                if (model.PacientId <= 0)
                {
                    ModelState.AddModelError("PacientId", "Vă rugăm să selectați un pacient!");
                }

                // Validare dată
                if (model.Data > DateTime.Now.Date.AddDays(1))
                {
                    ModelState.AddModelError("Data", "Data rețetei nu poate fi în viitor!");
                }

                if (model.Data < new DateTime(2000, 1, 1))
                {
                    ModelState.AddModelError("Data", "Data rețetei nu poate fi mai veche de anul 2000!");
                }

                // Validare data următoarei vizite
                if (model.DataUrmatoareiVizite.HasValue)
                {
                    if (model.DataUrmatoareiVizite.Value.Date < DateTime.Now.Date)
                    {
                        ModelState.AddModelError("DataUrmatoareiVizite", "Data următoarei vizite nu poate fi în trecut!");
                    }

                    if (model.DataUrmatoareiVizite.Value > DateTime.Now.AddYears(5))
                    {
                        ModelState.AddModelError("DataUrmatoareiVizite", "Data următoarei vizite este prea îndepărtată!");
                    }
                }

                // Verificare dacă medicamentul și pacientul există
                var medicamentExista = _pharmaContext.Medicamente.Any(m => m.Id == model.MedicamentId);
                var pacientExista = _pharmaContext.Pacienti.Any(p => p.Id == model.PacientId);
                var asociereExista = _pharmaContext.PacientMedicamente
                    .Any(pm => pm.MedicamentId == model.MedicamentId && pm.PacientId == model.PacientId);

                if (!medicamentExista)
                {
                    ModelState.AddModelError("", "Medicamentul selectat nu există!");
                }

                if (!pacientExista)
                {
                    ModelState.AddModelError("PacientId", "Pacientul selectat nu există!");
                }

                if (!asociereExista && medicamentExista && pacientExista)
                {
                    ModelState.AddModelError("PacientId", "Pacientul selectat nu este asociat cu acest medicament!");
                }

                if (ModelState.IsValid)
                {
                    var reteta = new Reteta
                    {
                        Data = model.Data.Date, // Asigurăm că salvăm doar data, fără ora
                        Serie = string.IsNullOrWhiteSpace(model.Serie) ? null : model.Serie.Trim(),
                        NrReteta = string.IsNullOrWhiteSpace(model.NrReteta) ? null : model.NrReteta.Trim(),
                        Observatii = string.IsNullOrWhiteSpace(model.Observatii) ? null : model.Observatii.Trim(),
                        PacientId = model.PacientId,
                        MedicamentId = model.MedicamentId,
                        DataUrmatoareiVizite = model.DataUrmatoareiVizite?.Date // Doar data, fără ora
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
                    .Where(p => p != null)
                    .OrderBy(p => p.Nume)
                    .ThenBy(p => p.Prenume)
                    .ToList();

                model.MedicamentNume = medicament?.Nume ?? "";
                model.PacientiDisponibili = pacientiLegati;

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "A apărut o eroare la salvarea rețetei. Vă rugăm să încercați din nou.";

                // Reload form data
                var medicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == model.MedicamentId);
                var pacientiLegati = _pharmaContext.PacientMedicamente
                    .Where(pm => pm.MedicamentId == model.MedicamentId)
                    .Include(pm => pm.Pacient)
                    .Select(pm => pm.Pacient)
                    .Where(p => p != null)
                    .OrderBy(p => p.Nume)
                    .ThenBy(p => p.Prenume)
                    .ToList();

                model.MedicamentNume = medicament?.Nume ?? "";
                model.PacientiDisponibili = pacientiLegati;

                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID rețetă invalid!";
                    return RedirectToAction("Index");
                }

                var reteta = _pharmaContext.Retete
                    .Include(r => r.pacient)
                    .Include(r => r.medicament)
                    .FirstOrDefault(r => r.Id == id);

                if (reteta == null)
                {
                    TempData["Error"] = "Rețeta nu a fost găsită!";
                    return RedirectToAction("Index");
                }

                // Get patients associated with this medicine
                var pacientiLegati = _pharmaContext.PacientMedicamente
                    .Where(pm => pm.MedicamentId == reteta.MedicamentId)
                    .Include(pm => pm.Pacient)
                    .Select(pm => pm.Pacient)
                    .Where(p => p != null)
                    .OrderBy(p => p.Nume)
                    .ThenBy(p => p.Prenume)
                    .ToList();

                var viewModel = new RetetaEditViewModel
                {
                    Id = reteta.Id,
                    Data = reteta.Data.Date, // Doar data
                    Serie = reteta.Serie,
                    NrReteta = reteta.NrReteta,
                    Observatii = reteta.Observatii,
                    PacientId = reteta.PacientId,
                    MedicamentId = reteta.MedicamentId,
                    DataUrmatoareiVizite = reteta.DataUrmatoareiVizite?.Date, // Doar data
                    MedicamentNume = reteta.medicament?.Nume ?? "",
                    PacientiDisponibili = pacientiLegati
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "A apărut o eroare la încărcarea rețetei. Vă rugăm să încercați din nou.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(RetetaEditViewModel model)
        {
            try
            {
                // Validări suplimentare similare cu Create
                if (model.Id <= 0)
                {
                    ModelState.AddModelError("", "ID rețetă invalid!");
                }

                if (model.PacientId <= 0)
                {
                    ModelState.AddModelError("PacientId", "Vă rugăm să selectați un pacient!");
                }

                // Validare dată
                if (model.Data > DateTime.Now.Date.AddDays(1))
                {
                    ModelState.AddModelError("Data", "Data rețetei nu poate fi în viitor!");
                }

                if (model.Data < new DateTime(2000, 1, 1))
                {
                    ModelState.AddModelError("Data", "Data rețetei nu poate fi mai veche de anul 2000!");
                }

                // Validare data următoarei vizite
                if (model.DataUrmatoareiVizite.HasValue)
                {
                    if (model.DataUrmatoareiVizite.Value.Date < DateTime.Now.Date)
                    {
                        ModelState.AddModelError("DataUrmatoareiVizite", "Data următoarei vizite nu poate fi în trecut!");
                    }

                    if (model.DataUrmatoareiVizite.Value > DateTime.Now.AddYears(5))
                    {
                        ModelState.AddModelError("DataUrmatoareiVizite", "Data următoarei vizite este prea îndepărtată!");
                    }
                }

                if (ModelState.IsValid)
                {
                    var reteta = _pharmaContext.Retete.FirstOrDefault(r => r.Id == model.Id);
                    if (reteta == null)
                    {
                        TempData["Error"] = "Rețeta nu a fost găsită!";
                        return RedirectToAction("Index");
                    }

                    reteta.Data = model.Data.Date; // Doar data
                    reteta.Serie = string.IsNullOrWhiteSpace(model.Serie) ? null : model.Serie.Trim();
                    reteta.NrReteta = string.IsNullOrWhiteSpace(model.NrReteta) ? null : model.NrReteta.Trim();
                    reteta.Observatii = string.IsNullOrWhiteSpace(model.Observatii) ? null : model.Observatii.Trim();
                    reteta.PacientId = model.PacientId;
                    reteta.DataUrmatoareiVizite = model.DataUrmatoareiVizite?.Date; // Doar data

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
                    .Where(p => p != null)
                    .OrderBy(p => p.Nume)
                    .ThenBy(p => p.Prenume)
                    .ToList();

                model.MedicamentNume = medicament?.Nume ?? "";
                model.PacientiDisponibili = pacientiLegati;

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "A apărut o eroare la actualizarea rețetei. Vă rugăm să încercați din nou.";

                // Reload form data
                var medicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == model.MedicamentId);
                var pacientiLegati = _pharmaContext.PacientMedicamente
                    .Where(pm => pm.MedicamentId == model.MedicamentId)
                    .Include(pm => pm.Pacient)
                    .Select(pm => pm.Pacient)
                    .Where(p => p != null)
                    .OrderBy(p => p.Nume)
                    .ThenBy(p => p.Prenume)
                    .ToList();

                model.MedicamentNume = medicament?.Nume ?? "";
                model.PacientiDisponibili = pacientiLegati;

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int? returnMedId)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID rețetă invalid!";
                    return RedirectToAction("Index");
                }

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
                TempData["Error"] = "A apărut o eroare la ștergerea rețetei. Vă rugăm să încercați din nou.";
            }

            // Return to the medicine details if we came from there, otherwise to general index
            if (returnMedId.HasValue && returnMedId.Value > 0)
                return RedirectToAction("Details", "Medicament", new { id = returnMedId.Value });

            return RedirectToAction("Index");
        }
    }
}