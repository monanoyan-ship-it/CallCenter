(function () {
    window.salonPublicAuth = window.salonPublicAuth || {
        getStoredPlatformToken: function () {
            var token = localStorage.getItem('platformToken');
            if (!token || token === 'null' || token === 'undefined') {
                localStorage.removeItem('platformToken');
                localStorage.removeItem('platformUser');
                return '';
            }
            return token;
        },
        normalizePhoneValue: function (countrySelectorId, phoneSelectorId) {
            var code = document.getElementById(countrySelectorId).value;
            var raw = document.getElementById(phoneSelectorId).value
                .replace(/[\s\-\(\)]/g, '')
                .replace(/\D/g, '');
            if (raw.startsWith('0')) raw = raw.substring(1);
            return code + raw;
        }
    };
})();
