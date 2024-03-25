using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Restoranas.Models;
using System.Diagnostics;

namespace Restoranas.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult CreateTable()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTable(TableInfo tableModel)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";

                    // Establish connection
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        try
                        {
                            // Open connection
                            conn.Open();
                            // Query to insert new rows into the "staliukas" table
                            string query = "INSERT INTO staliukas (staliuko_nr, vietos) VALUES (@staliukoNr, @vietos)";

                            // Create a command to execute the query
                            using (var cmd = new NpgsqlCommand(query, conn))
                            {
                                // Užpildome parametrus
                                cmd.Parameters.AddWithValue("@staliukoNr", tableModel.TableNumber);
                                cmd.Parameters.AddWithValue("@vietos", tableModel.Capacity);

                                // Execute the query
                                cmd.ExecuteNonQuery();
                            }

                            conn.Close();
                        }
                        catch (Exception ex)
                        {
                            // Prisijungimo klaida
                            ViewBag.ErrorMessage = $"Failed to connect to PostgreSQL: {ex.Message}";
                        }
                    }

                    return RedirectToAction(nameof(Index)); // Peradresuojame į Index veiksmą
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred during table creation: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
            }
            return View(tableModel); // Grąžiname peržiūros langą su įvestais duomenimis
        }

        public async Task<IActionResult> EditTable(int id)
        {
            TableInfo staliukas = new TableInfo();
            string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";

            // Establish connection
            using (var conn = new NpgsqlConnection(connString))
            {
                try
                {
                    // Open connection
                    conn.Open();

                    // Query to select all rows from the "Patiekalas" table
                    string query = "SELECT * FROM staliukas WHERE staliuko_nr=@stalnr";

                    // Create a command to execute the query
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        // Execute the query and obtain a reader
                        using (var reader = cmd.ExecuteReader())
                        {
                            cmd.Parameters.AddWithValue("@stalnr", id);

                            // Check if there are rows in the result set
                            while (reader.Read())
                            {
                                // Read values from the current row
                                int staliuko_nr = reader.GetInt32(0);
                                int vietos = reader.GetInt32(1);

                                // Create Patiekalas object and add it to the list
                                staliukas = (new TableInfo { TableNumber = staliuko_nr, Capacity = vietos });
                            }
                        }
                    }

                    // Close connection
                    conn.Close();
                }
                catch (Exception ex)
                {
                    // Prisijungimo klaida
                    ViewBag.ErrorMessage = $"Failed to connect to PostgreSQL: {ex.Message}";
                }
            }

            if (staliukas == null)
            {
                return NotFound();
            }

            return View(staliukas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTable(int id, [Bind("TableNumber,SeatCount")] TableInfo table)
        {
           /* if (id != table.TableId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(table);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TableExists(table.TableId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(GetTables));
            }*/
            return View(table);
        }

        public async Task<IActionResult> GetTables()
        {
            List<TableInfo> staliukai = new List<TableInfo>();

            // Connection string
            string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";

            // Establish connection
            using (var conn = new NpgsqlConnection(connString))
            {
                try
                {
                    // Open connection
                    conn.Open();

                    // Query to select all rows from the "Patiekalas" table
                    string query = "SELECT * FROM staliukas";

                    // Create a command to execute the query
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        // Execute the query and obtain a reader
                        using (var reader = cmd.ExecuteReader())
                        {
                            // Check if there are rows in the result set
                            while (reader.Read())
                            {
                                // Read values from the current row
                                int staliuko_nr = reader.GetInt32(0);
                                int vietos = reader.GetInt32(1);

                                // Create Patiekalas object and add it to the list
                                staliukai.Add(new TableInfo { TableNumber = staliuko_nr, Capacity = vietos});
                            }
                        }
                    }

                    // Close connection
                    conn.Close();
                }
                catch (Exception ex)
                {
                    // Prisijungimo klaida
                    ViewBag.ErrorMessage = $"Failed to connect to PostgreSQL: {ex.Message}";
                }
            }

            // Grąžinamas peržiūros langas su patiekalų sąrašu
            return View(staliukai);
        }
    }
}