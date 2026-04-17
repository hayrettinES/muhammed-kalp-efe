(() => {
  const STORAGE_KEY = "eduverse_assistant_history_v1";
  const MAX_HISTORY = 18;

  function escapeHtml(text) {
    return (text ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  function loadHistory() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      if (!Array.isArray(parsed)) return [];
      return parsed
        .filter(x => x && typeof x.content === "string" && typeof x.role === "string")
        .slice(-MAX_HISTORY);
    } catch {
      return [];
    }
  }

  function saveHistory(history) {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(history.slice(-MAX_HISTORY)));
    } catch {
      // localStorage kapalıysa sessiz geç
    }
  }

  function renderMessages(messages) {
    const messagesEl = document.getElementById("assistant-messages");
    if (!messagesEl) return;

    messagesEl.innerHTML = "";
    for (const m of messages) {
      const bubble = document.createElement("div");
      bubble.className = `assistant-bubble ${m.role === "user" ? "user" : "assistant"}`;
      bubble.innerHTML = escapeHtml(m.content).replaceAll("\n", "<br/>");
      messagesEl.appendChild(bubble);
    }

    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  async function sendMessage() {
    const input = document.getElementById("assistant-input");
    const sendBtn = document.getElementById("assistant-send");
    const errorEl = document.getElementById("assistant-error");

    const message = (input?.value ?? "").trim();
    if (!message) return;

    errorEl?.classList.remove("is-visible");
    if (errorEl) errorEl.style.display = "none";

    const history = loadHistory();

    // UI: user mesajını ve "yazıyor..." placeholder'ı ekle
    const userMsg = { role: "user", content: message };
    const placeholder = { role: "assistant", content: "Yazıyor..." };
    const uiMessages = [...history, userMsg, placeholder];
    renderMessages(uiMessages);

    if (sendBtn) sendBtn.disabled = true;
    if (input) input.value = "";

    try {
      const res = await fetch("/Assistant/Chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          message,
          history: history // server system prompt ekliyor; burada sadece user/assistant geçmişi
        })
      });

      if (!res.ok) {
        const err = await res.json().catch(() => null);
        const msg = err?.error || `Hata: ${res.status}`;
        throw new Error(msg);
      }

      const data = await res.json();
      const reply = (data?.reply ?? "").trim();
      if (!reply) throw new Error("Boş yanıt döndü.");

      const newHistory = [...history, userMsg, { role: "assistant", content: reply }];
      saveHistory(newHistory);
      renderMessages(newHistory);
    } catch (e) {
      const msg = (e && e.message) ? e.message : "Asistan cevap veremedi.";
      if (errorEl) {
        errorEl.textContent = msg;
        errorEl.classList.add("is-visible");
        errorEl.style.display = "block";
      }

      // history'ye boş placeholder basmayalım
      const newUiMessages = [...history, userMsg, { role: "assistant", content: "Üzgünüm, şu an yanıt üretemiyorum." }];
      renderMessages(newUiMessages);
    } finally {
      if (sendBtn) sendBtn.disabled = false;
      document.getElementById("assistant-input")?.focus?.();
    }
  }

  function togglePanel(isOpen) {
    const panel = document.getElementById("assistant-panel");
    if (!panel) return;

    if (isOpen) panel.classList.add("is-open");
    else panel.classList.remove("is-open");
    panel.setAttribute("aria-hidden", (!isOpen).toString());
  }

  document.addEventListener("DOMContentLoaded", () => {
    const toggleBtn = document.getElementById("assistant-toggle");
    const closeBtn = document.getElementById("assistant-close");
    const sendBtn = document.getElementById("assistant-send");

    const existing = loadHistory();
    if (existing.length === 0) {
      // İlk kullanımda kullanıcı görmesi için kısa bir yönlendirme
      const starter = [{ role: "assistant", content: "Merhaba! EduVerse ile ilgili sorularını Türkçe yanıtlayabilirim. Ne yapmak istiyorsun?" }];
      saveHistory(starter);
      renderMessages(starter);
    } else {
      renderMessages(existing);
    }

    // Başlangıç kapalı
    togglePanel(false);

    toggleBtn?.addEventListener("click", () => togglePanel(true));
    closeBtn?.addEventListener("click", () => togglePanel(false));

    sendBtn?.addEventListener("click", () => sendMessage());

    document.getElementById("assistant-input")?.addEventListener("keydown", (e) => {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
      }
    });
  });
})();

