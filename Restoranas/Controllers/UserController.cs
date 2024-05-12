using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Restoranas.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Restoranas.Helper;

namespace Restoranas.Controllers
{
    public class UserController : Controller
    {
        private readonly string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(User userModel)
        {
            if (ModelState.IsValid)
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    try
                    {
                        conn.Open();
                        var query = "SELECT naudotojo_id, naudotojo_tipas_id FROM naudotojas WHERE prisijungimo_vardas = @prisijungimoVardas AND slaptazodis = @slaptazodis";
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@prisijungimoVardas", userModel.prisijungimo_vardas);
                            cmd.Parameters.AddWithValue("@slaptazodis", userModel.slaptazodis);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    userModel.naudotojo_id = reader.GetInt32(reader.GetOrdinal("naudotojo_id"));
                                    int userType = reader.GetInt32(reader.GetOrdinal("naudotojo_tipas_id"));

                                    HttpContext.Session.SetInt32("UserId", userModel.naudotojo_id);
                                    HttpContext.Session.SetString("UserName", userModel.prisijungimo_vardas);
                                    HttpContext.Session.SetInt32("UserType", userType);

                                    switch (userType)
                                    {
                                        case 2:  
                                            return RedirectToAction("MainViewWaiter", "Waiter");
                                        case 3:
                                            return RedirectToAction("TablesPage", "Tables");
                                        default:
                                            return RedirectToAction("Aktualiausi", "Apsilankymai");
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError("", "Invalid login attempt.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Database error: {ex.Message}");
                    }
                }
            }

            return View(userModel);
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User userModel)
        {
            if (ModelState.IsValid)
            {
                userModel.naudotojo_tipas_id = 1;
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
                                return RedirectToAction("Login");
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

            return View(); 
        }
    }
}
