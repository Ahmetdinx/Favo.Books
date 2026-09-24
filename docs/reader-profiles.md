# Okur kimliği ve açık profiller

2026-09-24 uygulama notları.

## Kullanım

- Yeni kayıtta e-posta ve parolanın yanında kullanıcı adı zorunludur.
- Kullanıcı adı 3–24 karakterdir; a-z ile başlar, a-z/0-9/alt çizgi içerir.
  Baştaki/sondaki boşluklar temizlenir, büyük harfler küçültülür. Türkçe
  karakterler bu ilk sürümde desteklenmez. Sistem adları ayrı tutulur.
- Mevcut hesaplar aynı kimlik ve kitaplıkla devam eder; profil ayarından ad
  seçmeleri istenir. E-postadan otomatik ad üretilmez.
- Profil varsayılan olarak gizlidir. Ayarlarda açık profil seçildiğinde
  hangi alanların paylaşılacağı gösterilir.
- Üst menüde logo ile Ana Sayfa arasındaki arama, en az iki karakterle açık profilleri arar; 250 ms bekleme ve iptal desteğiyle en fazla beş sonuç gösterir. Okurlar sekmesi kaldırıldı; eski /okurlar bağlantısı ana sayfaya yönlenir.
- Açık bağlantı: /okur/{nickname}. Kullanıcı adı değişirse eski bağlantı kapanır.
- Başucu kitabı, iz bırakanlar, sayfalı kitaplık ve sayfadaki kitapların yedi
  kriter puanları paylaşılır. E-posta, parola, kişisel not/refleksiyon,
  öneri tercihleri ve öneri geri bildirimleri paylaşılmaz.

## Teknik sözleşme

- GET /api/readers/me ve PUT /api/readers/me yalnız oturum sahibinin ayarlarını
  okur/değiştirir. İstekten userId kabul edilmez.
- GET /api/readers/?query=...&page=1 yalnız açık profilleri getirir;
  sayfa boyutu 20, hasMore ile sonraki sayfa belirtilir.
- GET /api/readers/{nickname}?page=1 kitaplığı 20 kayıtlık sayfalarla getirir.
  Gizli veya bulunamayan profiller aynı 404 sonucunu döndürür.
- Reader yanıtları Cache-Control: no-store kullanır. Açık profil ekranı sekmeye
  dönüldüğünde yeniden doğrular. Önceden görüntülenmiş/kopyalanmış içeriğin
  geri alınması mümkün değildir; canlı sunucu itme mekanizması bulunmaz.
- Identity kullanıcı adı ve görünürlüğü aynı kayıt üzerinde atomik değiştirir.
  Bu, önceki ürün planındaki görünürlüğü Profiles'ta saklama önerisinin
  uygulama kararıdır. Profiles vitrin içeriğini sahiplenmeye devam eder.
- API hostu modül servislerinin açık DTO'larını birleştirir; modüller diğer
  modüllerin DbContext/tablo türlerine erişmez.
- PostgreSQL benzersiz indeksi eşzamanlı kullanıcı adı taleplerini korur.
- Migration: 20260924065516_AddReaderIdentity. Yerelde uygulandı; diğer
  ortamlarda yeni API sürümünden önce Identity migration uygulanmalı.
- Eski hesaplarda Nickname null ve IsPublic false olur; geçmiş kayıt silinmez.

## Doğrulama ve kalan sınırlar

141 API entegrasyon testi ve yedi mimari testi geçti. Son ayrılmış-ad ve
sayfalama eklerinden sonra altı ReaderEndpoints testi tekrar geçti.
Frontend TypeScript/Vite build ve lint geçti.

Testler anonim erişim, gizliden açığa/açıktan gizliye geçiş, not ve e-posta
sızıntısı, nickname normalizasyonu/çakışması, eşzamanlı talep, isim değiştirme,
mevcut hesap uyumu, yetkilendirme, arama sayfalaması ve joker karakter
kaçışını kapsar.

Tarayıcı uçtan uca/görsel kontrolü tamamlanmadı. Başarılar, sıralamalar, toplu
kitap ekleme ve profil başına ayrı okuma-listesi gizlilik ayarları bu pakette yok.
Geçmiş uygun olmayan eserlerin özel arşive ayrılması katalog planındaki ayrı
iştir; mevcut kitaplık kayıtları aynen korunur.
