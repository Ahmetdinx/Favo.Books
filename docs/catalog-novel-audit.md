# Roman kataloğu ilk denetimi — 2026-09-17

Denetim aracı: `tools/Favo.Books.CatalogAudit`.
Kural sürümü: `subject-form-v1`. Veritabanı işlemi salt okunurdur.

| Sonuç | Kitap |
| --- | ---: |
| Toplam | 9.858 |
| Açık roman etiketi bulunan aday | 84 |
| Başka edebî biçim etiketi bulunan aday | 961 |
| Çelişkili biçim bilgisi | 10 |
| Yeterli açık biçim kanıtı olmayan | 8.803 |

312 önceden kaydedilmiş kaynak sayfası incelendi. 9.857 kitap için kaynak kaydı
eşleştirildi; bir kitap için eşleştirme yok. Tüm kitaplarda kapak adresi bulunan
Türkçe baskı kaydı mevcut. Görseller indirilmedi veya görsel olarak doğrulanmadı.

**84 sayısı katalogdaki gerçek roman sayısı değildir.** Bu sayı dar kapsamlı
etiket kuralının bulduğu inceleme adaylarıdır. Roman etiketi olmayan birçok roman
belirsiz kalabilir; roman etiketli bir kaynak da roman üzerine inceleme olabilir.
Mevcut kategoriler `fiction` gibi geniş etiketlerden üretildiği için biçim kanıtı
olarak kullanılamaz. Bu rapora dayanarak kitap silinmez veya topluca yayımlanmaz.

Yerel ayrıntılar: `artifacts/catalog-audit-20260917/summary.json`, `books.jsonl`,
`sources.json`. Kaynak dosyalarının SHA-256 değerleri, özgün etiketler, kitap/eser
kimlikleri ve karar sürümü saklandı. Raporlarda kullanıcı bilgisi yok.

## Sonraki uygulama paketi

1. Mevcut katalogdaki bilinen klasik romanlardan, eser kimliği ve yayınevi/katalog
   kanıtıyla doğrulanmış bir başlangıç kümesi çıkar.
2. Aday sonucundan ayrı, kalıcı ve denetlenebilir editoryal uygunluk kaydı tasarla:
   roman / roman değil / belirsiz; kaynak, inceleyen, karar zamanı, kural sürümü.
3. Tarihî roman ile tarih araştırmasını, romanla öykü derlemesini ayıran örnekleri
   ve birbiriyle çelişen kaynakları kontrol et.
4. Doğrulanmış başlangıç kümesi ve kullanıcı geçmişini koruyan geçiş hazır olunca
   uygunluğu tüm katalog/öneri/yazma yollarında birlikte etkinleştir.

Şu an hiçbir kitap veya kullanıcı kaydı değiştirilmedi; roman filtresi henüz aktif
değil. Otomatik içe aktarma da başlatılmadı.

Doğrulama: Catalog birim testlerinin 83'ü geçti (19 yeni sınıflandırma senaryosu
dahil). Denetim aracı Release derlemesi sıfır uyarı/hata ile tamamlandı; yerel
veritabanı üzerinde rapor başarıyla üretildi.
