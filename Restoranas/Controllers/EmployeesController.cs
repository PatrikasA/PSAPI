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
                    string query = "SELECT naudotojo_id, prisijungimo_vardas, naudotojo_tipas_id FROM naudotojas WHERE naudotojo_tipas_id = 2";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int id = reader.GetInt32(0); // naudotojo_id
                                string vardas = reader.GetString(1); // prisijungimo_vardas
                                int role = reader.GetInt32(2); // naudotojo_tipas_id
                                darbuotojai.Add(new User { naudotojo_id = id, prisijungimo_vardas = vardas, naudotojo_tipas_id = role });
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



        [HttpGet]
        public IActionResult AddEmployeeForm()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEmployeeForm(User userModel)
        {
            userModel.naudotojo_tipas_id = 2; 
            if (ModelState.IsValid)
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    try
                    {
                        conn.Open();
                        var query = "INSERT INTO naudotojas (prisijungimo_vardas, slaptazodis, naudotojo_tipas_id) VALUES (@prisijungimoVardas, @slaptazodis, @naudotojoTipasId)";
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@prisijungimoVardas", userModel.prisijungimo_vardas);
                            cmd.Parameters.AddWithValue("@slaptazodis", userModel.slaptazodis);
                            cmd.Parameters.AddWithValue("@naudotojoTipasId", userModel.naudotojo_tipas_id);

                            var result = cmd.ExecuteNonQuery();
                            if (result > 0)
                            {
                                return RedirectToAction("EmployeesPage");
                            }
                            else
                            {
                                ModelState.AddModelError("", "Registration failed due to a database error.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                    }
                }
            }

            return View(userModel);
        }

        [HttpGet]
        public IActionResult RemoveEmployeeForm(int id)
        {
            User user = null;
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = "SELECT naudotojo_id, prisijungimo_vardas FROM naudotojas WHERE naudotojo_id = @id";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                naudotojo_id = reader.GetInt32(0),
                                prisijungimo_vardas = reader.GetString(1)
                            };
                        }
                    }
                }
            }
            if (user == null)
                return RedirectToAction("EmployeesPage");

            return View(user);
        }

        [HttpPost, ActionName("ConfirmRemoveEmployee")]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmRemoveEmployee(int naudotojo_id)
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = "DELETE FROM naudotojas WHERE naudotojo_id = @naudotojo_id";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@naudotojo_id", naudotojo_id);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("EmployeesPage");
        }





    }
}
