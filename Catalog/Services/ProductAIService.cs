using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Catalog.Services
{
    public class ProductAIService(
        IChatClient chatClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        VectorStoreCollection<ulong, ProductVector> productVectorCollection,
        CatalogDbContext dbContext)
    {
        public async Task<string> SupportAsync(string userQuery, CancellationToken cancellationToken = default)
        {
            var products = await dbContext.Products
                .AsNoTracking()
                .Select(p => new { p.Id, p.Name, p.Price })
                .ToListAsync(cancellationToken);

            string productCatalog = products.Count == 0 ? "- (No products Available)" :
                string.Join("\n", products.Select(p => $"- Id:{p.Id} Name:\"{p.Name}\" Price:{p.Price:C}"));

            var systemPrompt = $"""
                You are a helpfull assistant fot an outdoor camping products store.
                
                Rules:
                1. Only answer questions related to outdoor camping or the Product catalog. If unrelated say exactly: "I only answer question about outdoor camping products"
                2. Be concise and a little funny (light humor).
                3. If you don't know,reply exaclty: "I don't know that."
                4. Do not store memory of the conversation.
                5. when appropriate (most user questions),en with ONE relevant product recommendation from the catalog below.
                    - Pick the product that best matches the user's intent; if none clearly match.pick a random one.
                    - Format the recommendation on a new final line as:
                        Recommendation: <Product Name> - <Price>
                6. do NOT invent products not in the  catalog.

                Product Catalog:
                {productCatalog}
                """;

            var chatHistory = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System,systemPrompt),
                new (ChatRole.User,userQuery)
            };

            var response = await chatClient.GetResponseAsync(chatHistory, cancellationToken: cancellationToken);

            return response.Text ?? "No description available";

        }

        public async Task InitEmbeddingAsync()
        {
            await productVectorCollection.EnsureCollectionExistsAsync();

            var products = await dbContext.Products.ToListAsync();

            foreach (var product in products)
            {
                var productInfo = $"[{product.Name}] is a product that costs [{product.Price}] and is described as [{product.Description}]";

                var productVector = new ProductVector
                {
                    Id = (ulong)product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = (double)product.Price,
                    ImageUrl = product.ImageUrl,
                    Vector = await embeddingGenerator.GenerateVectorAsync(productInfo)
                };

                await productVectorCollection.UpsertAsync(productVector);
            }
        }
        public async Task<IEnumerable<Product>> SearchProductAsync(string query)
        {
            //Step1: Use IEmbeddingGenerator to turn the user's query to the vector.
            //Step2: Use IVectorStore to Search Qdrant for the most similar product vectors.
            //Step3: Get the IDs of the matching products from the search result.
            //Step4: Retrieve the full product details from our main PostgresSQL database for those IDs.
            //Step5: Return the list of Products.

            if (!await productVectorCollection.CollectionExistsAsync())
            {
                await InitEmbeddingAsync();
            }

            var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(query);

            var results = productVectorCollection.SearchAsync(queryEmbedding, 1);

            List<Product> products = [];

            await foreach (var resultItem in results)
            {
                products.Add(new Product
                {
                    Id = (int)resultItem.Record.Id,
                    Name = resultItem.Record.Name,
                    Description = resultItem.Record.Description,
                    Price = (decimal)resultItem.Record.Price,
                    ImageUrl = resultItem.Record.ImageUrl,
                });
            }

            return products;

        }
    }

}
