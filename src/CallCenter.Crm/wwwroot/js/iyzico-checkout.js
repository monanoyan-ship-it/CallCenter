(function () {
    window.renderIyzicoCheckoutHtml = function (container, html) {
        if (!container) return;
        container.innerHTML = html || '';

        var scripts = Array.prototype.slice.call(container.querySelectorAll('script'));
        scripts.forEach(function (oldScript) {
            var script = document.createElement('script');
            Array.prototype.slice.call(oldScript.attributes).forEach(function (attr) {
                script.setAttribute(attr.name, attr.value);
            });
            if (!oldScript.src) script.text = oldScript.textContent || '';
            oldScript.parentNode.replaceChild(script, oldScript);
        });
    };
})();
