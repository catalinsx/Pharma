using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharma.Data;
using pharma.Models;
using System.Globalization;

namespace pharma.Controllers
{
    public class PacientController : Controller
    {
        private PharmaContext _pharmaContext;
        public PacientController(PharmaContext pharmaContext)
        {
            _pharmaContext = pharmaContext;
        }

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
        {
            var pacienti = _pharmaContext.Pacienti
                .Include(p => p.PacientMedicamente)
                    .ThenInclude(pm => pm.Medicament)
                .Include(p => p.Retete)
                    .ThenInclude(r => r.medicament)
                .AsQueryable();

            // 🔍 Căutare
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.ToLower();
                pacienti = pacienti.Where(p =>
                    p.Nume.ToLower().Contains(searchTerm) ||
                    p.Prenume.ToLower().Contains(searchTerm) ||
                    (p.CNP != null && p.CNP.Contains(searchTerm)) ||
                    (p.NrTelefon != null && p.NrTelefon.Contains(searchTerm)));
            }

            // 📊 Număr total + paginare
            var totalItems = await pacienti.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var rezultate = await pacienti
                .OrderBy(p => p.Nume)
                .ThenBy(p => p.Prenume)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Info pentru View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;

            return View(rezultate);
        }


        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new PacientCreateViewModel
            {
                AvailableMedicamente = _pharmaContext.Medicamente.OrderBy(m => m.Nume).ToList()
            };
            return View(viewModel);
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
        }

        private bool PacientExists(string nume, string prenume, string? cnp, int excludeId = 0)
        {
            var query = _pharmaContext.Pacienti
                .Where(p => p.Id != excludeId &&
                           p.Nume.ToLower() == nume.ToLower() &&
                           p.Prenume.ToLower() == prenume.ToLower());

            // If CNP is provided, check for exact match
            if (!string.IsNullOrWhiteSpace(cnp))
            {
                return query.Any(p => p.CNP == cnp);
            }

            // If no CNP provided, check if there's already a patient with same name
            return query.Any();
        }

        [HttpPost]
        public IActionResult Create(PacientCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Normalize names
                model.Nume = CapitalizeFirstLetter(model.Nume.Trim());
                model.Prenume = CapitalizeFirstLetter(model.Prenume.Trim());

                // Check for duplicates
                if (PacientExists(model.Nume, model.Prenume, model.CNP))
                {
                    if (!string.IsNullOrWhiteSpace(model.CNP))
                    {
                        ModelState.AddModelError("CNP", "Există deja un pacient cu același nume și CNP!");
                    }
                    else
                    {
                        ModelState.AddModelError("Nume", "Există deja un pacient cu același nume și prenume. Adăugați CNP-ul pentru diferențiere.");
                    }
                    model.AvailableMedicamente = _pharmaContext.Medicamente.OrderBy(m => m.Nume).ToList();
                    return View(model);
                }

                // Create the patient
                var pacient = new Pacient
                {
                    Nume = model.Nume,
                    Prenume = model.Prenume,
                    CNP = string.IsNullOrWhiteSpace(model.CNP) ? null : model.CNP.Trim(),
                    NrTelefon = string.IsNullOrWhiteSpace(model.NrTelefon) ? null : model.NrTelefon.Trim(),
                    AlteDetalii = string.IsNullOrWhiteSpace(model.AlteDetalii) ? null : model.AlteDetalii.Trim()
                };

                _pharmaContext.Pacienti.Add(pacient);
                _pharmaContext.SaveChanges();

                // Create the relationships with selected medicines
                foreach (var medicamentId in model.SelectedMedicamentIds)
                {
                    var pacientMedicament = new PacientMedicament
                    {
                        PacientId = pacient.Id,
                        MedicamentId = medicamentId
                    };
                    _pharmaContext.PacientMedicamente.Add(pacientMedicament);
                }

                _pharmaContext.SaveChanges();

                TempData["Success"] = $"Pacientul {pacient.Nume} {pacient.Prenume} a fost adăugat cu succes!";
                return RedirectToAction("Index");
            }

            // If we got this far, something failed, redisplay form
            model.AvailableMedicamente = _pharmaContext.Medicamente.OrderBy(m => m.Nume).ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var pacient = _pharmaContext.Pacienti
                .Include(p => p.PacientMedicamente)
                .ThenInclude(pm => pm.Medicament)
                .FirstOrDefault(p => p.Id == id);

            if (pacient == null)
                return NotFound();

            var viewModel = new PacientEditViewModel
            {
                Id = pacient.Id,
                Nume = pacient.Nume,
                Prenume = pacient.Prenume,
                CNP = pacient.CNP,
                NrTelefon = pacient.NrTelefon,
                AlteDetalii = pacient.AlteDetalii,
                SelectedMedicamentIds = pacient.PacientMedicamente.Select(pm => pm.MedicamentId).ToList(),
                AvailableMedicamente = _pharmaContext.Medicamente.OrderBy(m => m.Nume).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Edit(PacientEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var pacient = _pharmaContext.Pacienti
                    .Include(p => p.PacientMedicamente)
                    .FirstOrDefault(p => p.Id == model.Id);

                if (pacient == null)
                    return NotFound();

                // Normalize names
                model.Nume = CapitalizeFirstLetter(model.Nume.Trim());
                model.Prenume = CapitalizeFirstLetter(model.Prenume.Trim());

                // Check for duplicates (excluding current patient)
                if (PacientExists(model.Nume, model.Prenume, model.CNP, model.Id))
                {
                    if (!string.IsNullOrWhiteSpace(model.CNP))
                    {
                        ModelState.AddModelError("CNP", "Există deja un alt pacient cu același nume și CNP!");
                    }
                    else
                    {
                        ModelState.AddModelError("Nume", "Există deja un alt pacient cu același nume și prenume. Adăugați CNP-ul pentru diferențiere.");
                    }
                    model.AvailableMedicamente = _pharmaContext.Medicamente.OrderBy(m => m.Nume).ToList();
                    return View(model);
                }

                // Update basic info
                pacient.Nume = model.Nume;
                pacient.Prenume = model.Prenume;
                pacient.CNP = string.IsNullOrWhiteSpace(model.CNP) ? null : model.CNP.Trim();
                pacient.NrTelefon = string.IsNullOrWhiteSpace(model.NrTelefon) ? null : model.NrTelefon.Trim();
                pacient.AlteDetalii = string.IsNullOrWhiteSpace(model.AlteDetalii) ? null : model.AlteDetalii.Trim();

                // Remove existing medicine associations
                _pharmaContext.PacientMedicamente.RemoveRange(pacient.PacientMedicamente);

                // Add new medicine associations
                foreach (var medicamentId in model.SelectedMedicamentIds)
                {
                    var pacientMedicament = new PacientMedicament
                    {
                        PacientId = pacient.Id,
                        MedicamentId = medicamentId
                    };
                    _pharmaContext.PacientMedicamente.Add(pacientMedicament);
                }

                _pharmaContext.SaveChanges();

                TempData["Success"] = $"Pacientul {pacient.Nume} {pacient.Prenume} a fost actualizat cu succes!";
                return RedirectToAction("Index");
            }

            // If we got this far, something failed, redisplay form
            model.AvailableMedicamente = _pharmaContext.Medicamente.OrderBy(m => m.Nume).ToList();
            return View(model);
        }

        // MODIFICAT: Metoda pentru modal în loc de pagină separată
        [HttpGet]
        public IActionResult GetDetails(int id)
        {
            var pacient = _pharmaContext.Pacienti
                .Include(p => p.PacientMedicamente)
                .ThenInclude(pm => pm.Medicament)
                .Include(p => p.Retete)
                .ThenInclude(r => r.medicament)
                .FirstOrDefault(p => p.Id == id);

            if (pacient == null)
                return NotFound();

            // Găsim următoarea vizită din rețete (nu din pacient direct)
            var urmatoareaVizita = pacient.Retete
                .Where(r => r.DataUrmatoareiVizite.HasValue)
                .OrderBy(r => r.DataUrmatoareiVizite.Value)
                .FirstOrDefault();

            return Json(new
            {
                id = pacient.Id,
                nume = pacient.Nume,
                prenume = pacient.Prenume,
                cnp = pacient.CNP,
                nrTelefon = pacient.NrTelefon,
                alteDetalii = pacient.AlteDetalii,
                medicamente = pacient.PacientMedicamente.Select(pm => new
                {
                    id = pm.Medicament.Id,
                    nume = pm.Medicament.Nume
                }).ToList(),
                retete = pacient.Retete.Select(r => new
                {
                    id = r.Id,
                    data = r.Data.ToString("dd.MM.yyyy"),
                    medicament = r.medicament?.Nume,
                    serie = r.Serie,
                    nrReteta = r.NrReteta,
                    dataUrmatoareiVizite = r.DataUrmatoareiVizite?.ToString("dd.MM.yyyy")
                }).OrderByDescending(r => r.data).ToList(),
                // Pentru afișarea următoarei vizite în modal
                urmatoareaVizita = urmatoareaVizita?.DataUrmatoareiVizite?.ToString("dd.MM.yyyy")
            });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var pacient = _pharmaContext.Pacienti
                .Include(p => p.PacientMedicamente)
                .Include(p => p.Retete)
                .FirstOrDefault(p => p.Id == id);

            if (pacient == null)
                return NotFound();

            // Store name for success message
            var numeComplet = $"{pacient.Nume} {pacient.Prenume}";
            var numarRetete = pacient.Retete.Count;

            // Remove all associations and prescriptions (cascade delete)
            _pharmaContext.PacientMedicamente.RemoveRange(pacient.PacientMedicamente);
            _pharmaContext.Retete.RemoveRange(pacient.Retete);
            _pharmaContext.Pacienti.Remove(pacient);

            _pharmaContext.SaveChanges();

            TempData["Success"] = $"Pacientul {numeComplet} a fost șters cu succes! ({numarRetete} rețete eliminate)";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveMedicineAssociation(int pacientId, int medicamentId)
        {
            var association = _pharmaContext.PacientMedicamente
                .FirstOrDefault(pm => pm.PacientId == pacientId && pm.MedicamentId == medicamentId);

            if (association == null)
                return NotFound();

            // Check if patient has prescriptions for this medicine
            var hasRetete = _pharmaContext.Retete
                .Any(r => r.PacientId == pacientId && r.MedicamentId == medicamentId);

            if (hasRetete)
            {
                TempData["Error"] = "Nu poți elimina asocierea cu acest medicament deoarece pacientul are rețete pentru acesta!";
                return RedirectToAction("Edit", new { id = pacientId });
            }

            _pharmaContext.PacientMedicamente.Remove(association);
            _pharmaContext.SaveChanges();

            var medicament = _pharmaContext.Medicamente.Find(medicamentId);
            TempData["Success"] = $"Asocierea cu medicamentul {medicament?.Nume} a fost eliminată!";

            return RedirectToAction("Edit", new { id = pacientId });
        }
    }
}