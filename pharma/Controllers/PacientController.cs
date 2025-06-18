using Microsoft.AspNetCore.Mvc;
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
            var pacienti = _pharmaContext.Pacienti.ToList();
            return View(pacienti);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Pacient pacient)
        {
            if (ModelState.IsValid)
            {
                _pharmaContext.Pacienti.Add(pacient);
                _pharmaContext.SaveChanges();

                return RedirectToAction("Index");
            }
           
            return View(pacient);
        }
    }
}
