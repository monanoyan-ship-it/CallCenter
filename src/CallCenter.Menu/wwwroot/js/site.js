(function () {
  window.menuT = window.menuT || function (key, fallback) {
    return fallback || key;
  };

  if (window.toastr) {
    toastr.options = {
      closeButton: true,
      progressBar: true,
      positionClass: 'toast-top-right',
      timeOut: 3000
    };
  }

  if (window.jQuery) {
    $.ajaxSetup({
      converters: {
        'text json': function (data) {
          if (data == null || data === '') return null;
          var text = (typeof data === 'string' ? data : String(data)).trim();
          if (!text) return null;
          try { return JSON.parse(text); } catch (_) { return null; }
        }
      },
      error: function (xhr) {
        if (xhr.status === 401 && !location.pathname.match(/^\/Account\/Login/i)) {
          window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
        }
      }
    });
  }

  var toggle = document.querySelector('[data-menu-toggle]');
  var sidebar = document.querySelector('.menu-sidebar');

  if (!toggle || !sidebar) {
    return;
  }

  toggle.addEventListener('click', function () {
    sidebar.classList.toggle('open');
  });
})();
