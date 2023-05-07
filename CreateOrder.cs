using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace Marketplace
{
    public class Order
    {
      public int OrderId { get; set; }

      public string ProductName { get; set; }

      public int Quantity { get; set; }
    }

    public class ApiResponse
    {
      public string Message { get; set; }

      public ApiResponse(string message) {
        this.Message = message;
      }
    }

    public static class CreateOrder
    {
        [FunctionName("CreateOrder")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Order order = JsonConvert.DeserializeObject<Order>(requestBody);

            using (SqlConnection connection = GetConnection())
            {
              connection.Open();

              // Create table if not exists
              SqlCommand createTableCommand = new SqlCommand("IF OBJECT_ID('dbo.Orders', 'U') IS NULL CREATE TABLE Orders (Id INT PRIMARY KEY,ProductName VARCHAR(300) NOT NULL,Quantity INT NOT NULL);", connection);
              createTableCommand.ExecuteNonQuery();

              SqlCommand createOrderCommand = new SqlCommand("INSERT INTO Orders VALUES (@Id, @ProductName, @Quantity)", connection);
              createOrderCommand.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = order.OrderId;
              createOrderCommand.Parameters.Add("@ProductName", System.Data.SqlDbType.VarChar).Value = order.ProductName;
              createOrderCommand.Parameters.Add("@Quantity", System.Data.SqlDbType.Int).Value = order.Quantity;

              createOrderCommand.ExecuteNonQuery();

              connection.Close();
            }

            ApiResponse response = new ApiResponse("An order has been placed");

            return new OkObjectResult(response);
        }

        private static SqlConnection GetConnection() {
          string connectionString = Environment.GetEnvironmentVariable("SQLAZURECONNSTR_SQL_CONNECTION");

          return new SqlConnection(connectionString);
        }
    }
}
