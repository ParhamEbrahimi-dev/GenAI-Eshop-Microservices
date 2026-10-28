using Microsoft.Extensions.AI;

namespace Catalog.Services
{
    public class ProductAIService(IChatClient chatClient, CatalogDbContext dbContext)
    {
        public async Task<string> SupportAsync(string userQuery, CancellationToken cancellationToken)
        {
            var products = await dbContext.Products
                .AsNoTracking()
                .Select(p => new { p.Id, p.Name, p.Price })
                .ToListAsync(cancellationToken);

            string productCatalog = products.Count == 0 ? "- (No products Available)" :
                string.Join("\n", products.Select(p => $"- Id:{p.Id} Name:\"{p.Name}\" Price:{p.Price}"));

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

            var response = await chatClient.GetResponseAsync(chatHistory,cancellationToken: cancellationToken);

            return response.Text ?? "No description available";

        }
    }
}
