
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<CatalogDbContext>(connectionName: "catalogdb");

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductAIService>();

//Add Chat AI Client
var credential = new ApiKeyCredential(builder.Configuration["AvalAi:Token"] ?? throw new InvalidOperationException());
var options = new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.avalai.ir/v1")
};

var openAIClient = new OpenAIClient(credential, options);

var chatClient = openAIClient.GetChatClient("gpt-5-mini").AsIChatClient();

var embeddingGenerator = openAIClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();

builder.Services.AddChatClient(chatClient);
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

//Add VectorDb Client Operation(Search)
builder.AddQdrantClient("vectordb");
builder.Services.AddQdrantCollection<ulong, ProductVector>("product-vectors");


var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseMigration();

app.MapProductEndpoints();

app.Run();
