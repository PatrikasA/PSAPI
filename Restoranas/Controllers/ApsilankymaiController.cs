using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Restoranas.Models;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Restoranas.Models;
//using Stripe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using System.Reflection;
using Restoranas.Helper;

namespace Restoranas.Controllers
{
    public class ApsilankymaiController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";
        private readonly IHttpClientFactory _httpClientFactory;

        public ApsilankymaiController(ILogger<HomeController> logger)
        {
            _logger = logger;
            // _httpClientFactory = httpClientFactory;

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
            List<(Visit, List<string>)> visits = new List<(Visit, List<string>)>();

            List<Visit> futureVisits = await GetFutureVisitsAsync();
            foreach (Visit visit in futureVisits)
            {
                List<string> meals = await GetMealsAsync(visit.apsilankymo_id);
                visits.Add((visit, meals)); ;
            }

            //ViewData["FutureVisits"] = futureVisits;
            ViewData["Visits"] = visits;
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

        private async Task<List<string>> GetMealsAsync(int id)
        {
            Dictionary<string, (int quantity, decimal totalPrice)> mealInfo = new Dictionary<string, (int, decimal)>();
            decimal totalSum = 0; // Bendra visų patiekalų suma

            string query = @"
        SELECT p.pavadinimas, up.kiekis, p.kaina
        FROM uzsakytas_patiekalas up
        INNER JOIN patiekalas p ON up.patiekalo_id = p.patiekalo_id
        WHERE up.apsilankymo_id = @apsilankymoId
    ";

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@apsilankymoId", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string mealName = reader.GetString(reader.GetOrdinal("pavadinimas"));
                            int quantity = reader.GetInt32(reader.GetOrdinal("kiekis"));
                            decimal price = (decimal)reader.GetDouble(reader.GetOrdinal("kaina"));

                            // Jei patiekalo pavadinimas jau yra žodyne, pridedame kiekį ir bendrą kainą
                            if (mealInfo.ContainsKey(mealName))
                            {
                                var (existingQuantity, existingTotalPrice) = mealInfo[mealName];
                                mealInfo[mealName] = (existingQuantity + quantity, existingTotalPrice + (quantity * price));
                            }
                            // Jei patiekalo pavadinimas dar nėra žodyne, pridedame naują įrašą
                            else
                            {
                                mealInfo[mealName] = (quantity, quantity * price);
                            }
                        }
                    }
                }
            }

            // Sukuriamas string'ų sąrašas su susumavimais
            List<string> meals = new List<string>();
            foreach (var kvp in mealInfo)
            {
                totalSum += kvp.Value.totalPrice;
                meals.Add($"{kvp.Key}: {kvp.Value.quantity}, Suma: {kvp.Value.totalPrice}");
            }

            meals.Add($"BENDRA SUMA: {totalSum}");
            return meals;
        }


        public IActionResult Meniu()
        {
            return View();
        }
        public async Task<IActionResult> Praeje()
        {
            List<Visit> visits = new List<Visit>();

            string query = "SELECT * FROM apsilankymas WHERE data < CURRENT_DATE ORDER BY data";

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            visits.Add(new Visit
                            {
                                apsilankymo_id = reader.GetInt32(reader.GetOrdinal("apsilankymo_id")),
                                data = reader.GetDateTime(reader.GetOrdinal("data")),
                                zmoniu_skaicius = reader.GetInt32(reader.GetOrdinal("zmoniu_skaicius")),
                                apmoketas = reader.GetBoolean(reader.GetOrdinal("apmoketas")),
                                naudotojo_id = 7,
                                staliuko_nr = reader.GetInt32(reader.GetOrdinal("staliuko_nr")),
                                uzbaigtas = reader.GetBoolean(reader.GetOrdinal("uzbaigtas"))
                            });
                        }
                    }
                }
            }

            return View(visits);
        }


        public async Task<IActionResult> ApmoketiUzsakyma(int apsilankymoId)
        {
            /*var apsilankymas = await GetUzsakymasFromDatabaseOrOtherSource(apsilankymoId);

            if (apsilankymas == null)
            {
                return NotFound(); // Jei užsakymas nerastas, grąžinti 404 klaidą
            }*/
            var response = true;
            if (response)
            {
                // Gaukite mokėjimo nuorodą iš atsakymo turinio ir nukreipkite vartotoją į PayPal Sandbox

                UpdateUzsakymasInDatabaseOrOtherSource(apsilankymoId);

                return RedirectToAction("Praeje", "Apsilankymai");
            }
            else
            {
                // Nepavyko sukurti mokėjimo, grąžinti klaidos pranešimą
                return BadRequest("nepavyko atlikti mokėjimo");
            }
            // PayPal Sandbox API URL ir kiti duomenys
            string payPalApiUrl = "https://api.sandbox.paypal.com";
            string clientId = "YourPayPalSandboxClientId";
            string clientSecret = "YourPayPalSandboxClientSecret";

            // Sukurkite HttpClient, naudodami IHttpClientFactory

            double suma = 15.99;
            // Sukurkite mokėjimo užklausos turinį
            var paymentRequest = new
            {
                intent = "sale",
                payer = new { payment_method = "paypal" },
                transactions = new[]
                {
                    new
                    {
                        amount = new { total = suma.ToString(), currency = "USD" },
                        description = "Apsilankymo apmokėjimas"
                    }
                },
                redirect_urls = new
                {
                    return_url = "http://yourwebsite.com/payment/success", // Nurodykite savo puslapio sėkmingo mokėjimo URL
                    cancel_url = "http://yourwebsite.com/payment/cancel"  // Nurodykite savo puslapio atšaukto mokėjimo URL
                }
            };
            var client = _httpClientFactory.CreateClient();

            // Nustatykite autorizacijos antraštę
            var byteArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            // Pateikite mokėjimo užklausą į PayPal Sandbox API
            var content = new StringContent(JsonConvert.SerializeObject(paymentRequest), Encoding.UTF8, "application/json");
            // var response = await client.PostAsync($"{payPalApiUrl}/v1/payments/payment", content);

            return NotFound();
        }



        private async Task<Visit> GetUzsakymasFromDatabaseOrOtherSource(int apsilankymoId)
        {

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();
                string query = "SELECT * FROM apsilankymas WHERE apsilankymo_id = @apsilankymo_id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@apsilankymo_id", apsilankymoId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {

                        return new Visit
                        {
                            apsilankymo_id = apsilankymoId,
                            data = reader.GetDateTime(reader.GetOrdinal("data")),
                            zmoniu_skaicius = reader.GetInt32(reader.GetOrdinal("zmoniu_skaicius")),
                            apmoketas = reader.GetBoolean(reader.GetOrdinal("apmoketas")),
                            naudotojo_id = 7,
                            staliuko_nr = reader.GetInt32(reader.GetOrdinal("staliuko_nr")),
                            uzbaigtas = reader.GetBoolean(reader.GetOrdinal("uzbaigtas"))
                        };

                    }
                }
            }

        }

        private async Task UpdateUzsakymasInDatabaseOrOtherSource(int uzsakymas_id)
        {
            string updateQuery = "UPDATE apsilankymas SET apmoketas = true WHERE apsilankymo_id = @apsilankymoId";

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@apsilankymoId", uzsakymas_id);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        [HttpPost]
        public  IActionResult Cancel(int visitId)
        {
            Console.WriteLine($"VisitId: {visitId}");

            try
            {
                StringBuilder commandTextBuilder = new StringBuilder();
                commandTextBuilder.AppendFormat("DELETE FROM uzsakytas_patiekalas WHERE apsilankymo_id = {0} ", visitId);
                StringBuilder commandTextBuilder2 = new StringBuilder();
                commandTextBuilder2.AppendFormat("DELETE FROM apsilankymas WHERE apsilankymo_id = {0} ", visitId);

                string commandText = commandTextBuilder.ToString();
                string commandText2 = commandTextBuilder2.ToString();

                bool success = DataSource.UpdateDataSQL(commandText);
                bool success2 = DataSource.UpdateDataSQL(commandText2);

                if (success && success2)
                {
                    return RedirectToAction("Aktualiausi", "Apsilankymai");
                }
                else
                {
                    return RedirectToAction("Aktualiausi", "Apsilankymai");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Įvyko klaida salinant apsilankyma: {ex.Message}");
            }
        }


    }
}