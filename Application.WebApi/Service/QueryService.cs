using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Application.WebApi.Service;

public class QueryService
{


    //private readonly string _connectionString;

    //public QueryService()
    //{
    //    // Manually loading the configuration file
    //    var configuration = new ConfigurationBuilder()
    //        .SetBasePath(Directory.GetCurrentDirectory())  // Set the base path to the current directory
    //        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)  // Add appsettings.json
    //        .Build();

    //    // Access the connection string directly from appsettings.json
    //    _connectionString = configuration.GetConnectionString("CommonDb");
    //}

    //public async Task<DataTable> GetDataTableFromSqlAsync(string query)
    //{

    //    DataTable dataTable = new DataTable();

    //    try
    //    {
    //        using (SqlConnection connection = new SqlConnection(_connectionString))
    //        {
    //            // Open the connection
    //            await connection.OpenAsync();
    //            Console.WriteLine("Database connection opened successfully.");

    //            // Execute the query
    //            using (SqlCommand command = new SqlCommand(query, connection))
    //            {
    //                using (SqlDataReader reader = await command.ExecuteReaderAsync())
    //                {
    //                    // Load the data into the DataTable
    //                    dataTable.Load(reader);
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"An error occurred: {ex.Message}");
    //    }

    //    return dataTable;
    //}
}
