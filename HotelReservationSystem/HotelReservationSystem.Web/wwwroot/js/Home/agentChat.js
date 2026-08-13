"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
(() => {
    const agentContainer = document.getElementById("agent-container");
    const agentButton = document.getElementById("agent-button");
    const agentChatButton = document.getElementById("agent-chat-button");
    const closeButton = document.querySelector(".agent-header .btn-close-chat");
    const messagesList = document.querySelector(".text-messages-list");
    const typingIndicator = document.getElementById("chat-typing-indicator");
    let sessionId = sessionStorage.getItem("SESSION_ID");
    if (!sessionId) {
        sessionId = crypto.randomUUID();
        sessionStorage.setItem("SESSION_ID", sessionId);
    }
    agentButton === null || agentButton === void 0 ? void 0 : agentButton.addEventListener('click', (e) => {
        e.stopPropagation();
        agentContainer === null || agentContainer === void 0 ? void 0 : agentContainer.classList.toggle('d-none');
        if (messagesList) {
            messagesList.scrollTo(0, messagesList.scrollHeight);
        }
    });
    agentChatButton === null || agentChatButton === void 0 ? void 0 : agentChatButton.addEventListener('click', (e) => {
        e.stopPropagation();
        agentContainer === null || agentContainer === void 0 ? void 0 : agentContainer.classList.toggle('d-none');
        if (messagesList) {
            messagesList.scrollTo(0, messagesList.scrollHeight);
        }
    });
    closeButton === null || closeButton === void 0 ? void 0 : closeButton.addEventListener('click', () => {
        agentContainer === null || agentContainer === void 0 ? void 0 : agentContainer.classList.add('d-none');
        if (messagesList) {
            messagesList.innerHTML = '';
            sessionStorage.removeItem("SESSION_ID");
            sessionId = crypto.randomUUID();
            sessionStorage.setItem("SESSION_ID", sessionId);
        }
    });
    document.addEventListener('click', function (event) {
        if (!(event.target instanceof Node))
            return;
        if (!agentContainer || !agentChatButton)
            return;
        const isOpen = !agentContainer.classList.contains('d-none');
        if (isOpen) {
            const clickedOutsideContainer = !agentContainer.contains(event.target);
            const clickedOutsideButton = !agentChatButton.contains(event.target);
            if (clickedOutsideContainer && clickedOutsideButton) {
                agentContainer.classList.add('d-none');
            }
        }
    });
    const sendMessage = (request) => __awaiter(void 0, void 0, void 0, function* () {
        var _a;
        const token = (_a = document.querySelector('input[name="__RequestVerificationToken"]')) === null || _a === void 0 ? void 0 : _a.value;
        const userMsgHtml = `
        <div class="text-message p-2 px-3 shadow-sm user-message align-self-end text-white bg-primary">
            ${request.message}
        </div>`;
        messagesList === null || messagesList === void 0 ? void 0 : messagesList.insertAdjacentHTML('beforeend', userMsgHtml);
        if (messagesList) {
            messagesList.scrollTo(0, messagesList.scrollHeight);
        }
        // Show typing indicator
        typingIndicator === null || typingIndicator === void 0 ? void 0 : typingIndicator.classList.remove('d-none');
        if (messagesList) {
            messagesList.scrollTo(0, messagesList.scrollHeight);
        }
        try {
            const response = yield fetch('/Agent/Ask', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(request)
            });
            if (response.ok) {
                const data = yield response.json();
                const botMsgHtml = `
                <div class="text-message p-2 px-3 shadow-sm bot-message align-self-start bg-white text-dark border">
                    ${data.answer}
                </div>`;
                messagesList === null || messagesList === void 0 ? void 0 : messagesList.insertAdjacentHTML('beforeend', botMsgHtml);
            }
            else {
                const botMsgHtml = `
                <div class="text-message p-2 px-3 shadow-sm bot-message align-self-start bg-white text-danger border">
                    Przepraszam, wystąpił problem podczas komunikacji z serwerem.
                </div>`;
                messagesList === null || messagesList === void 0 ? void 0 : messagesList.insertAdjacentHTML('beforeend', botMsgHtml);
            }
        }
        catch (error) {
            console.error("Kernel error:", error);
            const botMsgHtml = `
            <div class="text-message p-2 px-3 shadow-sm bot-message align-self-start bg-white text-danger border">
                Przepraszam, wystąpił błąd sieci.
            </div>`;
            messagesList === null || messagesList === void 0 ? void 0 : messagesList.insertAdjacentHTML('beforeend', botMsgHtml);
        }
        finally {
            // Hide typing indicator
            typingIndicator === null || typingIndicator === void 0 ? void 0 : typingIndicator.classList.add('d-none');
            if (messagesList) {
                messagesList.scrollTo(0, messagesList.scrollHeight);
            }
        }
    });
    const messageInput = document.querySelector(".chat-input");
    const sendButton = document.getElementById("send-btn");
    const handleSend = (e) => {
        e === null || e === void 0 ? void 0 : e.stopPropagation();
        const text = messageInput === null || messageInput === void 0 ? void 0 : messageInput.value.trim();
        if (text && sessionId) {
            const payload = {
                message: text,
                sessionId: sessionId
            };
            sendMessage(payload);
            messageInput.value = '';
            // Reset height if resized
            messageInput.style.height = 'auto';
        }
    };
    sendButton === null || sendButton === void 0 ? void 0 : sendButton.addEventListener('click', handleSend);
    messageInput === null || messageInput === void 0 ? void 0 : messageInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend(e);
        }
    });
    // Auto-resize input textarea based on content
    messageInput === null || messageInput === void 0 ? void 0 : messageInput.addEventListener('input', () => {
        messageInput.style.height = 'auto';
        messageInput.style.height = (messageInput.scrollHeight) + 'px';
    });
})();
//# sourceMappingURL=agentChat.js.map