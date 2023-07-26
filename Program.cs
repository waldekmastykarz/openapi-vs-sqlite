using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

async Task OpenApiYamlToSqlite()
{
    using var connection = new SqliteConnection("Data Source=graph-v1_0-openapi.db");
    connection.Open();

    var createTable = connection.CreateCommand();
    createTable.CommandText = "CREATE TABLE IF NOT EXISTS endpoints (path TEXT, graphVersion TEXT, hasSelect BOOLEAN)";
    createTable.ExecuteNonQuery();

    // Add an index on the path and graphVersion columns
    var createIndex = connection.CreateCommand();
    createIndex.CommandText = "CREATE INDEX IF NOT EXISTS idx_endpoints_path_version ON endpoints (path, graphVersion)";
    createIndex.ExecuteNonQuery();

    var file = new FileInfo("/Users/waldek/github/waldekmastykarz/openapi-vs-sqlite/graph-v1_0-openapi.yaml");
    var openApiFile = await new OpenApiStreamReader().ReadAsync(file.OpenRead());
    var openApiDoc = openApiFile.OpenApiDocument;

    // Iterate over the paths in the OpenAPI document
    foreach (var path in openApiDoc.Paths)
    {
        // Get the GET operation for this path
        var getOperation = path.Value.Operations.FirstOrDefault(o => o.Key == OperationType.Get).Value;
        if (getOperation == null)
        {
            continue;
        }

        // Check if the GET operation has a $select parameter
        var hasSelect = getOperation.Parameters.Any(p => p.Name == "$select");

        // Add a row to the endpoints table for this endpoint
        var insertEndpoint = connection.CreateCommand();
        insertEndpoint.CommandText = "INSERT INTO endpoints (path, graphVersion, hasSelect) VALUES (@path, @graphVersion, @hasSelect)";
        insertEndpoint.Parameters.AddWithValue("@path", path.Key);
        insertEndpoint.Parameters.AddWithValue("@graphVersion", "v1.0");
        insertEndpoint.Parameters.AddWithValue("@hasSelect", hasSelect);
        insertEndpoint.ExecuteNonQuery();
    }
}

void LookupFromSqlLite()
{
    using var connection = new SqliteConnection("Data Source=graph-v1_0-openapi.db");
    connection.Open();

    var endpoint = "/me";
    var graphVersion = "v1.0";
    // lookup information from the database
    var selectEndpoint = connection.CreateCommand();
    selectEndpoint.CommandText = "SELECT hasSelect FROM endpoints WHERE path = @path AND graphVersion = @graphVersion";
    selectEndpoint.Parameters.AddWithValue("@path", endpoint);
    selectEndpoint.Parameters.AddWithValue("@graphVersion", graphVersion);
    var result = selectEndpoint.ExecuteScalar();
    var hasSelect = result != null && Convert.ToInt32(result) == 1;
    if (hasSelect)
    {
        Console.WriteLine($"Endpoint {graphVersion}/{endpoint} supports $select");
    }
    else
    {
        Console.WriteLine($"Endpoint {graphVersion}/{endpoint} doesn't support $select");
    }
}

async Task LookupFromFile()
{
    var file = new FileInfo("/Users/waldek/github/waldekmastykarz/openapi-vs-sqlite/graph-v1_0-openapi.yaml");
    var openApiFile = await new OpenApiStreamReader().ReadAsync(file.OpenRead());
    var openApiDoc = openApiFile.OpenApiDocument;

    var endpoint = "/me";
    var hasSelect = openApiDoc.Paths[endpoint].Operations[OperationType.Get].Parameters.Any(p => p.Name == "$select");
    Console.WriteLine(hasSelect);
}

// await OpenApiYamlToSqlite();
// get current process id
var processId = Process.GetCurrentProcess().Id;
await LookupFromFile();
// LookupFromSqlLite();
Console.WriteLine("DONE");
Console.ReadLine();