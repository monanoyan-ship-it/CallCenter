/**
 * Click-to-Dial Content Script
 * Web sayfalarindaki telefon numaralarini otomatik tespit eder
 * ve tiklanabilir hale getirir.
 */

(function () {
    'use strict';

    // Telefon numarasi regex desenleri
    // Turkiye: +90 5XX XXX XX XX, 0 5XX XXX XX XX, (212) XXX XX XX
    // Uluslararasi: +XX XXXXXXXX
    const PHONE_PATTERNS = [
        /\+?\d{1,3}[\s.-]?\(?\d{2,4}\)?[\s.-]?\d{3}[\s.-]?\d{2}[\s.-]?\d{2}/g,  // Genel format
        /\+90[\s.-]?\d{3}[\s.-]?\d{3}[\s.-]?\d{2}[\s.-]?\d{2}/g,                  // TR mobil
        /0\s?\d{3}[\s.-]?\d{3}[\s.-]?\d{2}[\s.-]?\d{2}/g,                          // TR yerel
        /\(\d{3,4}\)\s?\d{3}[\s.-]?\d{2}[\s.-]?\d{2}/g                             // (alan kodu) format
    ];

    // Daha once islenmis node'lari izle
    const processedNodes = new WeakSet();

    // Call Center API URL (ayarlardan okunur)
    let apiBaseUrl = '';
    let authToken = '';

    // Ayarlari yukle
    chrome.storage.sync.get(['apiUrl', 'authToken'], (result) => {
        apiBaseUrl = result.apiUrl || '';
        authToken = result.authToken || '';
    });

    /**
     * Metin icindeki telefon numaralarini bulur ve tiklanabilir yapar.
     */
    function processTextNode(textNode) {
        if (processedNodes.has(textNode)) return;
        if (!textNode.textContent || textNode.textContent.trim().length < 7) return;

        // Atlanacak ebeveyn elementler
        const parent = textNode.parentNode;
        if (!parent || parent.nodeType !== 1) return;

        const tagName = parent.tagName.toLowerCase();
        if (['script', 'style', 'textarea', 'input', 'select', 'code', 'pre', 'a'].includes(tagName)) return;
        if (parent.classList.contains('ctd-link')) return;
        if (parent.isContentEditable) return;

        const text = textNode.textContent;
        let hasMatch = false;

        for (const pattern of PHONE_PATTERNS) {
            pattern.lastIndex = 0;
            if (pattern.test(text)) {
                hasMatch = true;
                break;
            }
        }

        if (!hasMatch) return;

        processedNodes.add(textNode);

        // Numara iceren metni HTML'e donustur
        let html = text;
        for (const pattern of PHONE_PATTERNS) {
            pattern.lastIndex = 0;
            html = html.replace(pattern, (match) => {
                const cleanNumber = match.replace(/[\s().-]/g, '');
                return `<span class="ctd-link" data-number="${cleanNumber}" title="Tikla ve Ara: ${match}">${match}</span>`;
            });
        }

        if (html === text) return; // Degisiklik yoksa atla

        const wrapper = document.createElement('span');
        wrapper.innerHTML = html;
        parent.replaceChild(wrapper, textNode);
    }

    /**
     * DOM agacini traverse eder ve metin node'larini isler.
     */
    function walkTree(node) {
        if (node.nodeType === Node.TEXT_NODE) {
            processTextNode(node);
            return;
        }

        if (node.nodeType !== Node.ELEMENT_NODE) return;

        const children = Array.from(node.childNodes);
        for (const child of children) {
            walkTree(child);
        }
    }

    /**
     * Click-to-dial tiklandiginda arama baslatir.
     */
    function handleClick(e) {
        const target = e.target.closest('.ctd-link');
        if (!target) return;

        e.preventDefault();
        e.stopPropagation();

        const number = target.dataset.number;
        if (!number) return;

        // Kullaniciya onay sor
        if (!confirm(`${number} numarasini aramak istiyor musunuz?`)) return;

        // Arama baslatma yontemleri (oncelik sirasina gore):
        // 1. callto:// protocol (Windows client'a yonlendirir)
        // 2. API call (dogrudan sunucuya)
        // 3. Web app'e yonlendirme

        if (apiBaseUrl && authToken) {
            // API call ile arama baslat
            fetch(`${apiBaseUrl}/api/calls/dial`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authToken}`
                },
                body: JSON.stringify({ destination: number })
            }).then(response => {
                if (response.ok) {
                    showNotification(`Araniyor: ${number}`, 'success');
                } else {
                    showNotification('Arama baslatilamadi', 'error');
                }
            }).catch(() => {
                // API basarisizsa callto:// dene
                window.location.href = `callto:${number}`;
            });
        } else {
            // callto:// protocol ile Windows client'a yonlendir
            window.location.href = `callto:${number}`;
        }
    }

    /**
     * Ekranda kisa bildirim gosterir.
     */
    function showNotification(message, type) {
        const notification = document.createElement('div');
        notification.className = `ctd-notification ctd-notification-${type}`;
        notification.textContent = message;
        document.body.appendChild(notification);

        setTimeout(() => {
            notification.classList.add('ctd-notification-fadeout');
            setTimeout(() => notification.remove(), 300);
        }, 2000);
    }

    // ── Sayfa yuklendiginde calistir ──
    walkTree(document.body);
    document.addEventListener('click', handleClick);

    // ── Dinamik eklenen icerik icin MutationObserver ──
    const observer = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (node.nodeType === Node.ELEMENT_NODE) {
                    walkTree(node);
                }
            }
        }
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
})();
