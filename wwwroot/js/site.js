﻿// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Floating Chat Button functionality
document.addEventListener('DOMContentLoaded', function() {
    const chatButton = document.getElementById('chatButton');
    const chatWindow = document.getElementById('chatWindow');
    const closeChatBtn = document.getElementById('closeChatBtn');
    const chatInput = document.getElementById('chatInput');
    const sendButton = document.getElementById('sendMessage');
    const chatMessages = document.getElementById('chatMessages');
    let isChatOpen = false;
    let conversationHistory = [];
    let isWaitingForResponse = false;

    // Toggle chat window when button is clicked
    chatButton.addEventListener('click', function() {
        if (isChatOpen) {
            closeChatWindow();
        } else {
            openChatWindow();
        }
    });

    // Close chat when close button is clicked
    closeChatBtn.addEventListener('click', function() {
        closeChatWindow();
    });

    // Close chat when clicking outside the chat window
    document.addEventListener('click', function(e) {
        if (isChatOpen && 
            !chatWindow.contains(e.target) && 
            !chatButton.contains(e.target)) {
            closeChatWindow();
        }
    });

    function openChatWindow() {
        chatWindow.style.display = 'flex';
        isChatOpen = true;
        
        // Show suggestions if this is the first time opening (no history)
        if (conversationHistory.length === 0) {
            showInitialSuggestions();
        }
        
        chatInput.focus();
    }

    function closeChatWindow() {
        chatWindow.style.display = 'none';
        isChatOpen = false;
    }

    function showInitialSuggestions() {
        // Clear the welcome message if it exists
        const welcomeMessage = chatMessages.querySelector('.text-center.text-muted');
        if (welcomeMessage) {
            welcomeMessage.remove();
        }

        const suggestionsDiv = document.createElement('div');
        suggestionsDiv.className = 'chat-suggestions';
        suggestionsDiv.innerHTML = `
            <div class="welcome-message">
                <i class="bi bi-robot" style="font-size: 2rem; color: #667eea;"></i>
                <h6 class="mt-2 mb-3">👋 Hello! I'm your AI shopping assistant</h6>
                <p class="text-muted small mb-3">I can help you with:</p>
            </div>
            <div class="suggestion-buttons">
                <button class="suggestion-btn" data-suggestion="What products do you have available?">
                    <i class="bi bi-box-seam"></i> Browse Products
                </button>
                <button class="suggestion-btn" data-suggestion="Show me laptops under £1000">
                    <i class="bi bi-laptop"></i> Find Laptops
                </button>
                <button class="suggestion-btn" data-suggestion="What are your best selling products?">
                    <i class="bi bi-star"></i> Best Sellers
                </button>
                <button class="suggestion-btn" data-suggestion="Tell me about your electronics">
                    <i class="bi bi-phone"></i> Electronics
                </button>
            </div>
        `;
        chatMessages.appendChild(suggestionsDiv);

        // Add click handlers to suggestion buttons
        const suggestionBtns = suggestionsDiv.querySelectorAll('.suggestion-btn');
        suggestionBtns.forEach(btn => {
            btn.addEventListener('click', function() {
                const suggestion = this.getAttribute('data-suggestion');
                chatInput.value = suggestion;
                sendChatMessage();
            });
        });
    }

    // Send message on button click
    sendButton.addEventListener('click', sendChatMessage);

    // Send message on Enter key
    chatInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendChatMessage();
        }
    });

    async function sendChatMessage() {
        const message = chatInput.value.trim();
        if (message === '' || isWaitingForResponse) return;

        // Remove suggestions if they exist
        const suggestions = chatMessages.querySelector('.chat-suggestions');
        if (suggestions) {
            suggestions.remove();
        }

        // Clear the initial welcome message if it exists
        const welcomeMessage = chatMessages.querySelector('.text-center.text-muted');
        if (welcomeMessage) {
            welcomeMessage.remove();
        }

        // Add user message to UI
        addMessage(message, 'user');
        chatInput.value = '';
        
        // Add to conversation history
        conversationHistory.push({
            role: 'user',
            content: message
        });

        // Show typing indicator
        isWaitingForResponse = true;
        sendButton.disabled = true;
        const typingIndicator = showTypingIndicator();

        try {
            // Call the API
            const response = await fetch('/api/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    message: message,
                    history: conversationHistory.slice(0, -1) // Send history without the current message
                })
            });

            const data = await response.json();
            
            // Remove typing indicator
            typingIndicator.remove();

            if (data.success && data.message) {
                // Add assistant response to UI
                addMessage(data.message, 'assistant');
                
                // Add to conversation history
                conversationHistory.push({
                    role: 'assistant',
                    content: data.message
                });
            } else {
                // Show error message
                addMessage(data.error || 'Sorry, I encountered an error. Please try again.', 'assistant', true);
            }
        } catch (error) {
            console.error('Chat error:', error);
            typingIndicator.remove();
            addMessage('Sorry, I\'m having trouble connecting. Please check your Azure OpenAI configuration and try again.', 'assistant', true);
        } finally {
            isWaitingForResponse = false;
            sendButton.disabled = false;
            chatInput.focus();
        }
    }

    function showTypingIndicator() {
        const typingDiv = document.createElement('div');
        typingDiv.className = 'chat-message assistant typing-indicator';
        typingDiv.innerHTML = `
            <div class="typing-dots">
                <span></span>
                <span></span>
                <span></span>
            </div>
        `;
        chatMessages.appendChild(typingDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
        return typingDiv;
    }

    function addMessage(text, sender, isError = false) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `chat-message ${sender}${isError ? ' error' : ''}`;
        
        // Convert markdown-style formatting to HTML
        const formattedText = formatMessageText(text);
        
        messageDiv.innerHTML = `
            <div>${formattedText}</div>
            <div class="chat-message-time">${new Date().toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</div>
        `;
        chatMessages.appendChild(messageDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    function formatMessageText(text) {
        // Simple formatting: convert newlines to <br> and basic markdown
        return text
            .replace(/\n/g, '<br>')
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/£(\d+\.?\d*)/g, '<strong>£$1</strong>');
    }
});