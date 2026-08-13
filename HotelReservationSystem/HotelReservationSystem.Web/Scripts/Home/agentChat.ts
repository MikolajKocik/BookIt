type Message = {
    message: string,
    isUser: boolean,
    isError?: boolean
}

interface UserRequest {
    message: string,
    sessionId: string
}

((): void => {
    const agentContainer = document.getElementById("agent-container");
    const agentButton: HTMLElement | null = document.getElementById("agent-button");
    const agentChatButton = document.getElementById("agent-chat-button");
    const closeButton = document.querySelector(".agent-header .btn-close-chat");
    const messagesList = document.querySelector(".text-messages-list");
    const typingIndicator = document.getElementById("chat-typing-indicator");

    let sessionId = sessionStorage.getItem("SESSION_ID");

    if (!sessionId) {
        sessionId = crypto.randomUUID();
        sessionStorage.setItem("SESSION_ID", sessionId);
    }

    agentButton?.addEventListener('click', (e) => {
        e.stopPropagation();
        agentContainer?.classList.toggle('d-none');
        if (messagesList) {
            messagesList.scrollTo(0, messagesList.scrollHeight);
        }
    });

    agentChatButton?.addEventListener('click', (e) => {
        e.stopPropagation();
        agentContainer?.classList.toggle('d-none');
        if (messagesList) {
            messagesList.scrollTo(0, messagesList.scrollHeight);
        }
    });

    closeButton?.addEventListener('click', () => {
        agentContainer?.classList.add('d-none');
        if (messagesList) {
            messagesList.innerHTML = '';
            sessionStorage.removeItem("SESSION_ID");
            sessionId = crypto.randomUUID();
            sessionStorage.setItem("SESSION_ID", sessionId);
        }
    });

    document.addEventListener('click', function (event) {
        if (!(event.target instanceof Node)) return;
        if (!agentContainer || !agentChatButton) return;

        const isOpen = !agentContainer.classList.contains('d-none');

        if (isOpen) {
            const clickedOutsideContainer = !agentContainer.contains(event.target);
            const clickedOutsideButton = !agentChatButton.contains(event.target);

            if (clickedOutsideContainer && clickedOutsideButton) {
                agentContainer.classList.add('d-none');
            }
        }
    });

    const addMessage = ({ message, isUser, isError = false }: Message) => {
        const element = document.createElement('div');
        element.className = isUser
            ? 'text-message p-2 px-3 shadow-sm user-message align-self-end text-white bg-primary'
            : `text-message p-2 px-3 shadow-sm bot-message align-self-start bg-white text-dark border ${isError
                ? 'bg-white text-danger border'
                : 'bg-white text-dark border'
            }`;

        element.textContent = message;
        messagesList?.appendChild(element);
    }

    const sendMessage = async (request: UserRequest) => {
        const token = (document.querySelector('input[name="__RequestVerificationToken"]') as HTMLInputElement)?.value;

        addMessage({
            message: request.message,
            isUser: true
        });

        if (messagesList) {
            messagesList.scrollTo(0, messagesList.scrollHeight);
        }

        // Show typing indicator
        typingIndicator?.classList.remove('d-none');
        if (messagesList) {
            messagesList.scrollTo(0, messagesList.scrollHeight);
        }

        try {
            const response = await fetch('/Agent/Ask', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(request)
            });

            if (response.ok) {
                const data = await response.json();
                addMessage({
                    message: data.answer,
                    isUser: false
                });
            } else {
                addMessage({
                    message: 'Przepraszam, wystąpił problem podczas komunikacji z serwerem.',
                    isUser: false,
                    isError: true
                });
            }
        } catch (error) {
            console.error("Kernel error:", error);
            addMessage({
                message: 'Przepraszam, wystąpił błąd sieci.',
                isUser: false,
                isError: true
            });
        } finally {
            // Hide typing indicator
            typingIndicator?.classList.add('d-none');
            if (messagesList) {
                messagesList.scrollTo(0, messagesList.scrollHeight);
            }
        }
    };

    const messageInput = document.querySelector(".chat-input") as HTMLTextAreaElement;
    const sendButton = document.getElementById("send-btn");

    const handleSend = (e?: Event) => {
        e?.stopPropagation();
        const text = messageInput?.value.trim();

        if (text && sessionId) {
            const payload: UserRequest = {
                message: text,
                sessionId: sessionId
            }
            sendMessage(payload);
            messageInput.value = '';
            // Reset height if resized
            messageInput.style.height = 'auto';
        }
    }

    sendButton?.addEventListener('click', handleSend);

    messageInput?.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend(e);
        }
    });

    // Auto-resize input textarea based on content
    messageInput?.addEventListener('input', () => {
        messageInput.style.height = 'auto';
        messageInput.style.height = (messageInput.scrollHeight) + 'px';
    });
})();