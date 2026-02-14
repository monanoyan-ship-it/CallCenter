/**
 * Click-to-Dial Background Service Worker
 * Sag-tik menu ve global event yonetimi.
 */

// Sag-tik context menu olustur
chrome.runtime.onInstalled.addListener(() => {
    chrome.contextMenus.create({
        id: 'ctd-call-selection',
        title: 'Secili numarayi ara: %s',
        contexts: ['selection']
    });
});

// Context menu tiklandi
chrome.contextMenus.onClicked.addListener((info, tab) => {
    if (info.menuItemId === 'ctd-call-selection' && info.selectionText) {
        const number = info.selectionText.replace(/[\s().-]/g, '');

        // Numara gecerli mi?
        if (!/^\+?\d{7,15}$/.test(number)) {
            return;
        }

        // Content script'e mesaj gonder
        chrome.tabs.sendMessage(tab.id, {
            action: 'dial',
            number: number
        });
    }
});

// Extension popup'tan gelen mesajlar
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === 'saveSettings') {
        chrome.storage.sync.set({
            apiUrl: message.apiUrl,
            authToken: message.authToken
        }, () => {
            sendResponse({ success: true });
        });
        return true; // Async response
    }

    if (message.action === 'getSettings') {
        chrome.storage.sync.get(['apiUrl', 'authToken'], (result) => {
            sendResponse(result);
        });
        return true;
    }
});
