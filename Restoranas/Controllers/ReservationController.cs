using Microsoft.AspNetCore.Mvc;
using Restoranas.Models;
using System.Diagnostics;

namespace Restoranas.Controllers
{
    public class ReservationController : Controller
    {
        public IActionResult Index()
        {
            // Gaukite staliukų sąrašą
            List<TableInfo> tables = GetTables();

            // Sukurkite IndexModel objektą su staliukų sąrašu
            IndexModel model = new IndexModel
            {
                Tables = tables
            };

            // Grąžinkite IndexModel į Index peržiūrą
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(IndexModel model)
        {
            // Čia galite naudoti gautą datą, pvz., ją išsaugoti į duomenų bazę ir t.t.
            // model.SelectedDate yra pasirinkta data iš kalendoriaus
            return View(model);
        }

        private List<TableInfo> GetTables()
        {
            // Jūsų esama logika gauti staliukų sąrašą
            return new List<TableInfo>
        {
            new TableInfo { TableNumber = "1", Capacity = 4 },
            new TableInfo { TableNumber = "2", Capacity = 6 },
            new TableInfo { TableNumber = "3", Capacity = 2 }
        };
        }

    }
}

