using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace FunctionApp1
{
    public static class fsctest
    {
        [FunctionName("fsctest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
               [CosmosDB(
                databaseName: "my-database",
                collectionName: "my-container",
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
            [Queue("outqueue"), StorageAccount("AzureWebJobsStorage")] ICollector<string> msg,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            
            // gjr add json document to CosmosdB output container
            if (!string.IsNullOrEmpty(name))
            {
                // Add a JSON document to the output container.
                await documentsOut.AddAsync(new
                {
                    // create a random ID
                    id = System.Guid.NewGuid().ToString(),
                    name = name
                });
            }

            //gjr  add message to storage queue
            if (!string.IsNullOrEmpty(name))
            {
                // Add a message to the output collection.
                msg.Add(string.Format("Name passed to the function: {0}", name));
            }
            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
