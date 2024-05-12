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

        string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";

        [HttpGet]
        public IActionResult CreateProduct()
        {
            Meal naujasPatiekalas = new Meal(); // Sukurkite naują Item objektą
            return View(naujasPatiekalas);
        }


        
        public ActionResult Index()
        {
            List<Meal> patiekalai = new List<Meal>();

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
					string query = "SELECT * FROM patiekalas WHERE parduodamas = true";

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
                                patiekalai.Add(new Meal { patiekalo_Id = patiekaloId, pavadinimas = pavadinimas, kaina = kaina, parduodamas = parduodamas });
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




        public ActionResult GetMeals(int uzsakymoId)
        {
            List<Meal> patiekalai = new List<Meal>();
            ViewBag.uzsakymoId = uzsakymoId;
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
					string query = "SELECT * FROM patiekalas WHERE parduodamas = true";

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
                                patiekalai.Add(new Meal { patiekalo_Id = patiekaloId, pavadinimas = pavadinimas, kaina = kaina, parduodamas = parduodamas });
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToVisit(int uzsakymoId, int patiekaloId, int kiekis)
        {
            try
            {
                StringBuilder commandTextBuilder = new StringBuilder();
                commandTextBuilder.Append("INSERT INTO uzsakytas_patiekalas (kiekis, patiekalo_id, apsilankymo_id) ");
                commandTextBuilder.AppendFormat("VALUES ({0}, {1}, {2})",
                            kiekis, patiekaloId, uzsakymoId);

                string commandText = commandTextBuilder.ToString();
                bool success = DataSource.UpdateDataSQL(commandText);

                if (success)
                {
                    TempData["SuccessMessage"] = "Patiekalas sėkmingai pridėtas prie užsakymo.";
                    return RedirectToAction("GetMeals", "Patiekalas", new { uzsakymoId = uzsakymoId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Patiekalo pridėjimas nepavyko , nes jis jau pridėtas.";
                    return RedirectToAction("GetMeals", "Patiekalas", new { uzsakymoId = uzsakymoId });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Įvyko klaida pridedant patiekalą prie apsilankymo: {ex.Message}");
            }
        }


        // Manager

        public ActionResult MealsPage()
        {
            List<Meal> patiekalai = new List<Meal>();

            // Connection string

            // Establish connection
            using (var conn = new NpgsqlConnection(connString))
            {
                try
                {
                    // Open connection
                    conn.Open();

					// Query to select all rows from the "Item" table
					string query = "SELECT * FROM patiekalas ORDER BY parduodamas DESC, patiekalo_id";

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
                                patiekalai.Add(new Meal { patiekalo_Id = patiekaloId, pavadinimas = pavadinimas, kaina = kaina, parduodamas = parduodamas });
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
            return View("~/Views/Manager/MealsPage.cshtml", patiekalai);
        }
        // Create

        [HttpGet]
        public IActionResult OpenMealCreation()
        {
            Meal naujasPatiekalas = new Meal(); // Sukurkite naują Item objektą
            return View("~/Views/Manager/MealCreateForm.cshtml", naujasPatiekalas);
        }

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateMeal(Meal mealModel)
		{
			if (ModelState.IsValid)
			{
				try
				{
					using (var conn = new NpgsqlConnection(connString))
					{
						await conn.OpenAsync();

						string checkQuery = "SELECT COUNT(*) FROM patiekalas WHERE pavadinimas = @pavadinimas";
						using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
						{
							checkCmd.Parameters.AddWithValue("@pavadinimas", mealModel.pavadinimas);
							long count = (long)await checkCmd.ExecuteScalarAsync();
							if (count > 0)
							{
								TempData["ErrorMessage"] = "Patiekalas su tokiu pavadinimu jau egzistuoja.";
								return View("~/Views/Manager/MealCreateForm.cshtml", mealModel);
							}
						}

						string query = "INSERT INTO patiekalas (pavadinimas, kaina, parduodamas) VALUES (@pavadinimas, @kaina, @parduodamas)";
						using (var cmd = new NpgsqlCommand(query, conn))
						{
							cmd.Parameters.AddWithValue("@pavadinimas", mealModel.pavadinimas);
							cmd.Parameters.AddWithValue("@kaina", mealModel.kaina);
							cmd.Parameters.AddWithValue("@parduodamas", mealModel.parduodamas);
							await cmd.ExecuteNonQueryAsync();
						}
					}

					return RedirectToAction("MealsPage");
				}
				catch (Exception ex)
				{
					TempData["ErrorMessage"] = $"An error occurred during meal creation: {ex.Message}";
				}
			}
			else
			{
				TempData["ErrorMessage"] = "Please correct the errors and try again.";
			}

			return View("~/Views/Manager/MealCreateForm.cshtml", mealModel);
		}

		// Delete

		// TABLE DELETE

		public async Task<IActionResult> OpenMealDelete(int id)
        {
            Meal itemToDelete = null;

            // Connect to the database and retrieve the item with the specified ID
            using (var conn = new NpgsqlConnection(connString))
            {
                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM patiekalas WHERE patiekalo_id = @id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int patiekaloId = reader.GetInt32(0);
                                string pavadinimas = reader.GetString(1);
                                double kaina = reader.GetDouble(2);
                                bool parduodamas = reader.GetBoolean(3);

                                itemToDelete = new Meal { patiekalo_Id = patiekaloId, pavadinimas = pavadinimas, kaina = kaina, parduodamas = parduodamas };
                            }
                            else
                            {
								// Item with the specified ID not found
								TempData["ErrorMessage"] = "Item not found.";
                            }
                        }
                    }

                    conn.Close();
                }
                catch (Exception ex)
                {
					TempData["ErrorMessage"]= $"Failed to connect to PostgreSQL: {ex.Message}";
                }
            }

            // Pass the item object to the view
            return View("~/Views/Manager/MealDeleteForm.cshtml", itemToDelete);
        }

        public async Task<IActionResult> DeleteMeal(int id)
        {
            // Check if the meal is ordered in future visits
            string checkQuery = @"
                SELECT COUNT(*)
                FROM uzsakytas_patiekalas
                WHERE patiekalo_id = @mealId
                AND apsilankymo_id IN (
                    SELECT apsilankymo_id
                    FROM apsilankymas
                    WHERE data >= CURRENT_DATE
                )";

            long orderCount;

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@mealId", id);
                    orderCount = (long)await cmd.ExecuteScalarAsync();
                }
            }

            if (orderCount > 0)
            {
                TempData["ErrorMessage"] = "Negalima ištrinti šio patiekalo, kadangi jis yra šiandienos ar ateities užsakyme.";
                return RedirectToAction("OpenMealDelete", new { id });
            }

            // If the meal is not ordered in future visits, proceed with deleting the meal
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    await conn.OpenAsync();

                    string deleteQuery = "DELETE FROM patiekalas WHERE patiekalo_id = @mealId";

                    using (var cmd = new NpgsqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@mealId", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return RedirectToAction("MealsPage");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the meal: {ex.Message}";
                return RedirectToAction("OpenMealDelete", new { id });
            }
        }

		// Edit

		public IActionResult OpenMealEdit(int id)
		{
			var mealInfo = GetMealInfoById(id);

			if (mealInfo == null)
			{
				return NotFound();
			}

			//TempData["ErrorMessage"] = $"pradzioj: {mealInfo.pavadinimas}, {mealInfo.parduodamas}, {mealInfo.patiekalo_Id}";

			return View("~/Views/Manager/MealEditForm.cshtml", mealInfo);
		}

		private Meal GetMealInfoById(int id)
		{
			using (var connection = new NpgsqlConnection(connString))
			{
				connection.Open();

				string query = "SELECT * FROM patiekalas WHERE patiekalo_id = @Id";

				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@Id", id);

					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							var mealInfo = new Meal
							{
								patiekalo_Id = reader.GetInt32(reader.GetOrdinal("patiekalo_id")),
								pavadinimas = reader.GetString(reader.GetOrdinal("pavadinimas")),
								kaina = reader.GetDouble(reader.GetOrdinal("kaina")),
								parduodamas = reader.GetBoolean(reader.GetOrdinal("parduodamas"))
							};
							return mealInfo;
						}
					}
				}
			}

			return null;
		}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeal(Meal mealModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the original meal information from the database
                    var originalMeal = GetMealInfoById(mealModel.patiekalo_Id);

                    if (originalMeal == null)
                    {
                        return NotFound();
                    }

                    // Check if kaina or pavadinimas were changed
                    bool kainaChanged = originalMeal.kaina != mealModel.kaina;
                    bool pavadinimasChanged = originalMeal.pavadinimas != mealModel.pavadinimas;

                    // Check if the meal is ordered in future visits
                    string checkQuery = @"
                    SELECT COUNT(*)
                    FROM uzsakytas_patiekalas
                    WHERE patiekalo_id = @mealId
                    AND apsilankymo_id IN (
                        SELECT apsilankymo_id
                        FROM apsilankymas
                        WHERE data >= CURRENT_DATE
                    )";

                    long orderCount;

                    using (var conn = new NpgsqlConnection(connString))
                    {
                        await conn.OpenAsync();

                        using (var cmd = new NpgsqlCommand(checkQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@mealId", mealModel.patiekalo_Id);
                            orderCount = (long)await cmd.ExecuteScalarAsync();
                        }
                    }

                    if (orderCount > 0 && (kainaChanged || pavadinimasChanged))
                    {
                        TempData["ErrorMessage"] = "Negalima atnaujinti šio patiekalo pavadinimo arba kainos, kadangi patiekalas yra šiandienos ar ateities užsakyme.";
                        return View("~/Views/Manager/MealEditForm.cshtml", mealModel);
                    }

                    // Update meal information in the database
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        await conn.OpenAsync();
                        string updateQuery = "UPDATE patiekalas SET pavadinimas = @name, kaina = @price, parduodamas = @isAvailable WHERE patiekalo_id = @mealId";

                        using (var cmd = new NpgsqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@name", mealModel.pavadinimas);
                            cmd.Parameters.AddWithValue("@price", mealModel.kaina);
                            cmd.Parameters.AddWithValue("@isAvailable", mealModel.parduodamas);
                            cmd.Parameters.AddWithValue("@mealId", mealModel.patiekalo_Id);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Redirect to the MealsPage view
                    return RedirectToAction("MealsPage");
                }
                catch (Exception ex)
                {
                    // Handle any errors that occur during the update process
                    TempData["ErrorMessage"] = $"An error occurred while updating the meal: {ex.Message}";
                }
            }

            // If model state is not valid, return to the edit form with the model
            return View("~/Views/Manager/MealEditForm.cshtml", mealModel);
        }

    }
}

