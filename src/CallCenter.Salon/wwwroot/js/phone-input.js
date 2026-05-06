/**
 * Telefon numarasi input helper.
 * Ulke kodu secimi (aranabilir), otomatik format, +90/0 algilama, bosluk silme.
 * Backend'e "+905XXXXXXXXX" formatinda gonderir.
 *
 * Kullanim:
 *   HTML: <div data-bind="phoneInput: myObservable"></div>
 *   veya JS: initPhoneInput(inputElement, koObservable)
 */

function phoneT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function phoneCountryKey(countryCode) {
    return 'salon.phone.country.plus' + String(countryCode || '').replace(/\D/g, '');
}

function phoneCountryName(country) {
    return phoneT(phoneCountryKey(country.code), country.name);
}

// Ulke kodlari (en cok kullanilanlar uste)
var PHONE_COUNTRIES = [
    { code: '+90', name: 'Türkiye', flag: '🇹🇷', format: 'XXX XXX XX XX', maxDigits: 10 },
    { code: '+49', name: 'Almanya', flag: '🇩🇪', format: 'XXXX XXXXXXX', maxDigits: 11 },
    { code: '+44', name: 'İngiltere', flag: '🇬🇧', format: 'XXXX XXXXXX', maxDigits: 10 },
    { code: '+1', name: 'ABD / Kanada', flag: '🇺🇸', format: 'XXX XXX XXXX', maxDigits: 10 },
    { code: '+33', name: 'Fransa', flag: '🇫🇷', format: 'X XX XX XX XX', maxDigits: 9 },
    { code: '+31', name: 'Hollanda', flag: '🇳🇱', format: 'XX XXX XXXX', maxDigits: 9 },
    { code: '+32', name: 'Belçika', flag: '🇧🇪', format: 'XXX XX XX XX', maxDigits: 9 },
    { code: '+43', name: 'Avusturya', flag: '🇦🇹', format: 'XXXX XXXXXX', maxDigits: 10 },
    { code: '+41', name: 'İsviçre', flag: '🇨🇭', format: 'XX XXX XX XX', maxDigits: 9 },
    { code: '+46', name: 'İsveç', flag: '🇸🇪', format: 'XX XXX XX XX', maxDigits: 9 },
    { code: '+47', name: 'Norveç', flag: '🇳🇴', format: 'XXX XX XXX', maxDigits: 8 },
    { code: '+45', name: 'Danimarka', flag: '🇩🇰', format: 'XX XX XX XX', maxDigits: 8 },
    { code: '+39', name: 'İtalya', flag: '🇮🇹', format: 'XXX XXX XXXX', maxDigits: 10 },
    { code: '+34', name: 'İspanya', flag: '🇪🇸', format: 'XXX XXX XXX', maxDigits: 9 },
    { code: '+7', name: 'Rusya', flag: '🇷🇺', format: 'XXX XXX XX XX', maxDigits: 10 },
    { code: '+380', name: 'Ukrayna', flag: '🇺🇦', format: 'XX XXX XX XX', maxDigits: 9 },
    { code: '+30', name: 'Yunanistan', flag: '🇬🇷', format: 'XXX XXX XXXX', maxDigits: 10 },
    { code: '+359', name: 'Bulgaristan', flag: '🇧🇬', format: 'XX XXX XXXX', maxDigits: 9 },
    { code: '+40', name: 'Romanya', flag: '🇷🇴', format: 'XXX XXX XXX', maxDigits: 9 },
    { code: '+994', name: 'Azerbaycan', flag: '🇦🇿', format: 'XX XXX XX XX', maxDigits: 9 },
    { code: '+995', name: 'Gürcistan', flag: '🇬🇪', format: 'XXX XXX XXX', maxDigits: 9 },
    { code: '+966', name: 'Suudi Arabistan', flag: '🇸🇦', format: 'XX XXX XXXX', maxDigits: 9 },
    { code: '+971', name: 'BAE', flag: '🇦🇪', format: 'XX XXX XXXX', maxDigits: 9 },
    { code: '+972', name: 'İsrail', flag: '🇮🇱', format: 'XX XXX XXXX', maxDigits: 9 },
    { code: '+61', name: 'Avustralya', flag: '🇦🇺', format: 'XXX XXX XXX', maxDigits: 9 },
    { code: '+81', name: 'Japonya', flag: '🇯🇵', format: 'XX XXXX XXXX', maxDigits: 10 },
    { code: '+86', name: 'Çin', flag: '🇨🇳', format: 'XXX XXXX XXXX', maxDigits: 11 },
    { code: '+82', name: 'Güney Kore', flag: '🇰🇷', format: 'XX XXXX XXXX', maxDigits: 10 },
    { code: '+55', name: 'Brezilya', flag: '🇧🇷', format: 'XX XXXXX XXXX', maxDigits: 11 },
];

/**
 * Telefon numarasini normalize et: sadece rakamlar, ulke kodu ile.
 * Ornekler:
 *   "0532 123 45 67"     -> { countryCode: "+90", national: "5321234567" }
 *   "+90 532 123 4567"   -> { countryCode: "+90", national: "5321234567" }
 *   "532 123 45 67"      -> { countryCode: "+90", national: "5321234567" }
 *   "+49 1234 5678901"   -> { countryCode: "+49", national: "12345678901" }
 */
function normalizePhone(raw, defaultCountryCode) {
    if (!raw) return { countryCode: defaultCountryCode || '+90', national: '' };

    // Bosluk, tire, parantez temizle
    var cleaned = raw.replace(/[\s\-\(\)]/g, '');

    // Ulke kodu algilama
    var detectedCode = null;
    var national = cleaned;

    // + ile basliyorsa ulke kodunu bul
    if (cleaned.startsWith('+')) {
        // Uzun kodlardan kisaya dogru dene (3 hane, 2 hane, 1 hane)
        for (var i = 0; i < PHONE_COUNTRIES.length; i++) {
            var cc = PHONE_COUNTRIES[i].code;
            if (cleaned.startsWith(cc)) {
                detectedCode = cc;
                national = cleaned.substring(cc.length);
                break;
            }
        }
        if (!detectedCode) {
            // Bilinmeyen ulke kodu — ilk 1-4 rakam
            var m = cleaned.match(/^\+(\d{1,4})/);
            if (m) {
                detectedCode = '+' + m[1];
                national = cleaned.substring(detectedCode.length);
            }
        }
    }
    // 00 ile basliyorsa (uluslararasi format)
    else if (cleaned.startsWith('00')) {
        cleaned = '+' + cleaned.substring(2);
        return normalizePhone(cleaned, defaultCountryCode);
    }
    // 0 ile basliyorsa (Turkiye yerel format)
    else if (cleaned.startsWith('0')) {
        detectedCode = defaultCountryCode || '+90';
        national = cleaned.substring(1);
    }

    if (!detectedCode) detectedCode = defaultCountryCode || '+90';

    // Sadece rakamlar
    national = national.replace(/\D/g, '');

    return { countryCode: detectedCode, national: national };
}

/**
 * Tam numara olustur: "+905321234567"
 */
function formatFullPhone(countryCode, national) {
    if (!national) return '';
    return countryCode + national;
}

/**
 * Knockout custom binding: data-bind="phoneInput: observable"
 * observable'a "+905XXXXXXXXX" formatında yazar.
 */
ko.bindingHandlers.phoneInput = {
    init: function (element, valueAccessor) {
        var observable = valueAccessor();
        var container = document.createElement('div');
        container.className = 'input-group input-group-sm';
        container.innerHTML =
            '<button class="btn btn-outline-secondary dropdown-toggle phone-country-btn" type="button" style="min-width:80px; font-size:.85rem;"></button>' +
            '<div class="dropdown-menu phone-country-dropdown p-1" style="max-height:250px; overflow-y:auto; min-width:260px;">' +
                '<input type="text" class="form-control form-control-sm mb-1 phone-country-search" placeholder="' + phoneT('salon.phone.country_search', 'Ülke ara...') + '" />' +
                '<div class="phone-country-list"></div>' +
            '</div>' +
            '<input type="tel" class="form-control form-control-sm phone-national-input" placeholder="5XX XXX XX XX" />';

        element.innerHTML = '';
        element.appendChild(container);

        var btn = container.querySelector('.phone-country-btn');
        var dropdown = container.querySelector('.phone-country-dropdown');
        var searchInput = container.querySelector('.phone-country-search');
        var listDiv = container.querySelector('.phone-country-list');
        var nationalInput = container.querySelector('.phone-national-input');

        var selectedCountry = PHONE_COUNTRIES[0]; // Default: Turkiye

        function renderList(filter) {
            var q = (filter || '').toLowerCase();
            var html = '';
            PHONE_COUNTRIES.forEach(function (c) {
                var localizedName = phoneCountryName(c);
                if (q && localizedName.toLowerCase().indexOf(q) < 0 && c.name.toLowerCase().indexOf(q) < 0 && c.code.indexOf(q) < 0) return;
                html += '<a class="dropdown-item small py-1 px-2" href="#" data-code="' + c.code + '">' +
                    c.flag + ' ' + localizedName + ' <span class="text-muted">' + c.code + '</span></a>';
            });
            listDiv.innerHTML = html || '<div class="text-muted small px-2">' + phoneT('salon.common.no_results', 'Sonuç yok') + '</div>';
        }

        function updateBtn() {
            btn.textContent = selectedCountry.flag + ' ' + selectedCountry.code;
        }

        function updateObservable() {
            var national = nationalInput.value.replace(/\D/g, '');
            observable(formatFullPhone(selectedCountry.code, national));
        }

        // Mevcut deger varsa parse et
        var currentVal = ko.unwrap(observable);
        if (currentVal) {
            var parsed = normalizePhone(currentVal);
            var found = PHONE_COUNTRIES.find(function (c) { return c.code === parsed.countryCode; });
            if (found) selectedCountry = found;
            nationalInput.value = parsed.national;
        }

        updateBtn();
        renderList('');

        // Dropdown toggle
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            dropdown.classList.toggle('show');
            if (dropdown.classList.contains('show')) {
                searchInput.value = '';
                renderList('');
                setTimeout(function () { searchInput.focus(); }, 50);
            }
        });

        // Search
        searchInput.addEventListener('input', function () {
            renderList(searchInput.value);
        });

        // Select country
        listDiv.addEventListener('click', function (e) {
            var target = e.target.closest('[data-code]');
            if (!target) return;
            e.preventDefault();
            var code = target.getAttribute('data-code');
            var found = PHONE_COUNTRIES.find(function (c) { return c.code === code; });
            if (found) {
                selectedCountry = found;
                updateBtn();
                updateObservable();
                dropdown.classList.remove('show');
                nationalInput.focus();
            }
        });

        // Close dropdown on outside click
        document.addEventListener('click', function () {
            dropdown.classList.remove('show');
        });
        dropdown.addEventListener('click', function (e) { e.stopPropagation(); });

        // National input
        nationalInput.addEventListener('input', function () {
            // Sadece rakam birak
            var digits = nationalInput.value.replace(/\D/g, '');
            if (selectedCountry.maxDigits && digits.length > selectedCountry.maxDigits) {
                digits = digits.substring(0, selectedCountry.maxDigits);
            }
            // Basa 0 geliyorsa sil (ulke kodu zaten var)
            if (digits.startsWith('0')) digits = digits.substring(1);
            nationalInput.value = digits;
            updateObservable();
        });

        // Paste handler — +90 veya 0 ile baslarsa algila
        nationalInput.addEventListener('paste', function (e) {
            setTimeout(function () {
                var pasted = nationalInput.value;
                var parsed = normalizePhone(pasted);
                var found = PHONE_COUNTRIES.find(function (c) { return c.code === parsed.countryCode; });
                if (found) selectedCountry = found;
                updateBtn();
                nationalInput.value = parsed.national;
                updateObservable();
            }, 10);
        });

        // Observable degistiginde input'u guncelle
        if (ko.isObservable(observable)) {
            observable.subscribe(function (newVal) {
                if (!newVal) { nationalInput.value = ''; return; }
                var parsed = normalizePhone(newVal);
                var found = PHONE_COUNTRIES.find(function (c) { return c.code === parsed.countryCode; });
                if (found && found.code !== selectedCountry.code) {
                    selectedCountry = found;
                    updateBtn();
                }
                if (nationalInput.value.replace(/\D/g, '') !== parsed.national) {
                    nationalInput.value = parsed.national;
                }
            });
        }
    }
};
