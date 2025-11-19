using Azure;
using Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using RetailMonolith.Data;
using RetailMonolith.Models;
using System.Text;
using System.Text.Json;

namespace RetailMonolith.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _db;
        private readonly AzureOpenAIClient _aiClient;
        private readonly string _deploymentName;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(
            AppDbContext db,
            IConfiguration configuration,
            ILogger<AnalyticsService> logger)
        {
            _db = db;
            _logger = logger;

            var endpoint = configuration["AzureOpenAI:Endpoint"] 
                ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
            var apiKey = configuration["AzureOpenAI:ApiKey"] 
                ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not configured");
            _deploymentName = configuration["AzureOpenAI:DeploymentName"] 
                ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured");

            _aiClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        public async Task<SalesAnalysisData> GetSalesDataAsync(int daysBack = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-daysBack);

            var orders = await _db.Orders
                .Include(o => o.Lines)
                .Where(o => o.CreatedUtc >= startDate)
                .ToListAsync();

            // Get all products to enrich analysis with category data
            var products = await _db.Products.ToListAsync();
            var productDict = products.ToDictionary(p => p.Sku, p => p);

            var analysisData = new SalesAnalysisData
            {
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.Total),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.Total) : 0,
                OrdersByStatus = orders.GroupBy(o => o.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DailySales = orders.GroupBy(o => o.CreatedUtc.Date)
                    .Select(g => new DailySales
                    {
                        Date = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.Total)
                    })
                    .OrderBy(d => d.Date)
                    .ToList(),
                TopProducts = orders.SelectMany(o => o.Lines)
                    .GroupBy(l => l.Name)
                    .Select(g => new ProductSales
                    {
                        Name = g.Key,
                        QuantitySold = g.Sum(l => l.Quantity),
                        Revenue = g.Sum(l => l.UnitPrice * l.Quantity)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(10)
                    .ToList(),
                CategoryPerformance = orders.SelectMany(o => o.Lines)
                    .Where(l => productDict.ContainsKey(l.Sku))
                    .GroupBy(l => productDict[l.Sku].Category ?? "Uncategorized")
                    .Select(g => new CategoryPerformance
                    {
                        Category = g.Key,
                        ProductCount = g.Select(l => l.Sku).Distinct().Count(),
                        UnitsSold = g.Sum(l => l.Quantity),
                        Revenue = g.Sum(l => l.UnitPrice * l.Quantity),
                        AveragePrice = g.Any() ? g.Average(l => l.UnitPrice) : 0
                    })
                    .OrderByDescending(c => c.Revenue)
                    .ToDictionary(c => c.Category, c => c)
            };

            return analysisData;
        }

        public async Task<SalesInsight> GenerateSalesInsightAsync(int daysBack = 30)
        {
            try
            {
                // Get sales data
                var salesData = await GetSalesDataAsync(daysBack);

                // Build prompt for Azure OpenAI
                var prompt = BuildAnalysisPrompt(salesData, daysBack);

                // Call Azure OpenAI
                var chatClient = _aiClient.GetChatClient(_deploymentName);
                
                var messages = new List<OpenAI.Chat.ChatMessage>
                {
                    new SystemChatMessage("You are a retail sales analytics expert helping sales teams maximize revenue. Analyze sales data and provide actionable insights in a clear, concise manner. Focus on specific, measurable recommendations that sales representatives can act on immediately. Use direct, action-oriented language targeted at sellers."),
                    new UserChatMessage(prompt)
                };

                var response = await chatClient.CompleteChatAsync(messages);
                var insightText = response.Value.Content[0].Text;

                // Parse the response into structured insight
                var insight = ParseInsightResponse(insightText);
                insight.GeneratedAt = DateTime.UtcNow;

                return insight;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales insight");
                return new SalesInsight
                {
                    Summary = "Unable to generate insights at this time.",
                    Trends = "Error occurred while analyzing data.",
                    Recommendations = "Please check your Azure OpenAI configuration.",
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        private string BuildAnalysisPrompt(SalesAnalysisData data, int daysBack)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Analyze the following sales data from the last {daysBack} days and provide insights:");
            sb.AppendLine();
            sb.AppendLine($"**Overall Metrics:**");
            sb.AppendLine($"- Total Orders: {data.TotalOrders}");
            sb.AppendLine($"- Total Revenue: ${data.TotalRevenue:N2}");
            sb.AppendLine($"- Average Order Value: ${data.AverageOrderValue:N2}");
            sb.AppendLine();

            if (data.OrdersByStatus.Any())
            {
                sb.AppendLine($"**Orders by Status:**");
                foreach (var status in data.OrdersByStatus)
                {
                    sb.AppendLine($"- {status.Key}: {status.Value} orders");
                }
                sb.AppendLine();
            }

            if (data.CategoryPerformance.Any())
            {
                sb.AppendLine($"**Category Performance:**");
                foreach (var category in data.CategoryPerformance.Values.OrderByDescending(c => c.Revenue).Take(5))
                {
                    sb.AppendLine($"- {category.Category}: {category.UnitsSold} units sold, ${category.Revenue:N2} revenue, avg price ${category.AveragePrice:N2}");
                }
                sb.AppendLine();
            }

            if (data.TopProducts.Any())
            {
                sb.AppendLine($"**Top 5 Products:**");
                foreach (var product in data.TopProducts.Take(5))
                {
                    sb.AppendLine($"- {product.Name}: {product.QuantitySold} units, ${product.Revenue:N2} revenue");
                }
                sb.AppendLine();
            }

            if (data.DailySales.Any())
            {
                sb.AppendLine($"**Daily Sales Trend (last 7 days):**");
                foreach (var day in data.DailySales.TakeLast(7))
                {
                    sb.AppendLine($"- {day.Date:MMM dd}: {day.OrderCount} orders, ${day.Revenue:N2}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("Please provide:");
            sb.AppendLine("1. SUMMARY: A brief overview (2-3 sentences) of the sales performance");
            sb.AppendLine("2. TRENDS: Key trends and patterns you observe in the data");
            sb.AppendLine("3. RECOMMENDATIONS: Provide exactly 3-5 specific, actionable recommendations for the sales team.");
            sb.AppendLine();
            sb.AppendLine("For RECOMMENDATIONS, format each as follows (use this exact format):");
            sb.AppendLine("RECOMMENDATION:");
            sb.AppendLine("Title: [Short actionable title]");
            sb.AppendLine("Description: [Brief explanation why this matters]");
            sb.AppendLine("Action: [Specific action to take]");
            sb.AppendLine("Category: [One of: Upsell, Pricing, Marketing, Inventory, Customer]");
            sb.AppendLine("Priority: [One of: High, Medium, Low]");
            sb.AppendLine();
            sb.AppendLine("Focus on actionable, seller-focused language. Examples:");
            sb.AppendLine("- Focus on upselling high-margin products");
            sb.AppendLine("- Adjust pricing strategy for specific categories");
            sb.AppendLine("- Target marketing campaigns for underperforming segments");
            sb.AppendLine("- Bundle complementary products");
            sb.AppendLine("- Improve conversion for specific product categories");
            sb.AppendLine();
            sb.AppendLine("Format your response with clear section headers.");

            return sb.ToString();
        }

        private SalesInsight ParseInsightResponse(string response)
        {
            var insight = new SalesInsight();
            var sections = response.Split(new[] { "SUMMARY:", "TRENDS:", "RECOMMENDATIONS:" }, 
                StringSplitOptions.RemoveEmptyEntries);

            if (sections.Length >= 3)
            {
                insight.Summary = sections[1].Trim();
                insight.Trends = sections[2].Trim();
                insight.Recommendations = sections.Length > 3 ? sections[3].Trim() : sections[2].Trim();
                
                // Parse structured recommendations
                insight.ActionableRecommendations = ParseStructuredRecommendations(response);
            }
            else
            {
                // Fallback if parsing fails
                insight.Summary = response.Length > 500 ? response.Substring(0, 500) : response;
                insight.Trends = "See summary for details";
                insight.Recommendations = "See summary for recommendations";
            }

            return insight;
        }

        private List<ActionableRecommendation> ParseStructuredRecommendations(string response)
        {
            var recommendations = new List<ActionableRecommendation>();
            
            try
            {
                // Split by RECOMMENDATION: markers
                var recSections = response.Split(new[] { "RECOMMENDATION:" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var section in recSections.Skip(1)) // Skip first section (before first RECOMMENDATION:)
                {
                    var rec = new ActionableRecommendation();
                    var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                        {
                            rec.Title = trimmedLine.Substring(6).Trim();
                        }
                        else if (trimmedLine.StartsWith("Description:", StringComparison.OrdinalIgnoreCase))
                        {
                            rec.Description = trimmedLine.Substring(12).Trim();
                        }
                        else if (trimmedLine.StartsWith("Action:", StringComparison.OrdinalIgnoreCase))
                        {
                            rec.Action = trimmedLine.Substring(7).Trim();
                        }
                        else if (trimmedLine.StartsWith("Category:", StringComparison.OrdinalIgnoreCase))
                        {
                            rec.Category = trimmedLine.Substring(9).Trim();
                        }
                        else if (trimmedLine.StartsWith("Priority:", StringComparison.OrdinalIgnoreCase))
                        {
                            rec.Priority = trimmedLine.Substring(9).Trim();
                        }
                    }
                    
                    // Only add if we have at least a title
                    if (!string.IsNullOrEmpty(rec.Title))
                    {
                        recommendations.Add(rec);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse structured recommendations, falling back to text-only");
            }

            return recommendations;
        }
    }
}
