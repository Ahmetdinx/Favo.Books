# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Favo Kitap; Türkçe baskısı olan romanlar için kişisel keşif ve paylaşılabilir sanal kitaplık uygulamasıdır. Kitap dosyası barındırmaz. Gönderi, mesajlaşma ve yorum dizileri kapsam dışıdır. Bu depo aynı zamanda tüm Favo uygulamalarının hesap kaynağıdır.

## Komutlar

```powershell
dotnet build Favo.Books.slnx
dotnet test Favo.Books.slnx                                                                        # Docker gerekir
dotnet test Favo.Books.slnx --filter 'FullyQualifiedName!~Favo.Books.Api.IntegrationTests'         # Docker olmadan
dotnet test tests/Modules/Catalog/Favo.Books.Modules.Catalog.UnitTests --filter 'FullyQualifiedName~TestAdi'
dotnet run --project tests/Integration/Favo.Books.SocialChecks                                     # dotnet test bulmaz
dotnet run --project src/backend/Api/Favo.Books.Api --launch-profile http
```

Ön yüz (`src/frontend`):

```powershell
npm ci
npm run dev
npm run build
npm run lint
npm run test:ratings
npm run test:recommendations
npm run test:catalog
```

Migration ekleme ve uygulama her modül için ayrı yapılır; API açılışta migration çalıştırmaz. `<M>` yerine `Identity`, `Catalog`, `Reading`, `Profiles` veya `Community` yazılır:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet ef migrations add Ad --project src/backend/Modules/<M>/Favo.Books.Modules.<M> --startup-project src/backend/Api/Favo.Books.Api --context <M>DbContext
dotnet ef database update --project src/backend/Modules/<M>/Favo.Books.Modules.<M> --startup-project src/backend/Api/Favo.Books.Api --context <M>DbContext
```

Yapılandırma user-secrets üzerindendir: `ConnectionStrings:Database` ve `Jwt:Secret` zorunludur. `TreatWarningsAsErrors` açıktır. Entegrasyon testleri geçici PostgreSQL 17 kapsayıcısı kullanır, geliştirme veritabanına dokunmaz. Windows'ta uzun yollar için klonlarken `core.longpaths=true` gerekir.

## Mimari

Modüler monolit: tek API, tek PostgreSQL, modül başına şema ve `DbContext`. Kurallar `ARCHITECTURE.md` ve `docs/decisions/` içindedir.

- Modüller `src/backend/Modules` altındadır ve yalnızca `Favo.Books.SharedKernel` projesine referans verebilir; bunu `tests/Architecture` zorlar. Modül başka modülün `DbContext`, şema veya tablosuna erişmez.
- Modüller arası senkron bağımlılık SharedKernel arayüzleriyle kurulur (`IBookCatalog`, `IDiscoveryCatalog`, `ICompletedBooks` vb.); uygulaması sahibi modüldedir.
- Birden çok modülün verisini birleştiren okumalar API host'unda yapılır: `ReaderEndpoints`, `SocialEndpoints`, `ReaderAchievements`.
- Modül içi düzen dikey dilimlidir: `Application/<Alan>/<UseCase>` ve `Presentation/<Alan>/<UseCase>`. Her modül `Add<M>Module` ve `Map<M>Endpoints` ile bağlanır; handler'lar DI'a elle kaydedilir.
- Generic repository yoktur, EF Core doğrudan kullanılır. Eşzamanlılık gereken yazmalarda ham SQL ile `ON CONFLICT` veya advisory lock kullanılır.
- Beş bağlam da aynı `public."__EFMigrationsHistory"` tablosunu paylaşır.
- Rotalar `/api/identity`, `/api/catalog`, `/api/reading`, `/api/profiles`, `/api/readers` biçimindedir. Belgelerdeki `/api/v1` henüz uygulanmamış bir hedeftir.

### Katalog kuralları

- Katalogda yalnızca `NovelEligibility.EligibleNovels()` koşulunu sağlayan kitaplar görünür: en son editoryal karar `Novel` ve Türkçe baskıda kapak adresi var.
- Serbest kitap ekleme kapalıdır; `POST /api/catalog/books` 422 döner. Kitaplar yalnızca `tools/` altındaki çevrimdışı araçlarla ve `data/curation` manifestleriyle girer.
- `PublisherImport` ve `ReadingListImport`, `--apply` için manifest hash'i eşleşen ve son 24 saat içinde üretilmiş doğrulama kaydı ister. Doğrulama araçlarının ham kaynak sayfaları `artifacts/` altındadır ve Git'e girmez.
- Aynı batch kimliği farklı içerikle yeniden kullanılamaz. Editoryal düzeltme yeni karar eklenerek yapılır, geçmiş yazılmaz.
- Open Library tek uygulanmış dış sağlayıcıdır; sağlayıcı tipleri Catalog dışına çıkmaz.

### Kimlik ve ön yüz

- Erişim JWT'si 15 dakikadır ve bellekte tutulur. Refresh token HttpOnly çerezde, `/api/identity` yoluna bağlıdır; rotasyon ve tekrar kullanımda aile iptali vardır.
- Katalog yönetim uçları `AuthorizationPolicies.CatalogAdmin` politikasını ister; JWT `role=admin` iddiası `identity.users."IsAdmin"` alanından gelir. Rol verme ucu yoktur, SQL ile verilir (README, Administrators).
- `KitapKivo` adlı JWT issuer/audience, çerez adı ve user-secrets kimliği uyumluluk için bilerek korunur; değiştirmek mevcut oturumları ve yerel ayarları bozar.
- Gizli profiller arama, liste ve akışlarda görünmez; takip için takip edenin profili de herkese açık olmalıdır.
- Ön yüz hem `/` hem `/kitap/` tabanında çalışır; yönlendirme ve paylaşım linkleri `import.meta.env.BASE_URL` kullanmalıdır.
- `src/frontend/src/auth/sessionTransport.ts`, Favo ve Favo.Movie ile birebir aynı kalmalıdır.

## Belgeler

`PROJECT_CONTEXT.md` tarih sıralı oturum notlarıdır ve eski yollar içerir. Kapsam için `docs/novel-product-plan.md` esastır.
