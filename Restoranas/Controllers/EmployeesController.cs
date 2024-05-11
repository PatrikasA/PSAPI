using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Restoranas.Models;

namespace Restoranas.Controllers
{
    public class EmployeesController : Controller
    {
        string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";

        public IActionResult OpenEmployeesPage()
        {
            return RedirectToAction("EmployeesPage");
        }

        public async Task<IActionResult> EmployeesPage()
        {
            List<User> darbuotojai = new List<User>();

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT prisijungimo_vardas, naudotojo_tipas_id FROM naudotojas";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string vardas = reader.GetString(0);
                                int role = reader.GetInt32(1);
                                darbuotojai.Add(new User { prisijungimo_vardas = vardas, naudotojo_tipas_id = role });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Failed to connect to PostgreSQL: {ex.Message}";
            }

            return View("~/Views/Manager/EmployeesPage.cshtml", darbuotojai);
        }


    }
}
