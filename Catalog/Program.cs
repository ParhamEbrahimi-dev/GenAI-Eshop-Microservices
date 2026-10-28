
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<CatalogDbContext>(connectionName: "catalogdb");

builder.Services.AddScoped<ProductService>();

//Add Chat AI Client
var credential = new ApiKeyCredential(builder.Configuration["AvalAi:Token"] ?? throw new InvalidOperationException());
var options = new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.avalai.ir/v1")
};

IChatClient chatClient = new OpenAIClient(credential, options).GetChatClient("gpt-5-mini").AsIChatClient();

builder.Services.AddChatClient(chatClient);



var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseMigration();

app.MapProductEndpoints();

app.Run();
