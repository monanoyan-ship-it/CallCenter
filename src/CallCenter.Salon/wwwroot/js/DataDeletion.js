(function () {
    var form = document.getElementById('dataDeletionForm');
    if (!form) return;

    var status = document.getElementById('dataDeletionStatus');
    var submit = document.getElementById('dataDeletionSubmit');
    var spinner = submit ? submit.querySelector('.spinner-border') : null;
    var icon = submit ? submit.querySelector('.bi-send') : null;

    function setBusy(isBusy) {
        if (!submit) return;
        submit.disabled = isBusy;
        if (spinner) spinner.classList.toggle('d-none', !isBusy);
        if (icon) icon.classList.toggle('d-none', isBusy);
    }

    function showStatus(kind, message) {
        if (!status) return;
        status.className = 'alert alert-' + kind;
        status.textContent = message;
    }

    function readValue(name) {
        var field = form.elements[name];
        return field ? field.value.trim() : '';
    }

    form.addEventListener('submit', async function (event) {
        event.preventDefault();

        var phone = readValue('phone');
        var email = readValue('email');
        if (!phone && !email) {
            showStatus('warning', form.dataset.contactRequired || 'Telefon veya e-posta zorunludur.');
            return;
        }

        var payload = {
            requesterName: readValue('requesterName'),
            phone: phone || null,
            email: email || null,
            requestTypeId: Number(readValue('requestTypeId')),
            requestDescription: readValue('requestDescription'),
            source: 'sln-web',
            website: readValue('website') || null
        };

        setBusy(true);
        try {
            var response = await fetch(window.location.pathname, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            var data = await response.json().catch(function () { return {}; });
            if (!response.ok) {
                showStatus('danger', data.error || data.message || form.dataset.errorMessage || 'Başvuru gönderilemedi.');
                return;
            }

            var tracking = data.uid ? ' ' + (form.dataset.trackingLabel || 'Takip numarası') + ': ' + data.uid : '';
            showStatus('success', (form.dataset.successMessage || 'Başvurunuz alındı.') + tracking);
            form.reset();
            form.elements.requestTypeId.value = '3';
        } catch (error) {
            showStatus('danger', form.dataset.errorMessage || 'Başvuru gönderilemedi.');
        } finally {
            setBusy(false);
        }
    });
})();
