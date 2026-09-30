# Roman ekleme politikası — 2026-09-17

23 Eylül ek notu: `tools/Favo.Books.PublisherImport` yayınevi kaynakları için
ayrı, çevrimdışı bir operatör akışı ekler. HTTP üzerinden onaysız kayıt yolu açmaz.
Kaynak karmasıyla eşleştirilen incelemeler
`data/curation/publisher-form-reviews-v1.json` içindedir. Roman dışı/belirsiz
kayıtlar ayrılır; yazar biyografisinden kaynaklanan yanlış elemeler kaynak
açıklaması incelenerek giderilir. Türkçe baskı, ISBN, kapak ve tekrar kontrolü
korunur. 23 Eylül'de 1.000 yeni roman aktarıldı: toplam 10.858 kayıt,
katalog koşullarını sağlayan 1.005 roman. Aynı batch yeniden kontrol edildiğinde
yeni kayıt oluşmadı. Ayrıntılar: `docs/publisher-novel-import-2026-09-23.md`.
Aşağıdaki gömülü liste kuralları Open Library akışı için geçerliliğini korur.
Yayınevi aracı için `tools/Favo.Books.PublisherImport/README.md` esas alınır.

Yeni kitap ekleme kuralları uygulandı. Katalog ve öneriler artık yalnızca son
inceleme kararı `Novel` olan, Türkçe kapaklı baskısı bulunan kayıtları gösterir.
Belirsiz kayıtlar ve kullanıcı geçmişi saklanır. Doğrulanmış `NotNovel` kayıtları
ayrı bakım komutuyla bağlantılı verileri yedeklenerek kaldırılabilir.

17 Eylül geçiş durumu: 9.858 kayıt korunuyor; yalnızca 5 kayıt doğrulanmış roman.
Silme raporunda kesinleşmiş `NotNovel` bulunmadı; hiçbir kitap silinmedi.
O tarihte istenen 1.000 yeni roman henüz eklenmemişti; aktarım 23 Eylül'de tamamlandı. Kaynak araştırması
`artifacts/novel-publisher-import` altında; bu HTML dosyaları onaylı içe aktarma
manifesti değildir. Toplu aktarım başlatılmadan önce eser, yazar, Türkçe baskı,
kapak ve mükerrerlik kontrolleri tamamlanmalıdır.

## Yeni kayıt koşulları

- Eser, derlemeye gömülü `data/curation/novel-import-priorities-v1.json` listesinde
  kaynakla doğrulanmış olmalı. Genel fiction/roman etiketi tek başına onay değildir.
- Sağlayıcı, eser başlığı ve yazar adları doğrulanmış kayıtla eşleşmeli.
- Onaylanan Türkçe başlığa sahip, kapak adresi bulunan Türkçe baskı seçilmeli.
  İlk Türkçe baskı kapaksızsa dönen diğer uygun baskılar da kontrol edilir.
- Kitap ve kaynaklı NovelReview aynı SaveChanges işlemiyle saklanır.
- Mevcut eser yeniden kullanılırken son roman kararı ve mevcut Türkçe kapak bilgisi
  tekrar kontrol edilir. Eski/onaysız veya onayı geri alınmış kayıt geçiş yapamaz.
- Başlık/yazarla serbest kitap oluşturan eski komut artık kayıt yazmaz. Geçerli
  isteklerde 422 `novel_review_required` döndürür; normal giriş/doğrulama kuralları sürer.

Doğrudan external resolve API, toplu import handler ve onu kullanan arka plan işi
aynı çözümleme kontrolünden geçer. Politika reddi, bulunamayan kitap yerine açık
bir ret kodu olarak raporlanır; toplu işlemde failure listesine/sayacına girer.

| Kod | Anlam |
| --- | --- |
| novel_review_required | Kaynakla onaylanmış eser listesinde değil |
| reviewed_identity_mismatch | Sağlayıcı/eser başlığı/yazar onayla uyuşmuyor |
| covered_turkish_edition_required | Onaylanan başlıkla kapaklı Türkçe baskı bulunamadı |
| existing_book_not_eligible | Mevcut kaydın güncel roman/kapak uygunluğu yok |

External resolve bu retlerde HTTP 422 ProblemDetails verir. Onaylı eser dış
kaynakta bulunamazsa 404 davranışı korunur. Yeni onay HTTP isteğiyle verilemez;
liste kaynak kanıtıyla geliştirme/editoryal süreçte genişletilir ve yeniden derlenir.

## Kontrollü sıra ve sınırlar

CatalogSeed artık rastgele Türkçe sonuç sayfalarını taramaz. Yalnız doğrulanmış
listede Priority alanına göre ilerler ve liste bitince durur. Varsayılan çalıştırma
yalnız önizlemedir; yazmak için `--apply` gerekir. Eski yarım kalan geniş import
dizinleri tekrar çalıştırılamaz. Yeni çalışma dizininde aynı kitaplar yeniden
oluşturulmaz; dış eser referansı ve son inceleme kararı kontrol edilir.

Liste şu an beş klasik içeriyor. Bu **ölçülmüş en çok okunan sıralaması değildir**;
editoryal klasik önceliğidir. Daha büyük liste için kaynaklı eser doğrulaması ve
güvenilir popülerlik/talep verisi gerekir. Bu geliştirmede yeni kitap indirilmedi
veya eklenmedi. Yeni onay mekanizması eski 9.858 kitabı otomatik onaylamaz.

Kapak kontrolü metadata seviyesindedir; görselin doğruluğu/ulaşılabilirliği bu
işlemde doğrulanmaz. Katı başlık/yazar eşleşmesi sağlayıcı metni değişince güvenli
biçimde reddedebilir; transliterasyon/değişiklik kaynakla incelenmelidir.
Eski katalog/öneri ekranlarında roman filtresinin açılması ayrı geçiş işidir.

## Doğrulama

- 129 API entegrasyon testi geçti (10 yeni import politikası senaryosu dahil).
- 90 Catalog birim testi geçti.
- API testleri izole PostgreSQL üzerinde çalıştı; normal kütüphane, profil ve
  değerlendirme akışları da tüm entegrasyon paketiyle doğrulandı.
- Yanlış yazar/başlık, onaysız yönlendirme, kapaksız/yabancı baskı, önce kapaksız
  sonra kapaklı baskı, geri alınmış onay, eski kayıt ve serbest ekleme denetlendi.
- Seed önizlemesi beş eseri sıraladı; ağ/veritabanı erişimi veya çıktı dizini oluşturmadı.

Sıradaki iş: Türk klasiklerini de kapsayan kontrollü listeyi genişletmek; mevcut
katalog sınıflandırmasını, gizli arşiv geçmişini ve keşif filtresini birlikte tamamlamak.
