using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharma.Data;
using pharma.Models;

namespace pharma.Controllers
{
    public class PacientController : Controller
    {
        private PharmaContext _pharmaContext;
        public PacientController(PharmaContext pharmaContext)
        {
            _pharmaContext = pharmaContext;
        }

        public IActionResult Index()
        {
            var pacienti = _pharmaContext.Pacienti
                .Include(p => p.PacientMedicamente)
                .ThenInclude(pm => pm.Medicament)
                .Include(p => p.Retete)
                .ThenInclude(r => r.medicament)
                .ToList();
            return View(pacienti);
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

        [HttpPost]
        public IActionResult Create(PacientCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create the patient
                var pacient = new Pacient
                {
                    Nume = model.Nume,
                    Prenume = model.Prenume
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

                // Update basic info
                pacient.Nume = model.Nume;
                pacient.Prenume = model.Prenume;

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

        [HttpGet]
        public IActionResult Details(int id)
        {
            var pacient = _pharmaContext.Pacienti
                .Include(p => p.PacientMedicamente)
                .ThenInclude(pm => pm.Medicament)
                .Include(p => p.Retete)
                .ThenInclude(r => r.medicament)
                .FirstOrDefault(p => p.Id == id);

            if (pacient == null)
                return NotFound();

            return View(pacient);
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