﻿// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Floating Chat Button functionality
document.addEventListener('DOMContentLoaded', function() {
    const chatButton = document.getElementById('chatButton');
    const chatModal = new bootstrap.Modal(document.getElementById('chatModal'));
    const chatInput = document.getElementById('chatInput');
    const sendButton = document.getElementById('sendMessage');
    const chatMessages = document.getElementById('chatMessages');

    // Open chat modal when button is clicked
    chatButton.addEventListener('click', function() {
        chatModal.show();
        chatInput.focus();
    });

    // Send message on button click
    sendButton.addEventListener('click', sendChatMessage);

    // Send message on Enter key
    chatInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            sendChatMessage();
        }
    });

    function sendChatMessage() {
        const message = chatInput.value.trim();
        if (message === '') return;

        // Clear the initial welcome message if it exists
        const welcomeMessage = chatMessages.querySelector('.text-center.text-muted');
        if (welcomeMessage) {
            welcomeMessage.remove();
        }

        // Add user message
        addMessage(message, 'user');
        chatInput.value = '';

        // Simulate AI response (you can replace this with actual API call)
        setTimeout(() => {
            const response = getAIResponse(message);
            addMessage(response, 'assistant');
        }, 1000);
    }

    function addMessage(text, sender) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `chat-message ${sender}`;
        messageDiv.innerHTML = `
            <div>${text}</div>
            <div class="chat-message-time">${new Date().toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</div>
        `;
        chatMessages.appendChild(messageDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    function getAIResponse(message) {
        // Simple response logic - replace with actual AI integration
        const lowerMessage = message.toLowerCase();
        
        if (lowerMessage.includes('product') || lowerMessage.includes('item')) {
            return "I can help you find products! Visit the Products page to browse our inventory, or tell me what you're looking for.";
        } else if (lowerMessage.includes('order')) {
            return "You can view your order history on the Orders page. Would you like me to help you with a specific order?";
        } else if (lowerMessage.includes('cart')) {
            return "Your shopping cart is accessible from the Cart page. You can add, remove, or update items there.";
        } else if (lowerMessage.includes('insight') || lowerMessage.includes('analytics')) {
            return "Check out our AI-powered Insights page for detailed sales analytics and recommendations!";
        } else if (lowerMessage.includes('help')) {
            return "I'm here to help! You can ask me about products, orders, your cart, or sales insights. What would you like to know?";
        } else {
            return "Thanks for your message! I'm currently a demo assistant. For full AI capabilities, integrate with Azure OpenAI. How else can I help you today?";
        }
    }
});