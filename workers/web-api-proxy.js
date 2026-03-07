/**
 * Cloudflare Worker: Web API Proxy
 * cc.corplynk.com/api/* ve /hubs/* isteklerini cc-api.corplynk.com'a yonlendirir.
 * Boylece tarayicida API adresi gorunmez (same-origin proxy).
 *
 * Route: cc.corplynk.com/api/* ve cc.corplynk.com/hubs/*
 *
 * Deploy:
 *   npx wrangler deploy workers/web-api-proxy.js --name web-api-proxy
 *   Sonra Cloudflare Dashboard > Workers Routes:
 *     cc.corplynk.com/api/*  -> web-api-proxy
 *     cc.corplynk.com/hubs/* -> web-api-proxy
 */

const API_ORIGIN = 'https://cc-api.corplynk.com';

export default {
  async fetch(request) {
    const url = new URL(request.url);

    // Sadece /api/ ve /hubs/ yollarini proxy et
    if (!url.pathname.startsWith('/api/') && !url.pathname.startsWith('/hubs/')) {
      return fetch(request);
    }

    // Hedef URL olustur
    const targetUrl = `${API_ORIGIN}${url.pathname}${url.search}`;

    // WebSocket upgrade (SignalR icin)
    if (request.headers.get('Upgrade') === 'websocket') {
      return fetch(targetUrl, {
        headers: request.headers,
      });
    }

    // Normal HTTP istegi
    const modifiedRequest = new Request(targetUrl, {
      method: request.method,
      headers: request.headers,
      body: request.body,
      redirect: 'follow',
    });

    // Host header'i hedef sunucuya uygun yap
    modifiedRequest.headers.set('Host', 'cc-api.corplynk.com');

    const response = await fetch(modifiedRequest);

    // CORS header'larini ayarla (same-origin oldugu icin gerekli degil ama guvenlik icin)
    const modifiedResponse = new Response(response.body, response);
    modifiedResponse.headers.set('Access-Control-Allow-Origin', 'https://cc.corplynk.com');
    modifiedResponse.headers.set('Access-Control-Allow-Credentials', 'true');

    return modifiedResponse;
  },
};
