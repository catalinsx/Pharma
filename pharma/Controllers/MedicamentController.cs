using Microsoft.AspNetCore.Mvc;
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
            var medicamente = _pharmaContext.Medicamente.AsQueryable();

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
                _pharmaContext.Medicamente.Add(medicament);
                _pharmaContext.SaveChanges();

              //  return RedirectToAction("Index");

            }
            return View(medicament);
        }

        public IActionResult Details(int id)
        {
            var medicament = _pharmaContext.Medicamente.FirstOrDefault(m => m.Id == id);
            if (medicament == null)
                return NotFound();

            return View(medicament);
        }
    }
}
