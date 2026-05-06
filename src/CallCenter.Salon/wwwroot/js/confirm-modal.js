// Shared Bootstrap confirm/prompt modal helper
// Usage: confirmModal('Title', 'Message', function() { /* on confirm */ });
// For input: confirmModal('Title', 'Message', function(inputValue) { }, { input: true, inputLabel: 'Label' });
(function () {
    var modalId = 'shared-confirm-modal';
    var _onConfirm = null;
    var _hasInput = false;
    function modalT(key, fallback) {
        return (window.salonT || function (k, f) { return f || k; })(key, fallback);
    }

    function ensureModal() {
        if (document.getElementById(modalId)) return;
        var html =
            '<div class="modal fade" id="' + modalId + '" tabindex="-1" aria-hidden="true">' +
            '  <div class="modal-dialog modal-dialog-centered">' +
            '    <div class="modal-content">' +
            '      <div class="modal-header py-2">' +
            '        <h6 class="modal-title" id="' + modalId + '-title"></h6>' +
            '        <button type="button" class="btn-close btn-close-sm" data-bs-dismiss="modal" aria-label="' + modalT('salon.common.close', 'Kapat') + '"></button>' +
            '      </div>' +
            '      <div class="modal-body">' +
            '        <p id="' + modalId + '-message" class="mb-2"></p>' +
            '        <div id="' + modalId + '-input-wrap" class="d-none">' +
            '          <label id="' + modalId + '-input-label" class="form-label small"></label>' +
            '          <input type="text" id="' + modalId + '-input" class="form-control form-control-sm" />' +
            '        </div>' +
            '      </div>' +
            '      <div class="modal-footer py-1">' +
            '        <button type="button" class="btn btn-sm btn-secondary" data-bs-dismiss="modal">' + modalT('salon.common.cancel_action', 'Vazgeç') + '</button>' +
            '        <button type="button" class="btn btn-sm btn-primary" id="' + modalId + '-confirm">' + modalT('salon.common.btn.confirm', 'Onayla') + '</button>' +
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
            if (typeof _onConfirm === 'function') {
                if (_hasInput) {
                    _onConfirm(document.getElementById(modalId + '-input').value);
                } else {
                    _onConfirm();
                }
            }
            _onConfirm = null;
        });
    }

    window.confirmModal = function (title, message, onConfirm, options) {
        ensureModal();
        options = options || {};
        _onConfirm = onConfirm;
        _hasInput = !!options.input;

        document.getElementById(modalId + '-title').textContent = title;
        document.getElementById(modalId + '-message').textContent = message;

        var inputWrap = document.getElementById(modalId + '-input-wrap');
        var inputEl = document.getElementById(modalId + '-input');
        var inputLabel = document.getElementById(modalId + '-input-label');

        if (_hasInput) {
            inputWrap.classList.remove('d-none');
            inputLabel.textContent = options.inputLabel || '';
            inputEl.value = options.inputDefault || '';
        } else {
            inputWrap.classList.add('d-none');
            inputEl.value = '';
        }

        var confirmBtn = document.getElementById(modalId + '-confirm');
        confirmBtn.className = 'btn btn-sm ' + (options.confirmClass || 'btn-primary');
        confirmBtn.textContent = options.confirmText || 'Onayla';

        var modalEl = document.getElementById(modalId);
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    };
})();
