document.addEventListener('DOMContentLoaded', () => {
    const apiUrlInput = document.getElementById('apiUrl');
    const authTokenInput = document.getElementById('authToken');
    const saveBtn = document.getElementById('saveBtn');
    const quickDialInput = document.getElementById('quickDial');
    const dialBtn = document.getElementById('dialBtn');
    const statusEl = document.getElementById('status');

    // Mevcut ayarlari yukle
    chrome.runtime.sendMessage({ action: 'getSettings' }, (result) => {
        if (result) {
            apiUrlInput.value = result.apiUrl || '';
            authTokenInput.value = result.authToken || '';
        }
    });

    // Kaydet
    saveBtn.addEventListener('click', () => {
        chrome.runtime.sendMessage({
            action: 'saveSettings',
            apiUrl: apiUrlInput.value.trim(),
            authToken: authTokenInput.value.trim()
        }, (response) => {
            if (response && response.success) {
                showStatus('Ayarlar kaydedildi', 'success');
            } else {
                showStatus('Kaydetme hatasi', 'error');
            }
        });
    });

    // Hizli arama
    dialBtn.addEventListener('click', () => {
        const number = quickDialInput.value.replace(/[\s().-]/g, '');
        if (!number || number.length < 7) {
            showStatus('Gecerli bir numara girin', 'error');
            return;
        }

        const apiUrl = apiUrlInput.value.trim();
        const token = authTokenInput.value.trim();

        if (apiUrl && token) {
            fetch(`${apiUrl}/api/calls/dial`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({ destination: number })
            }).then(r => {
                if (r.ok) showStatus(`Araniyor: ${number}`, 'success');
                else showStatus('Arama baslatilamadi', 'error');
            }).catch(() => {
                // Fallback: callto://
                window.open(`callto:${number}`);
            });
        } else {
            window.open(`callto:${number}`);
        }
    });

    // Enter tusu ile arama
    quickDialInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') dialBtn.click();
    });

    function showStatus(msg, type) {
        statusEl.textContent = msg;
        statusEl.className = `status status-${type}`;
        setTimeout(() => { statusEl.className = 'status'; }, 2000);
    }
});
