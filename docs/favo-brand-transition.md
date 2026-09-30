# Favo marka geçişi

Güncel durum: GitHub deposu artık `Ahmetdinx/Favo.Books`; API üzerinden aynı depo
kimliği korunarak yeniden adlandırıldığı doğrulandı ve origin güncellendi.
Aşağıdaki eski GitHub erişim engeli notları önceki kontrolün tarihçesidir.
Yerel ana klasör henüz taşınamadı: açık bir süreç dizini kilitli tutuyor.
Codex, Visual Studio ve bu klasördeki terminaller kapatıldıktan sonra üst dizinden
`powershell -ExecutionPolicy Bypass -File .\KitapKivo\Complete-FavoRename.ps1`
çalıştırılmalı. Ardından Codex'te Favo.Books klasörü, Visual Studio'da
Favo.Books.slnx açılmalı. Yeni Start-Favo.ps1 başlatıcısı konuma bağımlı değildir.

30 Eylül 2026 — Ana marka kullanıcı tarafından **Favo** olarak seçildi.

## Adlandırma

- Ana marka ve gelecekteki ana uygulama: **Favo**.
- Kitap uygulamasının görünen adı: **Favo Kitap**.
- Kitap çözümü, namespace ve assembly öneki: `Favo.Books`.
- Ön yüz paket adı: `favo-books`.
- GitHub deposu hedef adı: `Favo.Books` (henüz değiştirilmedi).
- Diğer alanlar: Film, Dizi, Anime, Çizgi Roman / Manga; henüz oluşturulmadı.

## Uygulanan değişiklikler

Gezinme, giriş/kayıt metinleri, ana sayfa marka referansları, sayfa başlığı ve
Türkçe belge dili güncellendi. .NET çözümü, projeler, namespace'ler, testler,
ithalat/bakım araçları ve çalıştırma yönergeleri yeni adları kullanıyor.

## Mevcut verilerle uyumluluk

Marka değişikliği veri taşıma işlemi değildir. Şu teknik tanımlayıcılar bilinçli
olarak korunur:

- JWT issuer/audience, refresh cookie ve Google nonce cookie adları.
- Değerlendirme taslakları ve başarı kutlamalarının tarayıcı depolama anahtarları.
- API `UserSecretsId`, bağlantı bilgileri ve veritabanı şemaları.
- Compose konteyner/volume adları ve mevcut yerel çalışma klasörü.
- Kaynak doğrulama hash'leri içeren `data/curation` dosyaları ve sonuç raporları.

EF migration kimlikleri ve tablo adları değişmedi; CLR tip adları yeni namespace'e
taşındı. Yeniden adlandırma için veritabanına migration uygulanmaz.
`kitapkivo` kullanıcı adı rezervasyonu korunurken `favo` da ayrılmıştır.

Yerel çalıştırma:

```powershell
dotnet build Favo.Books.slnx -c Release
dotnet run --project src/backend/Api/Favo.Books.Api --launch-profile http
```

Eski çalışan API süreci yeni kaynakları kullanmaz; yeni proje yolu ile yeniden
başlatılmalıdır. Ön yüz komutları ve portlar değişmedi.

## GitHub adımı

Bağlı GitHub araçlarında depo yeniden adlandırma işlemi yok; GitHub CLI kurulu
değil ve erişilebilen tarayıcıda GitHub oturumu açık değil. Bu nedenle GitHub
deposu hâlâ `Ahmetdinx/KitapKivo`; `origin` değiştirilmedi.

GitHub Settings → General → Repository name alanında `Favo.Books` adıyla yeniden
adlandırma tamamlanıp yeni adres doğrulandıktan sonra:

```powershell
git remote set-url origin https://github.com/Ahmetdinx/Favo.Books.git
git ls-remote origin HEAD
```

Çalışma klasörünü yeniden adlandırmak bu işlem için gerekli değildir. Daha sonra
taşınacaksa Compose proje adı ve mevcut volume bağlantısı önceden doğrulanmalıdır.

## Sıradaki adımlar

1. GitHub yeniden adlandırmasını tamamla.
2. Slogan, logo ve uygulama simgesini tasarla. “Bir sonraki favorini bul.” henüz
   bir öneridir; son tasarım kararı verilmedi.
3. Favo ana uygulamasında ortak giriş ve beş içerik alanının seçim ekranını kur.
4. Kitap uygulamasını mevcut hesapları koruyarak ortak kimlik sistemine bağla.
5. Kitap uygulaması için ayrıntılı canlıya hazırlık planını oluştur.
6. Diğer içerik projelerini sırayla geliştir.

Favo için alan adı sahipliği ve marka kullanılabilirliği bu kod değişikliğiyle
doğrulanmış değildir.

## Doğrulama

- Çözümün tüm projeleri Release yapılandırmasında derlendi; mimari ve modül testleri geçti.
- İlk API test turunda 160 testin 155'i geçti. Beş oturum testindeki eski çerez
  adını koruması gereken beklentiler düzeltildi. IdentityEndpointsTests ve
  ReaderEndpointsTests grupları yeniden çalıştırıldı: 20/20 başarılı.
- Test veritabanlarında beş modülün mevcut migration'ları yeni namespace ile uygulandı.
- Yedi katalog/ithalat/bakım aracı sıfır hata ve uyarıyla derlendi.
- Ön yüz üretim derlemesi, lint ve 18 mevcut ön yüz testi geçti.
- `data/curation` ve Compose dosyası değişmedi. Gerçek kullanıcı verisine yazılmadı.
- Tarayıcı üzerinden uygulama akışlarının görsel kontrolü yapılmadı.
- Commit/push ve GitHub depo adı değişikliği yapılmadı.

## Visual Studio düzeltmesi (30 Eylül 2026)

Eski çözüm Visual Studio'da açık kaldığından IDE, silinen `KitapKivo.slnx`
dosyasını eski proje yollarıyla yeniden kaydetmişti. Bu hem eksik proje hatalarına
hem de kök klasörde `dotnet build` için MSB1011 (birden fazla çözüm) hatasına neden oldu.
Kullanıcı Visual Studio'yu kapattıktan sonra eski çözüm ve `.vs` önbelleği
`artifacts/favo-vs-repair-*` altına yedeklendi. Kök klasörde yalnızca
`Favo.Books.slnx` bırakıldı. 21 projenin referansları doğrulandı; eksik referans yok.
Debug ve Release derlemeleri sıfır hata, sıfır uyarı ile tamamlandı.

Visual Studio'da Recent Projects içindeki eski KitapKivo kaydı yerine
`Favo.Books.slnx` açılmalı. .NET 10 için güncel Visual Studio kullanılmalı;
yerel çalışma klasörünün KitapKivo adını taşıması çözümün adı değildir.
