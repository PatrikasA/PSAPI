using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Restoranas.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Restoranas.Helper;

namespace Restoranas.Controllers
{
    public class PatiekalasController : Controller
    {
        [HttpGet]
        public IActionResult CreateProduct()
        {
            Item naujasPatiekalas = new Item(); // Sukurkite naują Item objektą
            return View(naujasPatiekalas);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateProduct(Item productModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    StringBuilder commandTextBuilder = new StringBuilder();
                    commandTextBuilder.Append("INSERT INTO patiekalas (patiekalo_id, pavadinimas, kaina, parduodamas) ");
                    commandTextBuilder.AppendFormat("VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')",
                        productModel.patiekalo_Id, productModel.pavadinimas, productModel.kaina, productModel.parduodamas);

                    string commandText = commandTextBuilder.ToString();
                    bool success = DataSource.UpdateDataSQL(commandText);

                    if (success)
                    {
                        return View("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "PREKES NEPRIDEJO due to a database error.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
            }
            return View("Index", productModel);
        }
        public ActionResult Index()
        {
            List<Item> patiekalai = new List<Item>();

            // Connection string
            string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";

            // Establish connection
            using (var conn = new NpgsqlConnection(connString))
            {
                try
                {
                    // Open connection
                    conn.Open();

                    // Query to select all rows from the "Item" table
                    string query = "SELECT * FROM patiekalas";

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
                                int patiekaloId = reader.GetInt32(0);
                                string pavadinimas = reader.GetString(1);
                                double kaina = reader.GetDouble(2);
                                bool parduodamas = reader.GetBoolean(3);

                                // Create Item object and add it to the list
                                patiekalai.Add(new Item { patiekalo_Id = patiekaloId, pavadinimas = pavadinimas, kaina = kaina, parduodamas = parduodamas });
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
            return View(patiekalai);
        }
    }
}
