/**
 * Autocomplete factory - KnockoutJS select/combobox yerine autocomplete olusturur.
 * @param {ko.observableArray} sourceArray - Veri kaynagi
 * @param {string} textField - Gosterilecek alan adi (ör: 'fullName', 'name')
 * @param {ko.observable} valueObservable - Secilen ID yazilacak form field
 * @param {number} [maxShow=20] - Dropdown'da max gosterilecek
 * @returns {{ query, selectedName, showDropdown, highlightIndex, filteredItems, onFocus, onBlur, onKeydown, select, clear, setFromValue, getDisplayText }}
 */
function createAutocomplete(sourceArray, textField, valueObservable, maxShow) {
    maxShow = maxShow || 20;
    var ac = {};
    ac.query = ko.observable('');
    ac.selectedName = ko.observable('');
    ac.showDropdown = ko.observable(false);
    ac.highlightIndex = ko.observable(-1);
    var blurTimeout = null;

    ac.sortedItems = ko.computed(function () {
        var items = (sourceArray() || []).slice();
        items.sort(function (a, b) {
            return ((a[textField] || '').localeCompare(b[textField] || '', 'tr'));
        });
        return items;
    });

    ac.filteredItems = ko.computed(function () {
        var q = (ac.query() || '').toLowerCase().trim();
        var sorted = ac.sortedItems();
        if (!q || q === ac.selectedName().toLowerCase()) {
            return sorted.slice(0, maxShow);
        }
        var filtered = sorted.filter(function (item) {
            return (item[textField] || '').toLowerCase().indexOf(q) >= 0;
        });
        return filtered.slice(0, maxShow);
    });

    ac.getDisplayText = function (item) {
        return item[textField] || '';
    };

    ac.onFocus = function () {
        if (blurTimeout) { clearTimeout(blurTimeout); blurTimeout = null; }
        ac.showDropdown(true);
        ac.highlightIndex(-1);
        return true;
    };

    ac.onBlur = function () {
        var q = (ac.query() || '').trim().toLowerCase();
        if (q && !ko.unwrap(valueObservable)) {
            var exact = (sourceArray() || []).find(function (item) {
                return String(item[textField] || '').trim().toLowerCase() === q;
            });
            if (exact) ac.select(exact);
        }
        blurTimeout = setTimeout(function () { ac.showDropdown(false); }, 200);
        return true;
    };

    ac.select = function (item) {
        valueObservable(item.id);
        ac.query(item[textField]);
        ac.selectedName(item[textField]);
        ac.showDropdown(false);
    };

    ac.clear = function () {
        valueObservable(null);
        ac.query('');
        ac.selectedName('');
        ac.highlightIndex(-1);
    };

    ac.onKeydown = function (data, event) {
        var key = event.keyCode;
        var items = ac.filteredItems();
        if (key === 40) { // Down
            ac.highlightIndex(Math.min(ac.highlightIndex() + 1, items.length - 1));
            ac.showDropdown(true);
            return false;
        } else if (key === 38) { // Up
            ac.highlightIndex(Math.max(ac.highlightIndex() - 1, -1));
            return false;
        } else if (key === 13) { // Enter
            var idx = ac.highlightIndex();
            if (idx >= 0 && idx < items.length) {
                ac.select(items[idx]);
            }
            return false;
        } else if (key === 27) { // Escape
            ac.showDropdown(false);
            return false;
        }
        // Reset selection on typing
        if (ac.selectedName() && ac.query() !== ac.selectedName()) {
            ac.selectedName('');
            valueObservable(null);
        }
        return true;
    };

    // query degisince highlight reset
    ac.query.subscribe(function (val) {
        if (val !== ac.selectedName()) {
            ac.highlightIndex(-1);
        }
    });

    /**
     * Edit modda ID'den text'i bulup set eder.
     * @param {number|string} id
     */
    ac.setFromValue = function (id) {
        if (!id) { ac.clear(); return; }
        var items = sourceArray() || [];
        for (var i = 0; i < items.length; i++) {
            if (items[i].id == id) {
                ac.query(items[i][textField] || '');
                ac.selectedName(items[i][textField] || '');
                return;
            }
        }
        // Bulunamadi - sadece ID'yi koru
        ac.query('');
        ac.selectedName('');
    };

    return ac;
}

/**
 * Multi-select autocomplete - birden fazla item secilebilir, tag/chip olarak gosterilir.
 * @param {ko.observableArray} sourceArray - Veri kaynagi
 * @param {string} textField - Gosterilecek alan adi
 * @param {ko.observableArray} valuesObservable - Secilen ID'ler dizisi
 * @param {number} [maxShow=20]
 */
function createMultiAutocomplete(sourceArray, textField, valuesObservable, maxShow) {
    maxShow = maxShow || 20;
    var ac = {};
    ac.query = ko.observable('');
    ac.showDropdown = ko.observable(false);
    ac.highlightIndex = ko.observable(-1);
    var blurTimeout = null;

    ac.selectedItems = ko.computed(function () {
        var ids = valuesObservable() || [];
        var items = sourceArray() || [];
        var result = [];
        for (var i = 0; i < ids.length; i++) {
            for (var j = 0; j < items.length; j++) {
                if (items[j].id == ids[i]) { result.push(items[j]); break; }
            }
        }
        return result;
    });

    ac.sortedItems = ko.computed(function () {
        var items = (sourceArray() || []).slice();
        items.sort(function (a, b) {
            return ((a[textField] || '').localeCompare(b[textField] || '', 'tr'));
        });
        return items;
    });

    ac.filteredItems = ko.computed(function () {
        var q = (ac.query() || '').toLowerCase().trim();
        var sorted = ac.sortedItems();
        var selected = valuesObservable() || [];
        // Secilmis olanlari filtrele
        var available = sorted.filter(function (item) {
            return selected.indexOf(item.id) < 0;
        });
        if (!q) return available.slice(0, maxShow);
        return available.filter(function (item) {
            return (item[textField] || '').toLowerCase().indexOf(q) >= 0;
        }).slice(0, maxShow);
    });

    ac.getDisplayText = function (item) { return item[textField] || ''; };

    ac.onFocus = function () {
        if (blurTimeout) { clearTimeout(blurTimeout); blurTimeout = null; }
        ac.showDropdown(true);
        ac.highlightIndex(-1);
        return true;
    };

    ac.onBlur = function () {
        blurTimeout = setTimeout(function () { ac.showDropdown(false); }, 200);
        return true;
    };

    ac.select = function (item) {
        var ids = (valuesObservable() || []).slice();
        if (ids.indexOf(item.id) < 0) ids.push(item.id);
        valuesObservable(ids);
        ac.query('');
        ac.highlightIndex(-1);
    };

    ac.remove = function (item) {
        var ids = (valuesObservable() || []).filter(function (id) { return id !== item.id; });
        valuesObservable(ids);
    };

    ac.clear = function () {
        valuesObservable([]);
        ac.query('');
        ac.highlightIndex(-1);
    };

    ac.onKeydown = function (data, event) {
        var key = event.keyCode;
        var items = ac.filteredItems();
        if (key === 40) {
            ac.highlightIndex(Math.min(ac.highlightIndex() + 1, items.length - 1));
            ac.showDropdown(true);
            return false;
        } else if (key === 38) {
            ac.highlightIndex(Math.max(ac.highlightIndex() - 1, -1));
            return false;
        } else if (key === 13) {
            var idx = ac.highlightIndex();
            if (idx >= 0 && idx < items.length) ac.select(items[idx]);
            return false;
        } else if (key === 27) {
            ac.showDropdown(false);
            return false;
        } else if (key === 8 && !ac.query()) {
            // Backspace bos inputta son tagi sil
            var ids = valuesObservable() || [];
            if (ids.length > 0) valuesObservable(ids.slice(0, -1));
            return false;
        }
        return true;
    };

    ac.query.subscribe(function () { ac.highlightIndex(-1); });

    ac.setFromValues = function (ids) {
        valuesObservable(ids || []);
        ac.query('');
    };

    return ac;
}

/**
 * Serbest metin autocomplete - onceden girilen degerleri onerir, yeni deger de yazilabilir.
 * @param {ko.observableArray} suggestionsArray - Oneri listesi (string dizisi)
 * @param {ko.observable} valueObservable - Deger yazilacak form field (string)
 * @param {number} [maxShow=15]
 */
function createTextAutocomplete(suggestionsArray, valueObservable, maxShow) {
    maxShow = maxShow || 15;
    var ac = {};
    ac.query = ko.observable('');
    ac.showDropdown = ko.observable(false);
    ac.highlightIndex = ko.observable(-1);
    var blurTimeout = null;

    ac.filteredItems = ko.computed(function () {
        var q = (ac.query() || '').toLowerCase().trim();
        var items = (suggestionsArray() || []).slice();
        if (!q) return items.slice(0, maxShow);
        return items.filter(function (s) {
            return (s || '').toLowerCase().indexOf(q) >= 0;
        }).slice(0, maxShow);
    });

    ac.onFocus = function () {
        if (blurTimeout) { clearTimeout(blurTimeout); blurTimeout = null; }
        ac.showDropdown(true);
        ac.highlightIndex(-1);
        return true;
    };

    ac.onBlur = function () {
        blurTimeout = setTimeout(function () {
            ac.showDropdown(false);
            valueObservable(ac.query());
        }, 200);
        return true;
    };

    ac.select = function (text) {
        valueObservable(text);
        ac.query(text);
        ac.showDropdown(false);
    };

    ac.clear = function () {
        valueObservable('');
        ac.query('');
        ac.highlightIndex(-1);
    };

    ac.onKeydown = function (data, event) {
        var key = event.keyCode;
        var items = ac.filteredItems();
        if (key === 40) {
            ac.highlightIndex(Math.min(ac.highlightIndex() + 1, items.length - 1));
            ac.showDropdown(true);
            return false;
        } else if (key === 38) {
            ac.highlightIndex(Math.max(ac.highlightIndex() - 1, -1));
            return false;
        } else if (key === 13) {
            var idx = ac.highlightIndex();
            if (idx >= 0 && idx < items.length) {
                ac.select(items[idx]);
            } else {
                valueObservable(ac.query());
                ac.showDropdown(false);
            }
            return false;
        } else if (key === 27) {
            ac.showDropdown(false);
            return false;
        }
        return true;
    };

    ac.query.subscribe(function (val) {
        valueObservable(val);
        ac.highlightIndex(-1);
    });

    ac.setFromValue = function (val) {
        ac.query(val || '');
        valueObservable(val || '');
    };

    return ac;
}
