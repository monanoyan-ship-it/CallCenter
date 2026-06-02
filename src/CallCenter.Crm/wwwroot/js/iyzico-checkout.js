(function () {
    function runInlineScript(code) {
        if (!code) return;
        (0, window.eval)(code);
    }

    window.renderIyzicoCheckoutHtml = function (container, html) {
        if (!container) return;

        try {
            delete window.iyziInit;
        } catch (e) {
            window.iyziInit = undefined;
        }

        container.innerHTML = html || '';
        container.style.minHeight = html ? '560px' : '';

        var inlineScripts = [];
        var scripts = Array.prototype.slice.call(container.querySelectorAll('script'));

        scripts.forEach(function (oldScript) {
            var script = document.createElement('script');
            Array.prototype.slice.call(oldScript.attributes).forEach(function (attr) {
                script.setAttribute(attr.name, attr.value);
            });

            if (!oldScript.src) {
                var code = oldScript.text || oldScript.textContent || '';
                inlineScripts.push(code);
                script.text = code;
            }

            if (oldScript.parentNode) oldScript.parentNode.replaceChild(script, oldScript);
        });

        window.setTimeout(function () {
            if (container.querySelector('iframe') || typeof window.iyziInit !== 'undefined') return;

            inlineScripts.forEach(function (code) {
                runInlineScript(code);
            });
        }, 0);
    };
})();
