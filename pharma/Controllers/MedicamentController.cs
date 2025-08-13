using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharma.Data;
using pharma.Models;

namespace pharma.Controllers
{
    public class MedicamentController : Controller
    {
        private readonly PharmaContext _pharmaContext;

        public MedicamentController(PharmaContext pharmaContext)
        {
            _pharmaContext = pharmaContext;
        }

        public IActionResult Index(string? search)
        {
            var medicamente = _pharmaContext.Medicamente
                .Include(m => m.PacientMedicamente)
                .ThenInclude(pm => pm.Pacient)
                .Include(m => m.Retete)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                medicamente = medicamente.Where(m => m.Nume.Contains(search));
            }

            var rezultate = medicamente.OrderBy(m => m.Nume).ToList();
            return View(rezultate);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Medicament medicament)
        {
            if (ModelState.IsValid)
            {
                // Check if medicine name already exists
                if (_pharmaContext.Medicamente.Any(m => m.Nume.ToLower() == medicament.Nume.ToLower()))
                {
                    ModelState.AddModelError("Nume", "Există already un medicament cu acest nume!");
                    return View(medicament);
                }

                _pharmaContext.Medicamente.Add(medicament);
                _pharmaContext.SaveChanges();

                TempData["Success"] = $"Medicamentul {medicament.Nume} a fost adăugat cu succes!";
                return RedirectToAction("Index");
            }
            return View(medicament);
        }

        public IActionResult Details(int id)
        {
            var medicament = _pharmaContext.Medicamente
                .Include(m => m.PacientMedicamente)
                .ThenInclude(pm => pm.Pacient)
                .Include(m => m.Retete)
                .ThenInclude(r => r.pacient)
                .FirstOrDefault(m => m.Id == id);

            if (medicament == null)
                return NotFound();

            return View(medicament);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var medicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == id);
            if (medicament == null)
                return NotFound();

            return View(medicament);
        }

        [HttpPost]
        public IActionResult Edit(Medicament medicament)
        {
            if (ModelState.IsValid)
            {
                var existingMedicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == medicament.Id);
                if (existingMedicament == null)
                    return NotFound();

                // Check if the new name conflicts with another medicine (excluding current one)
                if (_pharmaContext.Medicamente.Any(m => m.Id != medicament.Id && m.Nume.ToLower() == medicament.Nume.ToLower()))
                {
                    ModelState.AddModelError("Nume", "Există deja un medicament cu acest nume!");
                    return View(medicament);
                }

                existingMedicament.Nume = medicament.Nume;
                _pharmaContext.SaveChanges();

                TempData["Success"] = $"Medicamentul {medicament.Nume} a fost actualizat cu succes!";
                return RedirectToAction("Index");
            }

            return View(medicament);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var medicament = _pharmaContext.Medicamente
                .Include(m => m.PacientMedicamente)
                .Include(m => m.Retete)
                .FirstOrDefault(m => m.Id == id);

            if (medicament == null)
                return NotFound();

            // Check for dependencies
            if (medicament.Retete.Any())
            {
                TempData["Error"] = $"Nu poți șterge medicamentul {medicament.Nume} deoarece are {medicament.Retete.Count} rețete asociate!";
                return RedirectToAction("Index");
            }

            if (medicament.PacientMedicamente.Any())
            {
                TempData["Warning"] = $"Medicamentul {medicament.Nume} este asociat cu {medicament.PacientMedicamente.Count} pacienți. Asocierile vor fi eliminate.";
            }

            // Store name for success message
            var medicamentNume = medicament.Nume;
            var numarPacienti = medicament.PacientMedicamente.Count;

            // Remove all associations first
            _pharmaContext.PacientMedicamente.RemoveRange(medicament.PacientMedicamente);
            _pharmaContext.Medicamente.Remove(medicament);

            _pharmaContext.SaveChanges();

            TempData["Success"] = $"Medicamentul {medicamentNume} a fost șters cu succes! ({numarPacienti} asocieri eliminate)";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ManagePatients(int id)
        {
            var medicament = _pharmaContext.Medicamente
                .Include(m => m.PacientMedicamente)
                .ThenInclude(pm => pm.Pacient)
                .FirstOrDefault(m => m.Id == id);

            if (medicament == null)
                return NotFound();

            var viewModel = new MedicamentPatientsViewModel
            {
                MedicamentId = medicament.Id,
                MedicamentNume = medicament.Nume,
                AssociatedPatients = medicament.PacientMedicamente.Select(pm => pm.Pacient).ToList(),
                AvailablePatients = _pharmaContext.Pacienti
                    .Where(p => !p.PacientMedicamente.Any(pm => pm.MedicamentId == id))
                    .OrderBy(p => p.Nume)
                    .ThenBy(p => p.Prenume)
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AddPatientAssociation(int medicamentId, int pacientId)
        {
            // Check if association already exists
            if (_pharmaContext.PacientMedicamente.Any(pm => pm.MedicamentId == medicamentId && pm.PacientId == pacientId))
            {
                TempData["Warning"] = "Asocierea există deja!";
                return RedirectToAction("ManagePatients", new { id = medicamentId });
            }

            var association = new PacientMedicament
            {
                MedicamentId = medicamentId,
                PacientId = pacientId
            };

            _pharmaContext.PacientMedicamente.Add(association);
            _pharmaContext.SaveChanges();

            var pacient = _pharmaContext.Pacienti.Find(pacientId);
            TempData["Success"] = $"Pacientul {pacient?.Nume} {pacient?.Prenume} a fost asociat cu succes!";

            return RedirectToAction("ManagePatients", new { id = medicamentId });
        }

        [HttpPost]
        public IActionResult RemovePatientAssociation(int medicamentId, int pacientId)
        {
            var association = _pharmaContext.PacientMedicamente
                .FirstOrDefault(pm => pm.MedicamentId == medicamentId && pm.PacientId == pacientId);

            if (association == null)
                return NotFound();

            // Check if there are prescriptions for this combination
            var hasRetete = _pharmaContext.Retete
                .Any(r => r.MedicamentId == medicamentId && r.PacientId == pacientId);

            if (hasRetete)
            {
                TempData["Error"] = "Nu poți elimina această asociere deoarece există rețete pentru această combinație de pacient-medicament!";
                return RedirectToAction("ManagePatients", new { id = medicamentId });
            }

            _pharmaContext.PacientMedicamente.Remove(association);
            _pharmaContext.SaveChanges();

            var pacient = _pharmaContext.Pacienti.Find(pacientId);
            TempData["Success"] = $"Asocierea cu pacientul {pacient?.Nume} {pacient?.Prenume} a fost eliminată!";

            return RedirectToAction("ManagePatients", new { id = medicamentId });
        }
    }
}