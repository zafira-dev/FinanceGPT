using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace FinanceGPT
{
    public static class DatabaseHelper
    {
        // Your SQL Server Configuration
        // Server: SQLEXPRESS (SQL Server 16.0.1000 - DESKTOP-ATLQNI8\zafir)
        // Database: FinanceGPT

        public static string ConnectionString { get; set; } =
            @"Server=DESKTOP-ATLQNI8\SQLEXPRESS;Database=FinanceGPT;Integrated Security=True;";

        /// <summary>
        /// Test the database connection
        /// </summary>
        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Get a new SQL connection
        /// </summary>
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}