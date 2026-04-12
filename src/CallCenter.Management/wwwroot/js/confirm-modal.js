// Shared confirm/prompt modal helper for Management panel
// Replaces native confirm() and prompt() with Bootstrap modals
(function () {
    var _modalEl, _modal, _titleEl, _messageEl, _confirmBtn, _cancelBtn, _inputWrap, _inputField, _inputLabel;
    var _onConfirm = null;

    function ensureModal() {
        if (_modalEl) return;
        _modalEl = document.createElement('div');
        _modalEl.className = 'modal fade';
        _modalEl.id = 'confirmModalGlobal';
        _modalEl.tabIndex = -1;
        _modalEl.innerHTML =
            '<div class="modal-dialog modal-sm modal-dialog-centered">' +
            '<div class="modal-content">' +
            '<div class="modal-header py-2">' +
            '<h6 class="modal-title" id="confirmModalTitle">Onay</h6>' +
            '<button type="button" class="btn-close btn-close-sm" data-bs-dismiss="modal"></button>' +
            '</div>' +
            '<div class="modal-body">' +
            '<p id="confirmModalMessage" class="mb-2"></p>' +
            '<div id="confirmModalInputWrap" class="d-none">' +
            '<label class="form-label small" id="confirmModalInputLabel"></label>' +
            '<input type="text" class="form-control form-control-sm" id="confirmModalInput" />' +
            '</div>' +
            '</div>' +
            '<div class="modal-footer py-2">' +
            '<button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Iptal</button>' +
            '<button type="button" class="btn btn-primary btn-sm" id="confirmModalConfirmBtn">Tamam</button>' +
            '</div>' +
            '</div></div>';
        document.body.appendChild(_modalEl);
        _modal = new bootstrap.Modal(_modalEl);
        _titleEl = document.getElementById('confirmModalTitle');
        _messageEl = document.getElementById('confirmModalMessage');
        _confirmBtn = document.getElementById('confirmModalConfirmBtn');
        _cancelBtn = _modalEl.querySelector('[data-bs-dismiss="modal"]');
        _inputWrap = document.getElementById('confirmModalInputWrap');
        _inputField = document.getElementById('confirmModalInput');
        _inputLabel = document.getElementById('confirmModalInputLabel');

        _confirmBtn.addEventListener('click', function () {
            _modal.hide();
            if (_onConfirm) {
                var inputVal = _inputWrap.classList.contains('d-none') ? true : _inputField.value;
                _onConfirm(inputVal);
            }
            _onConfirm = null;
        });

        _modalEl.addEventListener('hidden.bs.modal', function () {
            _onConfirm = null;
        });
    }

    /**
     * confirmModal - Bootstrap modal replacement for confirm/prompt
     * @param {string} title - Modal title
     * @param {string} message - Modal body message
     * @param {function} onConfirm - Callback on confirm. For input mode receives the input value, for confirm mode receives true.
     * @param {object} [options] - Optional settings
     * @param {boolean} [options.input] - Show text input (replaces prompt)
     * @param {string} [options.inputLabel] - Label for the input field
     * @param {string} [options.inputPlaceholder] - Placeholder for input
     * @param {string} [options.inputValue] - Default value for input
     * @param {string} [options.confirmText] - Text for confirm button (default: Tamam)
     * @param {string} [options.confirmClass] - CSS class for confirm button (default: btn-primary)
     */
    window.confirmModal = function (title, message, onConfirm, options) {
        ensureModal();
        options = options || {};
        _titleEl.textContent = title || 'Onay';
        _messageEl.textContent = message || '';
        _onConfirm = onConfirm || null;

        if (options.input) {
            _inputWrap.classList.remove('d-none');
            _inputLabel.textContent = options.inputLabel || '';
            _inputField.placeholder = options.inputPlaceholder || '';
            _inputField.value = options.inputValue || '';
        } else {
            _inputWrap.classList.add('d-none');
            _inputField.value = '';
        }

        _confirmBtn.textContent = options.confirmText || 'Tamam';
        _confirmBtn.className = 'btn btn-sm ' + (options.confirmClass || 'btn-primary');

        _modal.show();
    };
})();
