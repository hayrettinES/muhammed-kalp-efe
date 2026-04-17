// ── Theme Toggle ──
(function () {
    const KEY = 'eduverse-theme';

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(KEY, theme);

        // Toggle ikonunu güncelle
        const icons = document.querySelectorAll('.theme-toggle-icon');
        icons.forEach(el => {
            el.textContent = theme === 'light' ? '☀️' : '🌙';
        });

        // Toggle label güncelle
        const labels = document.querySelectorAll('.theme-toggle-label');
        labels.forEach(el => {
            el.textContent = theme === 'light' ? 'Açık' : 'Koyu';
        });
    }

    function getTheme() {
        return localStorage.getItem(KEY) || 'dark';
    }

    function toggleTheme() {
        const current = getTheme();
        applyTheme(current === 'dark' ? 'light' : 'dark');
    }

    // Sayfa yüklendiğinde tema uygula (FOUC önleme)
    applyTheme(getTheme());

    // Toggle butonlarına click bağla
    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('.theme-toggle').forEach(el => {
            el.addEventListener('click', toggleTheme);
        });

        // İlk yüklemede ikonları güncelle
        applyTheme(getTheme());
    });

    // Global erişim (Layout=null sayfalar için)
    window.EduVerseTheme = { toggle: toggleTheme, apply: applyTheme, get: getTheme };
})();
