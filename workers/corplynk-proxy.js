addEventListener("fetch", event => {
  event.respondWith(handleRequest(event.request));
});

async function handleRequest(request) {
  try {
    const ORIGIN = "https://callcenter-landing-ephcbyzqeq-ew.a.run.app";
    const url = new URL(request.url);
    const targetUrl = ORIGIN + url.pathname + url.search;
    const newHeaders = new Headers(request.headers);
    newHeaders.set("Host", "callcenter-landing-ephcbyzqeq-ew.a.run.app");
    newHeaders.delete("cf-connecting-ip");
    newHeaders.delete("cf-ray");
    newHeaders.delete("cf-visitor");
    newHeaders.delete("cf-worker");
    const init = { method: request.method, headers: newHeaders, redirect: "manual" };
    if (request.method !== "GET" && request.method !== "HEAD") init.body = request.body;
    const response = await fetch(targetUrl, init);
    if ([301, 302, 303, 307, 308].includes(response.status)) {
      const location = response.headers.get("Location");
      if (location) {
        const newLocation = location.replace(ORIGIN, url.origin);
        const redirectResponse = new Response(response.body, response);
        redirectResponse.headers.set("Location", newLocation);
        redirectResponse.headers.set("X-Powered-By", "Corplynk");
        return redirectResponse;
      }
    }
    const modifiedResponse = new Response(response.body, response);
    modifiedResponse.headers.set("X-Powered-By", "Corplynk");
    return modifiedResponse;
  } catch (err) {
    return new Response("Service temporarily unavailable. Error: " + err.message, { status: 502, headers: { "Content-Type": "text/plain" } });
  }
}
