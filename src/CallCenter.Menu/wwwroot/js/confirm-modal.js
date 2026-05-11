// Shared Bootstrap confirm/prompt modal helper.
(function () {
    var modalId = 'menu-confirm-modal';
    var onConfirm = null;
    var hasInput = false;

    function menuT(key, fallback) {
        return (window.menuT || function (_, f) { return f; })(key, fallback);
    }

    function ensureModal() {
        if (document.getElementById(modalId)) return;

        var html =
            '<div class="modal fade" id="' + modalId + '" tabindex="-1" aria-hidden="true">' +
            '  <div class="modal-dialog modal-dialog-centered">' +
            '    <div class="modal-content">' +
            '      <div class="modal-header py-2">' +
            '        <h6 class="modal-title" id="' + modalId + '-title"></h6>' +
            '        <button type="button" class="btn-close btn-close-sm" data-bs-dismiss="modal" aria-label="' + menuT('menu.common.close', 'Kapat') + '"></button>' +
            '      </div>' +
            '      <div class="modal-body">' +
            '        <p id="' + modalId + '-message" class="mb-2"></p>' +
            '        <div id="' + modalId + '-input-wrap" class="d-none">' +
            '          <label id="' + modalId + '-input-label" class="form-label small"></label>' +
            '          <input type="text" id="' + modalId + '-input" class="form-control form-control-sm" />' +
            '        </div>' +
            '      </div>' +
            '      <div class="modal-footer py-1">' +
            '        <button type="button" class="btn btn-sm btn-secondary" data-bs-dismiss="modal">' + menuT('menu.common.cancel', 'Vazgec') + '</button>' +
            '        <button type="button" class="btn btn-sm btn-primary" id="' + modalId + '-confirm">' + menuT('menu.common.confirm', 'Onayla') + '</button>' +
            '      </div>' +
            '    </div>' +
            '  </div>' +
            '</div>';

        var container = document.createElement('div');
        container.innerHTML = html;
        document.body.appendChild(container.firstChild);

        document.getElementById(modalId + '-confirm').addEventListener('click', function () {
            var modal = bootstrap.Modal.getInstance(document.getElementById(modalId));
            if (modal) modal.hide();

            if (typeof onConfirm === 'function') {
                onConfirm(hasInput ? document.getElementById(modalId + '-input').value : undefined);
            }
            onConfirm = null;
        });
    }

    window.confirmModal = function (title, message, callback, options) {
        ensureModal();
        options = options || {};
        onConfirm = callback;
        hasInput = !!options.input;

        document.getElementById(modalId + '-title').textContent = title;
        document.getElementById(modalId + '-message').textContent = message;

        var inputWrap = document.getElementById(modalId + '-input-wrap');
        var inputEl = document.getElementById(modalId + '-input');
        var inputLabel = document.getElementById(modalId + '-input-label');

        if (hasInput) {
            inputWrap.classList.remove('d-none');
            inputLabel.textContent = options.inputLabel || '';
            inputEl.value = options.inputDefault || '';
        } else {
            inputWrap.classList.add('d-none');
            inputEl.value = '';
        }

        var confirmBtn = document.getElementById(modalId + '-confirm');
        confirmBtn.className = 'btn btn-sm ' + (options.confirmClass || 'btn-primary');
        confirmBtn.textContent = options.confirmText || menuT('menu.common.confirm', 'Onayla');

        bootstrap.Modal.getOrCreateInstance(document.getElementById(modalId)).show();
    };
})();
