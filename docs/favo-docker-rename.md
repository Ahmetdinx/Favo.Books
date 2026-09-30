# Favo Kitap yerel altyapı adları

30 Eylül 2026:

- Docker Compose projesi: `favo-kitap`
- PostgreSQL konteyneri: `favo-kitap-postgres`
- Aktif veri volume'u: `favo-kitap-postgres-data` (external)
- İstenen yerel klasör: `C:\Users\ahmet\source\repos\Favo Kitap`
- Çözüm dosyası ve GitHub deposu: `Favo.Books` (değişmedi)

Veritabanı adı ve bağlantı bilgileri değişmedi. Önce pg_dump özel format yedeği
alındı; eski PostgreSQL durdurulduktan sonra volume içerikleri sahiplik ve izinleri
korunarak kopyalandı. Yeni konteyner healthy. 28 tablonun tüm kayıt sayıları aynı;
11.047 kitap ve 5 kullanıcı korunuyor. Eski konteyner ve Compose ağı kaldırıldı.

Geri dönüş için eski `kitapkivo_kitapkivo-postgres-data` volume'u ve
`artifacts/favo-docker-rename/database.dump` korunuyor. Eski volume aktif değil.
İki veritabanını aynı anda aynı portta başlatmayın.

Sıfırdan kurulumda: `docker volume create favo-kitap-postgres-data`, ardından
`docker compose up -d --wait postgres`. Mevcut kurulumda volume zaten hazır.

Yerel klasör açık süreç kilidi nedeniyle henüz taşınamadı. Codex ve proje
klasöründeki diğer uygulamalar kapandıktan sonra üst dizinden:

```powershell
cd C:\Users\ahmet\source\repos
powershell -ExecutionPolicy Bypass -File .\KitapKivo\Complete-FavoRename.ps1
```

Betik klasörü `Favo Kitap` adına taşır; hedef mevcutsa üzerine yazmaz. Yeni
klasörü Codex'te tekrar açın. Siteyi `Start-Favo.ps1` ile başlatabilirsiniz.
