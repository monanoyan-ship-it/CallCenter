function slnJsT(key, fallback) {
    return (window.salonT || function (k, f) { return f || k; })(key, fallback);
}

function ConsentFormsViewModel() {
    var self = this;
    self.forms = ko.observableArray([]);
    self.signedConsents = ko.observableArray([]);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.selectedTemplateKey = ko.observable('');

    function html(title, paragraphs, risks, extra) {
        var parts = ['<h4>' + title + '</h4>'];
        paragraphs.forEach(function (p) { parts.push('<p>' + p + '</p>'); });
        if (risks && risks.length) {
            parts.push('<h5>Olası Riskler ve Bilgilendirme</h5>');
            parts.push('<ul>');
            risks.forEach(function (r) { parts.push('<li>' + r + '</li>'); });
            parts.push('</ul>');
        }
        if (extra) parts.push(extra);
        parts.push('<h5>Onay</h5>');
        parts.push('<p>Yukarıdaki bilgileri okudum, anladım ve işlemin yapılmasını kabul ediyorum.</p>');
        parts.push('<p><strong>Müşteri Adı Soyadı:</strong> ____________________________</p>');
        parts.push('<p><strong>Tarih:</strong> ____ / ____ / ______</p>');
        return parts.join('\n');
    }

    self.templates = ko.observableArray([
        {
            key: 'hair-color-consent',
            title: 'Saç Boyama Onam Formu',
            requireSignature: true,
            htmlContent: html(
                'Saç Boyama Onam Formu',
                [
                    'Bu form, saç boyama işlemi öncesinde müşterinin bilgilendirilmesi ve onayının alınması amacıyla hazırlanmıştır.',
                    'İşlem; saç rengi açma, koyulaştırma, tonlama, dip boya, röfle, balyaj, ombre/sombre veya benzeri kimyasal uygulamaları içerebilir.'
                ],
                [
                    'Saçta kuruma, matlaşma, kırılma veya yıpranma oluşabilir.',
                    'İstenen renk sonucu saçın önceki işlem geçmişi, mevcut rengi ve saç yapısına göre farklılık gösterebilir.',
                    'Saç açma işlemlerinde renk eşitsizliği veya ek tonlama ihtiyacı oluşabilir.',
                    'Saç derisinde yanma, kaşıntı, kızarıklık, hassasiyet veya alerjik reaksiyon görülebilir.',
                    'Daha önce boya, açıcı, keratin, perma veya düzleştirme yapılmış saçlarda beklenmeyen sonuçlar oluşabilir.'
                ],
                '<h5>Müşteri Beyanı</h5><p>Alerji, hamilelik, emzirme, saç derisi rahatsızlığı, açık yara, egzama, yakın zamanda yapılmış kimyasal işlem veya kullandığım özel ilaçlar varsa salon personeline bildirmem gerektiğini biliyorum.</p><p>Gerek görülürse test tutamı veya alerji testi önerilebileceğini kabul ederim.</p>'
            )
        },
        {
            key: 'laser-epilation-consent',
            title: 'Lazer Epilasyon Onam Formu',
            requireSignature: true,
            htmlContent: html(
                'Lazer Epilasyon Onam Formu',
                [
                    'Bu form, lazer epilasyon işlemi öncesinde müşterinin bilgilendirilmesi ve onayının alınması amacıyla hazırlanmıştır.',
                    'İşlem yapılacak bölge, cilt tipi, kıl yapısı ve seans aralıkları uzman değerlendirmesine göre belirlenir.'
                ],
                [
                    'İşlem sırasında yanma, batma, sıcaklık hissi, kızarıklık veya hassasiyet oluşabilir.',
                    'Nadir durumlarda kabuklanma, leke, yanık, su toplaması veya renk değişikliği görülebilir.',
                    'Güneşlenme, solaryum, bronzluk ve bazı ilaçlar işlem riskini artırabilir.',
                    'Sonuçlar kişiden kişiye değişebilir; seans sayısı garanti edilemez.'
                ],
                '<h5>Müşteri Beyanı</h5><p>Hamilelik, emzirme, aktif cilt hastalığı, açık yara, epilepsi, ışığa duyarlılık, kullanılan ilaçlar veya yakın zamanda bronzlaşma varsa bildirmem gerektiğini biliyorum.</p><p>İşlem öncesi ve sonrası bakım talimatlarına uymayı kabul ederim.</p>'
            )
        },
        {
            key: 'skin-care-consent',
            title: 'Cilt Bakımı Onam Formu',
            requireSignature: true,
            htmlContent: html(
                'Cilt Bakımı Onam Formu',
                [
                    'Bu form, cilt bakımı uygulaması öncesinde müşterinin bilgilendirilmesi ve onayının alınması amacıyla hazırlanmıştır.',
                    'İşlem; cilt temizliği, peeling, maske, serum, buhar, komedon temizliği veya benzeri bakım adımlarını içerebilir.'
                ],
                [
                    'İşlem sonrası geçici kızarıklık, hassasiyet, kuruluk veya hafif yanma hissi oluşabilir.',
                    'Aktif akne, hassas cilt, alerji veya cilt bariyeri bozukluğu olan kişilerde reaksiyon riski artabilir.',
                    'Kullanılan ürünlere karşı alerjik reaksiyon gelişebilir.'
                ],
                '<h5>Müşteri Beyanı</h5><p>Alerji, aktif cilt hastalığı, yakın zamanda yapılan peeling/lazer işlemi veya kullandığım cilt ilaçları varsa salon personeline bildirmem gerektiğini kabul ederim.</p>'
            )
        },
        {
            key: 'permanent-makeup-consent',
            title: 'Kalıcı Makyaj / Microblading Onam Formu',
            requireSignature: true,
            htmlContent: html(
                'Kalıcı Makyaj / Microblading Onam Formu',
                [
                    'Bu form, kalıcı makyaj veya microblading işlemi öncesinde müşterinin bilgilendirilmesi ve onayının alınması amacıyla hazırlanmıştır.',
                    'İşlem; pigment uygulaması, kaş/kirpik/dudak bölgesi tasarımı ve rötuş sürecini içerebilir.'
                ],
                [
                    'İşlem sonrası kızarıklık, şişlik, kabuklanma, hassasiyet veya renk değişimi oluşabilir.',
                    'Pigment tutma oranı cilt yapısı, bakım, metabolizma ve yaşam tarzına göre değişebilir.',
                    'Enfeksiyon, alerjik reaksiyon veya istenmeyen renk sonucu riski bulunur.',
                    'Rötuş ihtiyacı oluşabilir ve nihai sonuç iyileşme süreci sonunda değerlendirilir.'
                ],
                '<h5>Müşteri Beyanı</h5><p>Kan sulandırıcı, diyabet, hamilelik, emzirme, uçuk geçmişi, alerji, cilt hastalığı veya düzenli ilaç kullanımı varsa bildirmem gerektiğini biliyorum.</p>'
            )
        },
        {
            key: 'general-treatment-consent',
            title: 'Genel İşlem Onam Formu',
            requireSignature: true,
            htmlContent: html(
                'Genel İşlem Onam Formu',
                [
                    'Bu form, salon hizmetleri öncesinde müşterinin genel işlem koşulları hakkında bilgilendirilmesi ve onayının alınması amacıyla hazırlanmıştır.',
                    'İşlem içeriği, kullanılacak ürünler, süre ve beklenen sonuçlar hizmet türüne göre değişebilir.'
                ],
                [
                    'Her işlemde kişisel hassasiyet, alerji veya beklenmeyen reaksiyon riski bulunabilir.',
                    'Sonuçlar kişisel özelliklere, önceki işlemlere ve bakım talimatlarına uyuma göre değişebilir.',
                    'Müşteri, bilinen rahatsızlıklarını, alerjilerini ve özel durumlarını işlem öncesinde bildirmelidir.'
                ],
                '<h5>Müşteri Beyanı</h5><p>İşlem öncesi verdiğim bilgilerin doğru olduğunu ve işlem sonrası bakım önerilerine uyacağımı kabul ederim.</p>'
            )
        },
        {
            key: 'photo-video-consent',
            title: 'Fotoğraf / Video Kullanım İzni',
            requireSignature: true,
            htmlContent: html(
                'Fotoğraf / Video Kullanım İzni',
                [
                    'Bu form, işlem öncesi/sonrası görsellerin kayıt altına alınması ve belirtilen kanallarda kullanılabilmesi için müşterinin onayını almak amacıyla hazırlanmıştır.',
                    'Görseller salon arşivi, portföy, sosyal medya, web sitesi veya tanıtım materyallerinde kullanılabilir.'
                ],
                [
                    'Müşteri, görsellerin hangi amaçla kullanılacağını sorma ve izin kapsamını sınırlandırma hakkına sahiptir.',
                    'Kimlik bilgileri, açık izin olmadan paylaşılmamalıdır.'
                ],
                '<h5>İzin Kapsamı</h5><p>Görsellerimin salon portföyü ve tanıtım amaçlı kullanılmasına izin veriyorum. İzin kapsamı: ____________________________</p>'
            )
        },
        {
            key: 'kvkk-explicit-consent',
            title: 'KVKK Aydınlatma ve Açık Rıza Formu',
            requireSignature: true,
            htmlContent: html(
                'KVKK Aydınlatma ve Açık Rıza Formu',
                [
                    'Bu form, 6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında kişisel verilerin işlenmesine ilişkin bilgilendirme ve açık rıza alınması amacıyla hazırlanmıştır.',
                    'Ad, soyad, iletişim bilgileri, randevu bilgileri, işlem geçmişi, ödeme bilgileri ve hizmet sunumu için gerekli diğer veriler işlenebilir.'
                ],
                [
                    'Kişisel veriler hizmet sunumu, müşteri iletişimi, randevu yönetimi, yasal yükümlülükler ve pazarlama izni verilen durumlarda kampanya bilgilendirmesi için kullanılabilir.',
                    'Müşteri KVKK kapsamındaki haklarını kullanmak için salona başvurabilir.'
                ],
                '<h5>Açık Rıza</h5><p>Kişisel verilerimin yukarıda belirtilen amaçlarla işlenmesini kabul ediyorum. Pazarlama iletileri için ayrı onayım: Evet / Hayır</p>'
            )
        },
        {
            key: 'appointment-policy',
            title: 'Randevu İptal / No-show Politika Onayı',
            requireSignature: true,
            htmlContent: html(
                'Randevu İptal / No-show Politika Onayı',
                [
                    'Bu form, randevu iptal, geç kalma ve randevuya gelmeme koşullarının müşteriye bildirilmesi amacıyla hazırlanmıştır.',
                    'Salon, yoğunluk ve hizmet planlaması nedeniyle randevuların zamanında iptal edilmesini talep eder.'
                ],
                [
                    'Geç kalınması halinde hizmet süresi kısalabilir veya randevu yeniden planlanabilir.',
                    'Belirlenen süre içinde iptal edilmeyen randevular için kapora yanabilir veya no-show ücreti uygulanabilir.',
                    'Politika koşulları salon tarafından hizmet türüne göre güncellenebilir.'
                ],
                '<h5>Müşteri Beyanı</h5><p>Randevu iptal ve no-show politikasını okudum, anladım ve kabul ediyorum.</p>'
            )
        }
    ]);

    self.form = {
        title: ko.observable(''),
        htmlContent: ko.observable(''),
        requireSignature: ko.observable(false),
        isActive: ko.observable(true)
    };

    var formModal;

    self.loadForms = function () {
        $.ajax({ url: '/proxy/sln-consent-forms', method: 'GET' }).done(function (data) {
            self.forms(data.items || data);
        });
    };

    self.loadConsents = function () {
        $.ajax({ url: '/proxy/sln-consent-forms/consents', method: 'GET' }).done(function (data) {
            self.signedConsents(data.items || data);
        });
    };

    self.resetForm = function () {
        self.form.title('');
        self.form.htmlContent('');
        self.form.requireSignature(false);
        self.form.isActive(true);
        self.selectedTemplateKey('');
        self.isEditing(false);
        self.editingId(null);
    };

    self.applyTemplate = function () {
        var key = self.selectedTemplateKey();
        if (!key) return;
        var template = self.templates().find(function (item) { return item.key === key; });
        if (!template) return;

        self.form.title(template.title);
        self.form.htmlContent(template.htmlContent);
        self.form.requireSignature(template.requireSignature === true);
        self.form.isActive(true);
    };

    self.openNewForm = function () {
        self.resetForm();
        formModal.show();
    };

    self.openEditForm = function (form) {
        self.isEditing(true);
        self.editingId(form.id);
        self.form.title(form.title);
        self.form.htmlContent(form.htmlContent);
        self.form.requireSignature(form.requireSignature);
        self.form.isActive(form.isActive);
        self.selectedTemplateKey('');
        formModal.show();
    };

    self.saveForm = function () {
        var data = {
            title: self.form.title(),
            htmlContent: self.form.htmlContent(),
            requireSignature: self.form.requireSignature(),
            isActive: self.form.isActive()
        };

        if (!data.title || !data.htmlContent) {
            toastr.warning(slnJsT('salon.consentforms.js.baslik_ve_icerik_zorunludur', 'Başlık ve içerik zorunludur'));
            return;
        }

        self.isSaving(true);
        var url = '/proxy/sln-consent-forms';
        var method = 'POST';
        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({ url: url, method: method, contentType: 'application/json', data: JSON.stringify(data) })
            .done(function () {
                formModal.hide();
                self.loadForms();
                toastr.success(self.isEditing()
                    ? slnJsT('salon.consentforms.js.form_guncellendi', 'Form güncellendi')
                    : slnJsT('salon.consentforms.js.form_olusturuldu', 'Form oluşturuldu'));
                self.isSaving(false);
            }).fail(function (xhr) {
                toastr.error(xhr.responseJSON || slnJsT('salon.common.error.generic', 'Bir hata oluştu'));
                self.isSaving(false);
            });
    };

    function escapeHtml(value) {
        return (value || '').toString()
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function safeFileName(value) {
        var normalized = (value || 'onam-formu').toString()
            .trim()
            .toLocaleLowerCase('tr-TR')
            .replace(/ğ/g, 'g').replace(/ü/g, 'u').replace(/ş/g, 's')
            .replace(/ı/g, 'i').replace(/ö/g, 'o').replace(/ç/g, 'c')
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '');
        return normalized || 'onam-formu';
    }

    function buildWordDocument(form) {
        var title = escapeHtml(form.title || 'Onam Formu');
        var content = form.htmlContent || '';
        return '<!DOCTYPE html>' +
            '<html><head><meta charset="utf-8">' +
            '<title>' + title + '</title>' +
            '<style>' +
            '@page{size:A4;margin:2cm;}body{font-family:Arial,sans-serif;font-size:11pt;line-height:1.45;color:#111;}' +
            'h1,h2,h3,h4{margin:0 0 12pt 0;}h4{font-size:16pt;}h5{font-size:12pt;margin:14pt 0 6pt 0;}' +
            'p{margin:0 0 8pt 0;}ul{margin:0 0 10pt 20pt;}li{margin-bottom:4pt;}' +
            '.signature{margin-top:24pt;border-top:1px solid #999;padding-top:12pt;}' +
            '</style></head><body>' +
            '<h2>' + title + '</h2>' +
            content +
            '<div class="signature">' +
            '<p><strong>Müşteri İmzası:</strong> ____________________________</p>' +
            '<p><strong>Personel:</strong> ____________________________</p>' +
            '</div>' +
            '</body></html>';
    }

    self.downloadWord = function (form) {
        if (!form) return;
        var blob = new Blob(['\ufeff', buildWordDocument(form)], {
            type: 'application/msword;charset=utf-8'
        });
        var url = URL.createObjectURL(blob);
        var link = document.createElement('a');
        link.href = url;
        link.download = safeFileName(form.title) + '.doc';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    };

    self.removeForm = function (form) {
        confirmModal(slnJsT('salon.common.btn.confirm', 'Onayla'), slnJsT('salon.consentforms.js.bu_formu_silmek_istediginize_emin_misiniz', 'Bu formu silmek istediğinize emin misiniz?'), function () {
            $.ajax({ url: '/proxy/sln-consent-forms/' + form.id, method: 'DELETE' })
                .done(function () { self.loadForms(); toastr.success(slnJsT('salon.consentforms.js.form_silindi', 'Form silindi')); })
                .fail(function () { toastr.error(slnJsT('salon.common.delete_failed', 'Silinemedi')); });
        });
    };

    $(document).ready(function () {
        formModal = new bootstrap.Modal(document.getElementById('consentFormModal'));
        self.loadForms();
        self.loadConsents();
    });
}

ko.applyBindings(new ConsentFormsViewModel(), document.getElementById('consentforms-vm'));
