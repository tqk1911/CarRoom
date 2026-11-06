document.addEventListener("DOMContentLoaded", function () {
    const bubble = document.getElementById("chat-bubble");
    const box = document.getElementById("chat-box");
    const input = document.getElementById("chat-input");
    const messages = document.getElementById("chat-messages");
    if (!bubble) return;

    bubble.addEventListener("click", function () {
        box.classList.toggle("d-none");
        if (!box.classList.contains("d-none") && messages.children.length === 0) {
            addMessage("bot", "Xin chào! Tôi có thể tư vấn xe nào cho bạn hôm nay?");
        }
    });
    document.getElementById("chat-close").addEventListener("click", function () {
        box.classList.add("d-none");
    });
    document.getElementById("chat-send").addEventListener("click", send);
    input.addEventListener("keypress", function (e) { if (e.key === "Enter") send(); });

    async function send() {
        const text = input.value.trim();
        if (!text) return;
        addMessage("user", text);
        input.value = "";
        const waiting = addMessage("bot", "Đang trả lời...");
        try {
            const res = await fetch("/api/chat", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ message: text })
            });
            const data = await res.json();
            waiting.textContent = data.reply;
        } catch {
            waiting.textContent = "Không kết nối được, bạn thử lại sau nhé.";
        }
        messages.scrollTop = messages.scrollHeight;
    }

    function addMessage(who, text) {
        const el = document.createElement("div");
        el.className = "px-3 py-2 rounded-3 " +
            (who === "user" ? "bg-primary text-white align-self-end" : "bg-white border align-self-start");
        el.style.maxWidth = "80%";
        el.textContent = text;
        messages.appendChild(el);
        messages.scrollTop = messages.scrollHeight;
        return el;
    }
});