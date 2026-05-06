(() => {
  let aktivSohbetId = 0;

  function escapeHtml(text) {
    return (text ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  function parseMarkdown(text) {
    let html = escapeHtml(text);
    html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/\*(.*?)\*/g, '<em>$1</em>');
    html = html.replace(/\[ID:\s*(\d+)\]\s*&quot;(.*?)&quot;/g, '<span class="course-badge" data-id="$1">🎓 $2</span>');
    html = html.replace(/\n/g, '<br/>');
    return html;
  }

  function renderMessages(messages) {
    const messagesEl = document.getElementById("assistant-messages");
    if (!messagesEl) return;
    messagesEl.innerHTML = "";
    for (const m of messages) {
      const bubble = document.createElement("div");
      bubble.className = `assistant-bubble ${m.role === "user" ? "user" : "assistant"}`;
      bubble.innerHTML = parseMarkdown(m.content);
      messagesEl.appendChild(bubble);
    }
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  // Sohbet listesini yükle
  async function sohbetListesiYukle() {
    try {
      const res = await fetch("/Assistant/Sohbetler");
      if (!res.ok) return;
      const sohbetler = await res.json();
      const listEl = document.getElementById("assistant-sidebar-list");
      if (!listEl) return;

      if (sohbetler.length === 0) {
        listEl.innerHTML = '<p style="color:var(--assistant-text2);text-align:center;padding:2rem;font-size:13px;">Henüz sohbet yok.<br>Yeni sohbet başlatın!</p>';
        return;
      }

      listEl.innerHTML = "";
      for (const s of sohbetler) {
        const item = document.createElement("div");
        item.className = `sohbet-item${s.id === aktivSohbetId ? " active" : ""}`;
        item.innerHTML = `
          <div class="sohbet-item-baslik" title="${escapeHtml(s.baslik)}">${escapeHtml(s.baslik)}</div>
          <span class="sohbet-item-tarih">${(s.tarih || "").substring(0, 10)}</span>
          <button class="sohbet-item-sil" title="Sil" data-id="${s.id}">🗑</button>
        `;
        // Sohbete tıklama
        item.querySelector(".sohbet-item-baslik").addEventListener("click", () => sohbetiYukle(s.id));
        // Silme
        item.querySelector(".sohbet-item-sil").addEventListener("click", (e) => {
          e.stopPropagation();
          sohbetSil(s.id);
        });
        listEl.appendChild(item);
      }
    } catch { }
  }

  // Belirli bir sohbetin mesajlarını yükle
  async function sohbetiYukle(sohbetId) {
    aktivSohbetId = sohbetId;
    try {
      const res = await fetch(`/Assistant/Mesajlar/${sohbetId}`);
      if (!res.ok) return;
      const mesajlar = await res.json();
      renderMessages(mesajlar);
    } catch { }
    sidebarKapat();
    sohbetListesiYukle(); // aktif olanı güncelle
  }

  // Yeni sohbet oluştur
  async function yeniSohbet() {
    try {
      const res = await fetch("/Assistant/YeniSohbet", { method: "POST" });
      if (!res.ok) return;
      const data = await res.json();
      aktivSohbetId = data.sohbetId;
      renderMessages([{ role: "assistant", content: "Merhaba! EduVerse ile ilgili sorularını Türkçe yanıtlayabilirim. Ne yapmak istiyorsun?" }]);
      sohbetListesiYukle();
    } catch { }
    sidebarKapat();
  }

  // Sohbet sil
  async function sohbetSil(sohbetId) {
    try {
      await fetch(`/Assistant/SohbetSil/${sohbetId}`, { method: "POST" });
      if (aktivSohbetId === sohbetId) {
        aktivSohbetId = 0;
        renderMessages([]);
      }
      sohbetListesiYukle();
    } catch { }
  }

  // Mesaj gönder
  async function sendMessage() {
    const input = document.getElementById("assistant-input");
    const sendBtn = document.getElementById("assistant-send");
    const errorEl = document.getElementById("assistant-error");

    const message = (input?.value ?? "").trim();
    if (!message) return;

    errorEl?.classList.remove("is-visible");
    if (errorEl) errorEl.style.display = "none";

    // Mevcut mesajları al
    const messagesEl = document.getElementById("assistant-messages");
    const mevcutMesajlar = [];
    if (messagesEl) {
      messagesEl.querySelectorAll(".assistant-bubble").forEach(b => {
        mevcutMesajlar.push({
          role: b.classList.contains("user") ? "user" : "assistant",
          content: b.textContent || ""
        });
      });
    }

    // UI güncelle
    const userMsg = { role: "user", content: message };
    const placeholder = { role: "assistant", content: "Yazıyor..." };
    renderMessages([...mevcutMesajlar, userMsg, placeholder]);

    if (sendBtn) sendBtn.disabled = true;
    if (input) input.value = "";

    try {
      const res = await fetch("/Assistant/Chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          message,
          sohbetId: aktivSohbetId,
          history: []
        })
      });

      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.error || `Hata: ${res.status}`);
      }

      const data = await res.json();
      const reply = (data?.reply ?? "").trim();
      if (!reply) throw new Error("Boş yanıt döndü.");

      // Sohbet ID güncelle
      if (data.sohbetId) aktivSohbetId = data.sohbetId;

      renderMessages([...mevcutMesajlar, userMsg, { role: "assistant", content: reply }]);
      sohbetListesiYukle();
    } catch (e) {
      const msg = (e && e.message) ? e.message : "Asistan cevap veremedi.";
      if (errorEl) {
        errorEl.textContent = msg;
        errorEl.classList.add("is-visible");
        errorEl.style.display = "block";
      }
      renderMessages([...mevcutMesajlar, userMsg, { role: "assistant", content: "Üzgünüm, şu an yanıt üretemiyorum." }]);
    } finally {
      if (sendBtn) sendBtn.disabled = false;
      document.getElementById("assistant-input")?.focus?.();
    }
  }

  // Sidebar aç/kapa
  function sidebarAc() {
    const sb = document.getElementById("assistant-sidebar");
    if (sb) sb.classList.add("is-open");
    sohbetListesiYukle();
  }

  function sidebarKapat() {
    const sb = document.getElementById("assistant-sidebar");
    if (sb) sb.classList.remove("is-open");
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
    const historyBtn = document.getElementById("assistant-history-btn");
    const newChatBtn = document.getElementById("assistant-new-chat");
    const sidebarCloseBtn = document.getElementById("assistant-sidebar-close");

    // Panel açıldığında son sohbeti yükle veya yeni oluştur
    toggleBtn?.addEventListener("click", async () => {
      togglePanel(true);
      if (aktivSohbetId === 0) {
        // Son sohbet varsa onu yükle, yoksa yeni oluştur
        try {
          const res = await fetch("/Assistant/Sohbetler");
          if (res.ok) {
            const sohbetler = await res.json();
            if (sohbetler.length > 0) {
              await sohbetiYukle(sohbetler[0].id);
            } else {
              await yeniSohbet();
            }
          }
        } catch {
          renderMessages([{ role: "assistant", content: "Merhaba! Sorunu yaz, yardımcı olayım." }]);
        }
      }
    });

    closeBtn?.addEventListener("click", () => { togglePanel(false); sidebarKapat(); });
    historyBtn?.addEventListener("click", () => sidebarAc());
    newChatBtn?.addEventListener("click", () => yeniSohbet());
    sidebarCloseBtn?.addEventListener("click", () => sidebarKapat());

    sendBtn?.addEventListener("click", () => sendMessage());

    document.getElementById("assistant-input")?.addEventListener("keydown", (e) => {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
      }
    });

    // Başlangıç kapalı
    togglePanel(false);
  });
})();
