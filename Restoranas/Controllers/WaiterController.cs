using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using Restoranas.Models;

namespace Restoranas.Controllers
{
    public class WaiterController : Controller
    {
        private readonly string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";


        [HttpGet]
        public IActionResult MainViewWaiter()
        {
            return View("MainViewWaiter");
        }


        [HttpGet]
        public IActionResult VisitsPageWaiter()
        {
            var visits = new List<Visit>();
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = "SELECT * FROM apsilankymas WHERE uzbaigtas = false OR apmoketas = false";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            visits.Add(new Visit
                            {
                                apsilankymo_id = reader.GetInt32(reader.GetOrdinal("apsilankymo_id")),
                                data = reader.GetDateTime(reader.GetOrdinal("data")),
                                zmoniu_skaicius = reader.GetInt32(reader.GetOrdinal("zmoniu_skaicius")),
                                apmoketas = reader.GetBoolean(reader.GetOrdinal("apmoketas")),
                                naudotojo_id = reader.IsDBNull(reader.GetOrdinal("naudotojo_id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("naudotojo_id")),
                                staliuko_nr = reader.GetInt32(reader.GetOrdinal("staliuko_nr")),
                                uzbaigtas = reader.GetBoolean(reader.GetOrdinal("uzbaigtas"))
                            });
                        }
                    }
                }
            }
            return View(visits);
        }

        [HttpGet]
        public IActionResult CancelVisitConfirmation(int id)
        {
            ViewBag.VisitId = id;
            return View();
        }

        [HttpGet]
        public IActionResult VisitDetails(int id)
        {
            ViewBag.VisitId = id;
            return View();
        }

        [HttpGet]
        public IActionResult EnterClientOrder(int id)
        {
            ViewBag.VisitId = id;
            return View();
        }

        [HttpGet]
        public IActionResult ModifyClientOrder(int id)
        {
            ViewBag.VisitId = id;
            return View();
        }


        [HttpGet]
        public IActionResult ClientOrderCheque(int id)
        {
            List<OrderItem> orderItems = new List<OrderItem>();
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = @"
                    SELECT up.kiekis, p.pavadinimas, p.kaina
                    FROM uzsakytas_patiekalas up
                    JOIN patiekalas p ON p.patiekalo_id = up.patiekalo_id
                    WHERE up.apsilankymo_id = @VisitId AND p.parduodamas = true";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VisitId", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orderItems.Add(new OrderItem
                            {
                                Name = reader.GetString(reader.GetOrdinal("pavadinimas")),
                                Count = reader.GetInt32(reader.GetOrdinal("kiekis")),
                                Price = reader.GetDouble(reader.GetOrdinal("kaina"))
                            });
                        }
                    }
                }
            }

            double totalPrice = orderItems.Sum(item => item.Price * item.Count);

            ViewBag.TotalPrice = totalPrice;
            ViewBag.VisitId = id;
            return View(orderItems);
        }

        [HttpPost]
        public IActionResult SubmitCheque(int id)
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = "UPDATE apsilankymas SET uzbaigtas = true WHERE apsilankymo_id = @VisitId";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VisitId", id);
                    int affectedRows = cmd.ExecuteNonQuery();
                    //gal kazkada error message idet
                }
            }

            return RedirectToAction("VisitDetails", new { id = id });
        }

        [HttpGet]
        public IActionResult CreateVisit()
        {
            List<SelectListItem> tableNumbers = new List<SelectListItem>();
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = "SELECT staliuko_nr FROM staliukas ORDER BY staliuko_nr";

                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tableNumbers.Add(new SelectListItem
                        {
                            Value = reader["staliuko_nr"].ToString(),
                            Text = reader["staliuko_nr"].ToString()
                        });
                    }
                }
            }

            ViewBag.TableNumbers = tableNumbers;

            var newVisit = new Visit
            {
                data = DateTime.Now,
                apmoketas = false,
                uzbaigtas = false,
                zmoniu_skaicius = 1
            };

            return View(newVisit);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateVisit(Visit visit)
        {
            if (ModelState.IsValid)
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    var query = @"
                INSERT INTO apsilankymas (data, zmoniu_skaicius, apmoketas, naudotojo_id, staliuko_nr, uzbaigtas)
                VALUES (@Data, @ZmoniuSkaicius, @Apmoketas, @NaudotojoId, @StaliukoNr, @Uzbaigtas)";
                    try
                    {
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Data", visit.data);
                            cmd.Parameters.AddWithValue("@ZmoniuSkaicius", visit.zmoniu_skaicius);
                            cmd.Parameters.AddWithValue("@Apmoketas", visit.apmoketas);
                            cmd.Parameters.AddWithValue("@NaudotojoId", visit.naudotojo_id ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@StaliukoNr", visit.staliuko_nr);
                            cmd.Parameters.AddWithValue("@Uzbaigtas", visit.uzbaigtas);

                            cmd.ExecuteNonQuery();
                        }
                        return RedirectToAction("VisitsPageWaiter");
                    }
                    catch (NpgsqlException ex)
                    {
                        ModelState.AddModelError("", "Nerastas naudotojas su tokiu ID.");
                    }
                }
            }
            return View(visit);
        }

    }
}
