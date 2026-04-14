// Turkiye 81 Il — Discover ve Branch formlarinda datalist olarak kullanilir.
window.TR_CITIES = [
    "Adana","Adıyaman","Afyonkarahisar","Ağrı","Amasya","Ankara","Antalya","Artvin","Aydın","Balıkesir",
    "Bilecik","Bingöl","Bitlis","Bolu","Burdur","Bursa","Çanakkale","Çankırı","Çorum","Denizli",
    "Diyarbakır","Edirne","Elazığ","Erzincan","Erzurum","Eskişehir","Gaziantep","Giresun","Gümüşhane","Hakkari",
    "Hatay","Isparta","Mersin","İstanbul","İzmir","Kars","Kastamonu","Kayseri","Kırklareli","Kırşehir",
    "Kocaeli","Konya","Kütahya","Malatya","Manisa","Kahramanmaraş","Mardin","Muğla","Muş","Nevşehir",
    "Niğde","Ordu","Rize","Sakarya","Samsun","Siirt","Sinop","Sivas","Tekirdağ","Tokat",
    "Trabzon","Tunceli","Şanlıurfa","Uşak","Van","Yozgat","Zonguldak","Aksaray","Bayburt","Karaman",
    "Kırıkkale","Batman","Şırnak","Bartın","Ardahan","Iğdır","Yalova","Karabük","Kilis","Osmaniye","Düzce"
];

// "istanbul  " -> "İstanbul" — TR-aware title case + trim
window.normalizeTrCity = function (raw) {
    if (!raw) return raw;
    var s = String(raw).trim().replace(/\s+/g, ' ');
    if (!s) return '';
    // Once butun harfleri TR-small, sonra her kelimenin ilk harfini TR-upper
    var lower = s.toLocaleLowerCase('tr-TR');
    return lower.split(' ').map(function (w) {
        if (!w) return w;
        return w.charAt(0).toLocaleUpperCase('tr-TR') + w.slice(1);
    }).join(' ');
};
