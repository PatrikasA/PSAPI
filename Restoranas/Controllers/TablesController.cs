using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Restoranas.Models;

namespace Restoranas.Controllers
{
    public class TablesController : Controller
    {
        string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";



        // TABLE CREATION
        public IActionResult OpenTableCreation()
        {
            return View("~/Views/Manager/TablesCreationForm.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTable(TableInfo tableModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        await conn.OpenAsync();

                        // Check if a table with the same number already exists
                        string checkQuery = "SELECT COUNT(*) FROM staliukas WHERE staliuko_nr = @staliukoNr";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@staliukoNr", tableModel.TableNumber);
                            long count = (long)await checkCmd.ExecuteScalarAsync();
                            if (count > 0)
                            {
                                ModelState.AddModelError("", "A table with the same number already exists.");
                                return View("~/Views/Manager/TablesCreationForm.cshtml", tableModel);
                            }
                        }

                        string query = "INSERT INTO staliukas (staliuko_nr, vietos) VALUES (@staliukoNr, @vietos)";
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@staliukoNr", tableModel.TableNumber);
                            cmd.Parameters.AddWithValue("@vietos", tableModel.Capacity);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    return RedirectToAction("TablesPage");
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

            return View("~/Views/Manager/TablesCreationForm.cshtml", tableModel);
        }
        // TABLE DELETE

        public IActionResult OpenTableDelete(int id)
        {
            return View("~/Views/Manager/TablesDeleteForm.cshtml", id);
        }

        public async Task<IActionResult> DeleteTable(int id)
        {
            // Check if there are any future reservations for the specified table
            string checkQuery = "SELECT COUNT(*) FROM apsilankymas WHERE staliuko_nr = @tableNumber AND data > NOW()";
            long reservationCount; // Change data type to long

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@tableNumber", id); // Change to use id parameter
                    reservationCount = (long)await cmd.ExecuteScalarAsync(); // Cast to long
                }
            }

            if (reservationCount > 0)
            {
                TempData["ErrorMessage"] = "Negalima ištrinti šio staliuko, kadangi jis yra naudojamas ateities rezervacijose.";
                return RedirectToAction("OpenTableDelete", new { id }); // Change to use id parameter
            }

            // If there are no future reservations, proceed with deleting the table
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    await conn.OpenAsync();

                    string deleteQuery = "DELETE FROM staliukas WHERE staliuko_nr = @tableNumber";

                    using (var cmd = new NpgsqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@tableNumber", id); // Change to use id parameter
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return RedirectToAction("TablesPage");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the table: {ex.Message}";
                return RedirectToAction("OpenTableDelete", new { id }); // Change to use id parameter
            }
        }




        // TABLE EDIT

        public IActionResult OpenTableEdit(int id)
        {
            var tableInfo = GetTableInfoById(id);

            if (tableInfo == null)
            {
                return NotFound();
            }

            return View("~/Views/Manager/TablesEditForm.cshtml", tableInfo);
        }

        private TableInfo GetTableInfoById(int id)
        {
            using (var connection = new NpgsqlConnection(connString))
            {
                connection.Open();

                // Construct the SQL query
                string query = "SELECT * FROM staliukas WHERE staliuko_nr = @Id";

                // Create NpgsqlCommand with the query and parameters
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    // Execute the query and read the result
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Map data from the reader to a TableInfo object
                            var tableInfo = new TableInfo
                            {
                                TableNumber = reader.GetInt32(reader.GetOrdinal("staliuko_nr")),
                                Capacity = reader.GetInt32(reader.GetOrdinal("vietos"))
                                // Assuming "staliuko_nr" and "vietos" are column names in your staliukas table
                            };
                            return tableInfo;
                        }
                    }
                }
            }

            return null; // Return null if no matching record is found
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTable(TableInfo tableModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if there are any future reservations for the specified table
                    string checkQuery = "SELECT COUNT(*) FROM apsilankymas WHERE staliuko_nr = @tableNumber AND data > NOW()";
                    long reservationCount; // Change data type to long

                    using (var conn = new NpgsqlConnection(connString))
                    {
                        await conn.OpenAsync();

                        using (var cmd = new NpgsqlCommand(checkQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@tableNumber", tableModel.TableNumber); // Change to use id parameter
                            reservationCount = (long)await cmd.ExecuteScalarAsync(); // Cast to long
                        }
                    }

                    if (reservationCount > 0)
                    {
                        TempData["ErrorMessage"] = "Negalima redaguoti šio staliuko, kadangi jis yra naudojamas ateities rezervacijose.";
                        return View("~/Views/Manager/TablesEditForm.cshtml", tableModel);
                    }



                    // Update table information in the database
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        await conn.OpenAsync();
                        string updateQuery = "UPDATE staliukas SET vietos = @capacity WHERE staliuko_nr = @tableNumber";

                        using (var cmd = new NpgsqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@capacity", tableModel.Capacity);
                            cmd.Parameters.AddWithValue("@tableNumber", tableModel.TableNumber);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Redirect to the TablesPage view
                    return RedirectToAction("TablesPage");
                }
                catch (Exception ex)
                {
                    // Handle any errors that occur during the update process
                    ViewBag.ErrorMessage = $"An error occurred while updating the table: {ex.Message}";
                }
            }

            // If model state is not valid, return to the edit form with the model
            return View("~/Views/Manager/TablesEditForm.cshtml", tableModel);
        }


        // TABLE GET

        public IActionResult ReturnToTablesPage()
        {
            return RedirectToAction("TablesPage");
        }

        public async Task<IActionResult> TablesPage()
        {
            List<TableInfo> staliukai = new List<TableInfo>();

            using (var conn = new NpgsqlConnection(connString))
            {
                try
                {
                    conn.Open();

                    string query = "SELECT * FROM staliukas ORDER BY staliuko_nr";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int staliuko_nr = reader.GetInt32(0);
                                int vietos = reader.GetInt32(1);
                                staliukai.Add(new TableInfo { TableNumber = staliuko_nr, Capacity = vietos });
                            }
                        }
                    }

                    conn.Close();
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"Failed to connect to PostgreSQL: {ex.Message}";
                }
            }

            return View("~/Views/Manager/TablesPage.cshtml", staliukai);
        }
    }
}
