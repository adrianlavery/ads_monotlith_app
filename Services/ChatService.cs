using Azure;
using Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using RetailMonolith.Data;
using System.Text;
using System.Text.Json;

namespace RetailMonolith.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatService> _logger;
        private AzureOpenAIClient? _openAIClient;
        private ChatClient? _chatClient;

        public ChatService(AppDbContext db, IConfiguration config, ILogger<ChatService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        private void EnsureClientsInitialized()
        {
            if (_openAIClient != null && _chatClient != null)
                return;

            var endpoint = _config["AzureOpenAI:Endpoint"];
            var apiKey = _config["AzureOpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException(
                    "Azure OpenAI endpoint is not configured. Please set the AZURE_OPENAI_ENDPOINT environment variable.");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Azure OpenAI API key is not configured. Please set the AZURE_OPENAI_API_KEY environment variable.");
            }

            var deploymentName = _config["AzureOpenAI:DeploymentName"] ?? "gpt-4";

            _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _chatClient = _openAIClient.GetChatClient(deploymentName);
        }

        public async Task<string> BuildSystemPromptAsync(CancellationToken ct = default)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("You are a helpful retail shopping assistant for RetailMonolith, an online retail store.");
            sb.AppendLine("Your role is to help customers find products, answer questions about their cart and orders, and provide shopping recommendations.");
            sb.AppendLine();
            sb.AppendLine("## Available Product Categories:");
            
            var categories = await _db.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync(ct);
            
            foreach (var category in categories)
            {
                var count = await _db.Products.CountAsync(p => p.IsActive && p.Category == category, ct);
                sb.AppendLine($"- {category}: {count} products available");
            }

            sb.AppendLine();
            sb.AppendLine("## Complete Product Catalog:");
            sb.AppendLine("Below is the COMPLETE list of ALL products available in the store. Use this information to answer ANY product-related questions.");
            sb.AppendLine();
            
            // Load ALL products from the database
            var products = await _db.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync(ct);

            var groupedProducts = products.GroupBy(p => p.Category);
            
            foreach (var categoryGroup in groupedProducts)
            {
                sb.AppendLine($"\n### {categoryGroup.Key} ({categoryGroup.Count()} items):");
                foreach (var product in categoryGroup)
                {
                    var priceFormatted = product.Currency == "USD" ? $"${product.Price:F2}" : $"£{product.Price:F2}";
                    sb.AppendLine($"- **{product.Name}** (SKU: {product.Sku}): {priceFormatted}");
                    if (!string.IsNullOrWhiteSpace(product.Description))
                    {
                        sb.AppendLine($"  Description: {product.Description}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Instructions:");
            sb.AppendLine("1. You have the COMPLETE product catalog above. Answer ALL questions about products, prices, and availability accurately.");
            sb.AppendLine("2. When asked about specific products (e.g., 'Electronics Item 1'), search the catalog above and provide the EXACT price and details.");
            sb.AppendLine("3. When customers ask 'how much is [product]?', respond with the exact price from the catalog above.");
            sb.AppendLine("4. Always format prices with the currency symbol (£ or $) as shown in the catalog.");
            sb.AppendLine("5. When recommending products, mention the product name, SKU, and price.");
            sb.AppendLine("6. If asked about price ranges, list all products within that budget from the catalog above.");
            sb.AppendLine("7. For cart questions, direct users to /Cart/Index. For checkout, direct to /Checkout/Index. For orders, direct to /Orders/Index.");
            sb.AppendLine("8. Be friendly, accurate, and helpful. You have ALL product information - use it to answer questions precisely.");
            sb.AppendLine("9. If a customer asks about a product by name or partial name, search the catalog and provide the exact match with price.");

            return sb.ToString();
        }

        public async Task<string> GetChatResponseAsync(string userMessage, string conversationHistory, CancellationToken ct = default)
        {
            try
            {
                EnsureClientsInitialized();
                
                var systemPrompt = await BuildSystemPromptAsync(ct);
                
                var messages = new List<OpenAI.Chat.ChatMessage>
                {
                    new SystemChatMessage(systemPrompt)
                };

                // Parse conversation history if provided
                if (!string.IsNullOrWhiteSpace(conversationHistory))
                {
                    try
                    {
                        var history = JsonSerializer.Deserialize<List<RetailMonolith.Services.ChatMessage>>(conversationHistory);
                        if (history != null)
                        {
                            foreach (var msg in history)
                            {
                                if (msg.Role == "user")
                                    messages.Add(new UserChatMessage(msg.Content));
                                else if (msg.Role == "assistant")
                                    messages.Add(new AssistantChatMessage(msg.Content));
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse conversation history");
                    }
                }

                // Add the current user message
                messages.Add(new UserChatMessage(userMessage));

                var chatOptions = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = int.TryParse(_config["AzureOpenAI:MaxTokens"], out var maxTokens) ? maxTokens : 800,
                    Temperature = float.TryParse(_config["AzureOpenAI:Temperature"], out var temp) ? temp : 0.7f
                };

                var response = await _chatClient!.CompleteChatAsync(messages, chatOptions, ct);

                return response.Value.Content[0].Text;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Azure OpenAI configuration error");
                return "Sorry, I'm having trouble connecting. Please check your Azure OpenAI configuration and try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat response from Azure OpenAI");
                return "I apologize, but I'm having trouble connecting to the chat service right now. Please try again in a moment or browse our products directly.";
            }
        }
    }
}
