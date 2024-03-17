using Npgsql;
using System.Data;
using System.Data.SqlClient;

namespace Restoranas.Helper
{
    public static class DataSource
    {
        /// <summary>
        /// Atnaujina duomenis pagal SQL pateikta kodą
        /// </summary>
        /// <param name="CommandText">SQL querry</param>
        public static bool UpdateDataSQL(string CommandText)
        {
            string connString = "Host=ep-solitary-forest-a28gt5ec-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=psapi_faxai;Username=psapi_faxai_owner;Password=g3xbiOmuETp7;";

            try
            {
                using (var connection = new NpgsqlConnection(connString))
                {
                    using (var command = new NpgsqlCommand(CommandText, connection))
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
