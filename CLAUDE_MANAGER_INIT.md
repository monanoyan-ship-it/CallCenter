# ClaudeManager Entegrasyon Sablonu

Bu dosyayi yeni bir projeye kopyaladiginda Gemini'ye su komutu ver: 
*"Bu dosyadaki kurallara gore ClaudeManager entegrasyonunu yap ve GEMINI.md dosyasini olustur."*

## Kurulum Bilgileri
- **Manager URL:** http://127.0.0.1:41847
- **Project ID:** [BURAYA_ID_YAZILACAK]
- **Chat ID:** [BURAYA_CHAT_ID_YAZILACAK]

## Gemini Talimatlari
1. Her session basinda `Invoke-RestMethod -Uri "http://127.0.0.1:41847/api/guide?cwd=$(pwd)"` komutuyla rehberi oku.
2. Bilgi ararken onceligi Manager API'sine ver (Notes, Journal, Roadmap).
3. Yapilan islemleri ilgili Chat ID'ye mesaj olarak atarak otonom ilerleyisi kaydet.
4. Ahmet'in Turkce tercihlerine ve "Quick Win" vizyonuna sadik kal.
