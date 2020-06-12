using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System;

namespace my.retris.Function
{
    public static class retrisdbaccess
    {
        private static string SQL_CONNECTION = Environment.GetEnvironmentVariable("SQLAZURECONNSTR_sqldb_connection", EnvironmentVariableTarget.Process);        

        [FunctionName("retrisdbaccess")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ResultModel>(requestBody);

            Task<ObjectResult> result;
            if (data == null)
            {
                result = HandleGet(log);
            } else
            {
                result = HandlePost(data, log);
            }

            return await result;
        }

        public async static Task<ObjectResult> HandleGet(ILogger log)
        {
            log.LogInformation("Returning top 10 score");
            var results = new List<Dictionary<string, string>>();
            using (SqlConnection connection = new SqlConnection(SQL_CONNECTION))
            {
                await connection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand("SELECT TOP 10 * FROM results ORDER BY score DESC", connection))
                {
                    var reader = await sqlCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = (IDataRecord) reader;
                        var result = new Dictionary<string, string>();
                        result.Add("nick", row["nick"].ToString());
                        result.Add("score", row["score"].ToString());
                        
                        log.LogInformation($"results: {JsonConvert.SerializeObject(result)}");
                        results.Add(result);
                    }
                    
                }
            }
            return new OkObjectResult(JsonConvert.SerializeObject(results));
        }

        public async static Task<ObjectResult> HandlePost(ResultModel data, ILogger log)
        {
            if (string.IsNullOrEmpty(data.Nick))
            {
                return new BadRequestObjectResult("Invalid data");
            }
            log.LogInformation($"Inserting {data}");
            var results = new List<Dictionary<string, string>>();

            using (SqlConnection connection = new SqlConnection(SQL_CONNECTION))
            {
                await connection.OpenAsync();
                using (SqlCommand sqlCommand = new SqlCommand($"INSERT INTO results (nick, score) VALUES ('{data.Nick}', {data.Score})", connection))
                {
                    var linesChanged = await sqlCommand.ExecuteNonQueryAsync();
                    if (linesChanged > 0)
                    {
                        return new OkObjectResult(JsonConvert.SerializeObject(data));
                    }
                    
                }
            }
            return new BadRequestObjectResult("Something went wrong");
        }

        public class ResultModel
        {
            public string Nick { get; set; }
            public int Score { get; set; }

            public override string ToString()
            {
                return $"{Nick} - {Score}";
            }
        }
    }
}
