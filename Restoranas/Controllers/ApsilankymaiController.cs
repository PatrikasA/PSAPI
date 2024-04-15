using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Restoranas.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace Restoranas.Controllers
{
    public class ApsilankymaiController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";


        public ApsilankymaiController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Rezervuoti staliuka | Make a reservation
        public IActionResult OpenVisitCreation()
        {
            return View("VisitCreationForm");
        }
        public IActionResult CreateVisit(Visit visit)
        {
            if (!ModelState.IsValid)
            {
                return View("VisitCreationForm", visit);
            }

            // Find the optimal table combination
            List<int> optimalTables = FindOptimalTableCombination(visit);

            // If no optimal tables found
            if (optimalTables.Count == 0)
            {
                return View("VisitCreationForm");
            }

            // Create new Visit
            string insertQuery = "INSERT INTO apsilankymas (data, zmoniu_skaicius, apmoketas, naudotojo_id, staliuko_nr, uzbaigtas) " +
                                 "VALUES (@data, @peopleCount, @paid, @userId, @tableNumber, @completed)";

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                foreach (int tableNumber in optimalTables)
                {
                    using (var cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@data", visit.data);
                        cmd.Parameters.AddWithValue("@peopleCount", visit.zmoniu_skaicius);
                        cmd.Parameters.AddWithValue("@paid", false);
                        cmd.Parameters.AddWithValue("@userId", 6);
                        cmd.Parameters.AddWithValue("@tableNumber", tableNumber);
                        cmd.Parameters.AddWithValue("@completed", false);

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToAction("Aktualiausi");
        }
    

        // Returns list of tables that together can fit all of the people needed for the visit
        public List<int> FindOptimalTableCombination(Visit visit)
        {

            // Get all tables
            List<TableInfo> allTables = GetAllTables();

            // Filter tables based on availability
            List<TableInfo> availableTables = allTables
                .Where(t => IsTableAvailable(t.TableNumber, visit.data)).ToList();

            // Sort available tables by capacity
            availableTables = availableTables.OrderBy(t => t.Capacity).ToList();

            List<int> selectedTables = new List<int>();
            int goalCapacity = visit.zmoniu_skaicius;

            // Check if a single table can accommodate all people
            foreach (var table in availableTables)
            {
                if (table.Capacity >= goalCapacity)
                {
                    selectedTables.Add(table.TableNumber);
                    return selectedTables;
                }
            }

            // Check combinations of two tables
            for (int i = 0; i < availableTables.Count - 1; i++)
            {
                for (int j = i + 1; j < availableTables.Count; j++)
                {
                    if (availableTables[i].Capacity + availableTables[j].Capacity >= goalCapacity)
                    {
                        selectedTables.Add(availableTables[i].TableNumber);
                        selectedTables.Add(availableTables[j].TableNumber);
                        return selectedTables;
                    }
                }
            }

            // If no combination found, return empty list
            return selectedTables;
        }

        private List<TableInfo> GetAllTables()
        {
            List<TableInfo> allTables = new List<TableInfo>();

            // Select all tables
            string query = "SELECT staliuko_nr, vietos FROM staliukas";

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int tableNumber = reader.GetInt32(0);
                            int capacity = reader.GetInt32(1);

                            allTables.Add(new TableInfo { TableNumber = tableNumber, Capacity = capacity });
                        }
                    }
                }
            }

            return allTables;
        }

        private bool IsTableAvailable(int tableNumber, DateTime visitDateTime)
        {
            bool isAvailable = true;

            // Select future reservations for the specified table
            string query = "SELECT * FROM apsilankymas WHERE staliuko_nr = @tableNumber AND data > @currentTime";

            var futureReservations = new List<Visit>();

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@tableNumber", tableNumber);
                    cmd.Parameters.AddWithValue("@currentTime", DateTime.Now);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            futureReservations.Add(new Visit
                            {
                                apsilankymo_id = reader.GetInt32(reader.GetOrdinal("apsilankymo_id")),
                                data = reader.GetDateTime(reader.GetOrdinal("data")),
                                zmoniu_skaicius = reader.GetInt32(reader.GetOrdinal("zmoniu_skaicius")),
                                apmoketas = reader.GetBoolean(reader.GetOrdinal("apmoketas")),
                                naudotojo_id = reader.GetInt32(reader.GetOrdinal("naudotojo_id")),
                                staliuko_nr = reader.GetInt32(reader.GetOrdinal("staliuko_nr")),
                                uzbaigtas = reader.GetBoolean(reader.GetOrdinal("uzbaigtas"))
                            });

                            // Check if the requested time overlaps with any existing reservation
                            //if (visitDateTime >= futureReservations.Last().data.AddHours(-1) &&
                            //    visitDateTime <= futureReservations.Last().data.AddHours(3))
                            if (visitDateTime.Day == futureReservations.Last().data.Day)
                            {
                                // Table cannot be reserved for this time
                                isAvailable = false;
                                break;
                            }
                        }
                    }
                }
            }
            return isAvailable;
        }

        //

        public IActionResult VisitsPage()
        {
            return View("Aktualiausi");
        }

        public async Task<IActionResult> Aktualiausi()
        {
            // Get the latest future visits from the database
            List<Visit> futureVisits = await GetFutureVisitsAsync();
            ViewData["FutureVisits"] = futureVisits;

            return View();
        }

        private async Task<List<Visit>> GetFutureVisitsAsync()
        {
            List<Visit> futureVisits = new List<Visit>();

            string query = "SELECT * FROM apsilankymas WHERE data > CURRENT_DATE ORDER BY data";

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            futureVisits.Add(new Visit
                            {
                                apsilankymo_id = reader.GetInt32(reader.GetOrdinal("apsilankymo_id")),
                                data = reader.GetDateTime(reader.GetOrdinal("data")),
                                zmoniu_skaicius = reader.GetInt32(reader.GetOrdinal("zmoniu_skaicius")),
                                apmoketas = reader.GetBoolean(reader.GetOrdinal("apmoketas")),
                                naudotojo_id = reader.GetInt32(reader.GetOrdinal("naudotojo_id")),
                                staliuko_nr = reader.GetInt32(reader.GetOrdinal("staliuko_nr")),
                                uzbaigtas = reader.GetBoolean(reader.GetOrdinal("uzbaigtas"))
                            });
                        }
                    }
                }
            }

            return futureVisits;
        }

        public IActionResult Meniu()
        {
            return View();
        }
        public IActionResult Praeje()
        {
            return View();
        }

    }
}