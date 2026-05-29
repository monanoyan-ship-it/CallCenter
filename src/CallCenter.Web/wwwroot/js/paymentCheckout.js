window.corplynkPayments = window.corplynkPayments || {};

window.corplynkPayments.injectCheckoutHtml = function (containerId, html) {
    var container = document.getElementById(containerId);
    if (!container) return;

    try {
        container.innerHTML = html || '';
        var scripts = Array.prototype.slice.call(container.querySelectorAll('script'));
        scripts.forEach(function (oldScript) {
            var script = document.createElement('script');
            for (var i = 0; i < oldScript.attributes.length; i++) {
                var attr = oldScript.attributes[i];
                script.setAttribute(attr.name, attr.value);
            }
            if (!oldScript.src && oldScript.textContent) {
                script.textContent = oldScript.textContent;
            }
            oldScript.parentNode.replaceChild(script, oldScript);
        });
    } catch (e) {
        console.error('iyzico checkout inject', e);
        container.innerHTML = '<p class="text-danger small mb-0">Odeme formu yuklenirken hata olustu. Sayfayi yenileyip tekrar deneyin.</p>';
    }
};
