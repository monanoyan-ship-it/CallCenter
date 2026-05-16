(function () {
    if (window.__slnLayoutBound) return;
    window.__slnLayoutBound = true;

    var root = document.getElementById('salon-layout-data');
    if (!root) return;

    var redirecting = false;
    var authenticated = root.getAttribute('data-authenticated') === 'true';
    var roleId = parseInt(root.getAttribute('data-role-id') || '101', 10);
    var jwtBranch = root.getAttribute('data-jwt-branch') || '';
    var sessionKey = root.getAttribute('data-session-key') || '';
    var translationsRaw = root.getAttribute('data-translations') || '{}';

    function getSessionStorage() {
        try {
            return window.sessionStorage;
        } catch (error) {
            return null;
        }
    }

    function getTranslations() {
        try {
            return JSON.parse(translationsRaw);
        } catch (error) {
            return {};
        }
    }

    function toggleGroup(btn) {
        var items = btn ? btn.nextElementSibling : null;
        var chevron = btn ? btn.querySelector('.chevron') : null;
        var isOpen = items && items.classList.contains('show');

        document.querySelectorAll('.nav-group-items.show').forEach(function (element) {
            element.classList.remove('show');
            var previous = element.previousElementSibling;
            var previousChevron = previous ? previous.querySelector('.chevron') : null;
            if (previousChevron) previousChevron.classList.remove('open');
        });

        if (!isOpen && items) {
            items.classList.add('show');
            if (chevron) chevron.classList.add('open');
        }
    }

    function getStoredBranch() {
        var storage = getSessionStorage();
        if (!storage) return '';
        return storage.getItem('slnBranchId') || '';
    }

    function setStoredBranch(branchId) {
        var storage = getSessionStorage();
        if (!storage) return;

        if (!branchId) storage.removeItem('slnBranchId');
        else storage.setItem('slnBranchId', branchId);
    }

    function switchBranch(branchId) {
        setStoredBranch(branchId);
        window.location.reload();
    }

    function buildBranchSelector(branches) {
        var container = document.getElementById('branchSelector');
        if (!container || branches.length <= 1) return;

        var currentBranch = getStoredBranch();
        var select = document.createElement('select');
        select.className = 'form-select form-select-sm';
        select.style.width = 'auto';
        select.style.minWidth = '150px';
        select.setAttribute('data-branch-select', '1');

        var allOption = document.createElement('option');
        allOption.value = '';
        allOption.textContent = window.salonT('salon.common.all_branches', 'Tum Subeler');
        if (!currentBranch) allOption.selected = true;
        select.appendChild(allOption);

        branches.forEach(function (branch) {
            var option = document.createElement('option');
            option.value = String(branch.id);
            option.textContent = branch.name || '';
            if (currentBranch && currentBranch === String(branch.id)) option.selected = true;
            select.appendChild(option);
        });

        container.replaceChildren(select);
    }

    function initBranchSelector() {
        if (!authenticated) return;

        var storage = getSessionStorage();
        if (storage) {
            var previousSessionKey = storage.getItem('slnBranchSessionKey');
            if (previousSessionKey !== sessionKey) {
                if (jwtBranch) storage.setItem('slnBranchId', jwtBranch);
                else storage.removeItem('slnBranchId');
                storage.setItem('slnBranchSessionKey', sessionKey);
            }

            if (jwtBranch) storage.setItem('slnBranchId', jwtBranch);
        }

        if (roleId !== 101) return;

        $.ajax({
            url: '/proxy/sln-branches?_nb=1',
            dataType: 'text',
            cache: false
        }).done(function (text, _status, xhr) {
            if (xhr && (xhr.status === 204 || xhr.status === 205)) return;

            var body = text == null ? '' : String(text).trim();
            if (!body || body === 'null') return;

            var data;
            try {
                data = JSON.parse(body);
            } catch (error) {
                return;
            }

            var branches = Array.isArray(data) ? data : ((data && data.items) || []);
            buildBranchSelector(branches);
        });
    }

    function injectBranch(url) {
        if (!url || url.indexOf('/proxy/') < 0) return url;
        if (url.indexOf('_nb=1') >= 0) return url;
        if (/([?&])branchId=/.test(url)) return url;
        if (/([?&])allBranches=/.test(url)) return url;

        var branchId = getStoredBranch();
        if (!branchId) return url;

        return url + (url.indexOf('?') >= 0 ? '&' : '?') + 'branchId=' + encodeURIComponent(branchId);
    }

    window.salonTranslations = Object.assign({}, window.salonTranslations || {}, getTranslations());
    window.salonT = window.salonT || function (key, fallback) {
        return (window.salonTranslations && window.salonTranslations[key]) || fallback || key;
    };
    window.slnGetBranch = getStoredBranch;
    window.switchBranch = switchBranch;
    window.slnAllBranchesValue = '__all__';
    window.slnBuildBranchTargetOptions = function (branches) {
        return [{
            id: window.slnAllBranchesValue,
            name: window.salonT('salon.common.all_branches', 'Tum Subeler')
        }].concat(branches || []);
    };
    window.slnResolveBranchTarget = function (value, warningKey, fallback) {
        if (value === window.slnAllBranchesValue) {
            return { ok: true, branchId: null, allBranches: true };
        }

        var branchId = parseInt(value, 10) || null;
        if (branchId) {
            return { ok: true, branchId: branchId, allBranches: false };
        }

        var currentBranch = parseInt(getStoredBranch(), 10) || null;
        if (currentBranch) {
            return { ok: true, branchId: currentBranch, allBranches: false };
        }

        if (window.toastr) {
            toastr.warning(window.salonT(warningKey || 'salon.common.branch_target_required', fallback || 'Sube secin veya Tum Subeler secenegini kullanin'));
        }
        return { ok: false, branchId: null, allBranches: false };
    };
    window.slnAppendBranchTarget = function (url, target) {
        if (!target || !target.ok) return url;
        var separator = url.indexOf('?') >= 0 ? '&' : '?';
        if (target.allBranches) return url + separator + 'allBranches=true';
        if (target.branchId) return url + separator + 'branchId=' + encodeURIComponent(target.branchId);
        return url;
    };

    $.ajaxSetup({
        converters: {
            'text json': function (data) {
                if (data == null || data === '') return null;
                var value = (typeof data === 'string' ? data : String(data)).trim();
                if (!value) return null;
                try {
                    return JSON.parse(value);
                } catch (error) {
                    return null;
                }
            }
        },
        error: function (xhr) {
            if (xhr.status === 401 && !redirecting && !location.pathname.match(/^\/Account\/Login/i)) {
                redirecting = true;
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
            }
        }
    });

    if (window.toastr) {
        toastr.options = { closeButton: true, progressBar: true, positionClass: 'toast-top-right', timeOut: 3000 };
    }

    document.addEventListener('click', function (event) {
        var navToggle = event.target.closest('[data-nav-group-toggle]');
        if (navToggle) {
            event.preventDefault();
            toggleGroup(navToggle);
        }
    });

    document.addEventListener('change', function (event) {
        var branchSelect = event.target.closest('[data-branch-select]');
        if (!branchSelect) return;
        switchBranch(branchSelect.value);
    });

    document.cookie = 'SelectedBranch=;path=/;max-age=0';

    $.ajaxPrefilter(function (options) {
        options.url = injectBranch(options.url);
    });

    if (!window.__slnAjaxWrapped) {
        window.__slnAjaxWrapped = true;
        var originalAjax = $.ajax;
        $.ajax = function (urlOrOptions, maybeOptions) {
            var options = typeof urlOrOptions === 'string'
                ? Object.assign({ url: urlOrOptions }, maybeOptions || {})
                : Object.assign({}, urlOrOptions || {});

            options.url = injectBranch(options.url);
            return originalAjax.call(this, options);
        };
    }

    if (!window.__slnFetchWrapped && window.fetch) {
        window.__slnFetchWrapped = true;
        var originalFetch = window.fetch;
        window.fetch = function (input, init) {
            if (typeof input === 'string') {
                input = injectBranch(input);
            } else if (input && typeof input.url === 'string') {
                input = new Request(injectBranch(input.url), input);
            }

            return originalFetch.call(this, input, init);
        };
    }

    initBranchSelector();
})();
