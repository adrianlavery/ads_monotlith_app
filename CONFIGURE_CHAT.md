# Configure AI Chat Assistant

## Quick Setup

The chat assistant is now fully integrated and ready to use! Follow these steps to enable Azure OpenAI:

### Option 1: Using Environment Variables (Recommended)

Set these environment variables before running the application:

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="your-api-key-here"
```

### Option 2: Update appsettings.json

Edit `appsettings.json` or `appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4",
    "MaxTokens": 800,
    "Temperature": 0.7
  }
}
```

## Getting Azure OpenAI Credentials

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure OpenAI resource
3. Click on "Keys and Endpoint"
4. Copy:
   - **Endpoint**: `https://your-resource.openai.azure.com/`
   - **Key**: One of the two keys provided

## Deployment Name

Make sure your deployment name matches the model you deployed in Azure OpenAI:
- `gpt-4` (recommended)
- `gpt-4-turbo`
- `gpt-35-turbo`

## Features

✅ **Intelligent Product Search**: Ask about products, prices, and categories
✅ **Real-time Responses**: Powered by Azure OpenAI GPT-4
✅ **Context-Aware**: Remembers conversation history
✅ **Helpful Suggestions**: Quick-start buttons for common queries
✅ **Product Knowledge**: Has access to your entire product catalog
✅ **Floating UI**: Non-intrusive popup interface

## Example Queries

- "Show me laptops under £1000"
- "What products do you have in the Electronics category?"
- "Tell me about your best-selling items"
- "I need a phone with good camera quality"
- "What's the price of [product name]?"

## Testing Without Azure OpenAI

If Azure OpenAI is not configured, the chat will display a friendly error message asking you to configure it. The application will continue to work normally for all other features.

## Troubleshooting

**Error: "Failed to connect"**
- Check your Azure OpenAI endpoint URL
- Verify your API key is correct
- Ensure the deployment name matches your Azure OpenAI deployment

**No response from chat**
- Check browser console for errors (F12)
- Verify the `/api/chat` endpoint is accessible
- Check application logs for detailed error messages
