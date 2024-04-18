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
        public IActionResult ModifyClientOrder(int id)
        {
            ViewBag.VisitId = id;
            var orderedItems = GetOrderedItemsWithIds(id);
            return View(orderedItems);
        }

        public List<OrderItem> GetOrderedItemsWithIds(int visitId)
        {
            var orderedItems = new List<OrderItem>();
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = @"
            SELECT up.uzsakymo_id, p.pavadinimas, up.kiekis, p.kaina
            FROM uzsakytas_patiekalas up
            JOIN patiekalas p ON p.patiekalo_id = up.patiekalo_id
            WHERE up.apsilankymo_id = @VisitId AND p.parduodamas = true";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VisitId", visitId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orderedItems.Add(new OrderItem
                            {
                                OrderId = reader.GetInt32(reader.GetOrdinal("uzsakymo_id")),
                                Name = reader.GetString(reader.GetOrdinal("pavadinimas")),
                                Count = reader.GetInt32(reader.GetOrdinal("kiekis")),
                                Price = reader.GetDouble(reader.GetOrdinal("kaina"))
                            });
                        }
                    }
                }
            }
            return orderedItems;
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

        [HttpGet]
        public IActionResult EnterClientOrder(int id)
        {
            ViewBag.VisitId = id;
            ViewBag.MenuItems = GetMenuItems();
            ViewBag.OrderedItems = GetOrderedItems(id);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnterClientOrder(int id, int patiekaloId, int quantity)
        {
            if (quantity > 0)
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    var query = @"
                        INSERT INTO uzsakytas_patiekalas (kiekis, patiekalo_id, apsilankymo_id)
                        VALUES (@Quantity, @PatiekaloId, @ApsilankymoId)";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@PatiekaloId", patiekaloId);
                        cmd.Parameters.AddWithValue("@ApsilankymoId", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return RedirectToAction("EnterClientOrder", new { id = id });
        }

        public List<SelectListItem> GetMenuItems()
        {
            var items = new List<SelectListItem>();
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = "SELECT patiekalo_id, pavadinimas, kaina FROM patiekalas WHERE parduodamas = true";
                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new Item
                        {
                            patiekalo_Id = reader.GetInt32(reader.GetOrdinal("patiekalo_id")),
                            pavadinimas = reader.GetString(reader.GetOrdinal("pavadinimas")),
                            kaina = reader.GetDouble(reader.GetOrdinal("kaina")),
                            parduodamas = true
                        };
                        items.Add(new SelectListItem
                        {
                            Value = item.patiekalo_Id.ToString(),
                            Text = $"{item.pavadinimas} - ${item.kaina:F2}"
                        });
                    }
                }
            }
            return items;
        }

        public List<OrderItem> GetOrderedItems(int visitId)
        {
            var orderedItems = new List<OrderItem>();
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = @"
            SELECT p.pavadinimas, up.kiekis, p.kaina
            FROM uzsakytas_patiekalas up
            JOIN patiekalas p ON p.patiekalo_id = up.patiekalo_id
            WHERE up.apsilankymo_id = @VisitId AND p.parduodamas = true";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VisitId", visitId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orderedItems.Add(new OrderItem
                            {
                                Name = reader.GetString(reader.GetOrdinal("pavadinimas")),
                                Count = reader.GetInt32(reader.GetOrdinal("kiekis")),
                                Price = reader.GetDouble(reader.GetOrdinal("kaina"))
                            });
                        }
                    }
                }
            }
            return orderedItems;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderItem(int id, int uzsakymoId, int newQuantity)
        {
            if (newQuantity > 0)
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    var query = @"UPDATE uzsakytas_patiekalas SET kiekis = @Quantity
                          WHERE uzsakymo_id = @UzsakymoId AND apsilankymo_id = @ApsilankymoId";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Quantity", newQuantity);
                        cmd.Parameters.AddWithValue("@UzsakymoId", uzsakymoId);
                        cmd.Parameters.AddWithValue("@ApsilankymoId", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                return RemoveOrderItem(id, uzsakymoId);
            }
            return RedirectToAction("ModifyClientOrder", new { id = id });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveOrderItem(int id, int uzsakymoId)
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = "DELETE FROM uzsakytas_patiekalas WHERE uzsakymo_id = @UzsakymoId";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UzsakymoId", uzsakymoId);
                    int result = cmd.ExecuteNonQuery();
                    if (result == 0)
                    {
                        TempData["Error"] = "Įvyko klaida";
                    }
                }
            }
            return RedirectToAction("ModifyClientOrder", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelVisit(int id)
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var query = @"
            UPDATE apsilankymas 
            SET apmoketas = true, uzbaigtas = true
            WHERE apsilankymo_id = @VisitId";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VisitId", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("VisitsPageWaiter");
        }



    }
}
