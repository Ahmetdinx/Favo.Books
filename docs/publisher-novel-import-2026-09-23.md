# Yayınevi roman aktarımı: 23 Eylül 2026

**Tamamlandı: 1.000 yeni roman eklendi.** Tamamında kapaklı Türkçe baskı var.
Toplam kitap sayısı 9.858'den 10.858'e, katalog koşullarını sağlayan roman sayısı
5'ten 1.005'e çıktı. Mevcut kayıtlar ve kullanıcı geçmişi korundu.

1.228 doğrulanmış adaydan 227 tekrar/mevcut kayıt ayrıldı; kalan 1.001 eserin
1.000'i tek transaction ile eklendi. Aynı batch'in sonraki önizlemesi mevcut
1.000 kimliği tanıdı, sıfır yeni eser bildirdi ve toplamlar değişmedi.
Manifest, doğrulama özeti ve eklenen seçki `data/curation/publisher-novels-20260923`
altında. Batch: `publisher-novels-20260918`.

## Tamamlanan geliştirmeler

- Yayınevi ürün sayfası, yazar, ISBN ve kapak bilgilerini toplayan devam
  edilebilir araçlar; kaynak HTML dosyaları yalnızca yerel `artifacts/` altında.
- Kaynak karması, yayınevi, edebi tür çelişkileri, kapak yanıtı ve görsel boyutu
  kontrolü. Kapak önbelleği 24 saatten eskiyse yeniden kontrol edilir.
- Kaynakla bağlı inceleme kararları:
  `data/curation/publisher-form-reviews-v1.json`. Şiir, deneme, toplu eser,
  Türkçe baskısı belirsiz kayıtlar bekletilir. Yazar biyografisindeki kelimeler
  yüzünden yanlış elenen romanlar eser açıklaması incelenerek ayrılır.
- Gizli kayıtlar dahil tüm veritabanında ISBN, kaynak adresi ve normalleştirilmiş
  başlık kontrolü. Cep/ciltli baskılar, bilinen alternatif başlıklar ve 1Q84'ün
  ayrı ciltleri yeni eser sayısını şişirmez.
- Tam hedefe ulaşılmıyorsa hiç yazmayan, tek transaction ile çalışan ve aynı
  aktarım kimliği tekrar kullanıldığında ikinci bir grup eklemeyen aktarım aracı.
- Doğan kapakları katalogda yayınevinin 250 piksel küçük görsel adresiyle sunulur.

Klasik dizilere öncelik verilir. Bu liste ölçülmüş satış veya okunma sıralaması
değildir. Alt tür, özgün dil ve baskı biçimi konusunda kaynakta olmayan bilgi
üretilmez. Kapak kontrolü otomatik erişim/boyut kontrolüdür; her kapağın insan
tarafından görsel incelendiği anlamına gelmez.

## Doğrulama

112 Catalog birim testi, Release aktarım aracı derlemesi ve Python sözdizimi
kontrolü geçti. Önceki çalışmada 134 API entegrasyon testi, dört frontend katalog
testi ve frontend derlemesi geçti; son kimlik değişikliklerinden sonra Docker
testlerinin tekrar çalıştığı iddia edilmez.

Eklenen kimliklerin 1.000'i de farklı; seçilen baskıların tamamı `tur` dil koduna
ve kontrol edilmiş HTTPS kapak adresine sahip. Veritabanı toplamları ve merkezi
Catalog uygunluk sorgusu üzerinden sonuç doğrulandı. Yerel API 5117 portundaki
okuma kontrolünde erişilebilir değildi; tarayıcı üzerinden son kontrol yapılmadı.

## Yeniden çalıştırma

Bu iş tamamlandı; başka 1.000 kitap eklemek için batch kimliğini değiştirmeyin.
Aynı aktarımı kontrol etmek için aynı manifest, hedef ve batch ile önizleme alın.
Yeni bir yazma çalışmasında kaynak/kapak doğrulaması güncel olmalıdır.

Komut sözleşmesi ve güvenlik sınırları için
`tools/Favo.Books.PublisherImport/README.md` dosyasına bakın. Git'e kod gönderilmesi
veritabanındaki kayıtları başka ortama taşımaz.
