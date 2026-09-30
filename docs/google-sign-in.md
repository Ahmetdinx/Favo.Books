# Google ile kayıt ve giriş

Uygulama Google Identity Services düğmesini kullanır. Client Secret gerekmez.
Client ID yapılandırılmadığında Google düğmesi gizlidir; e-posta girişi devam eder.

## Google Cloud kurulumu

1. https://console.cloud.google.com/ adresinde Favo.Books projesini oluştur veya seç.
2. Google Auth Platform bölümünde uygulama bilgilerini (Branding), destek e-postasını
   ve iletişim bilgilerini doldur. Genel kullanıcılar için External hedef kitlesini seç.
3. Test durumundayken gerekiyorsa kullanacağın Google hesaplarını Test users listesine ekle.
4. Clients bölümünden OAuth client oluştur; Application type: Web application.
5. Authorized JavaScript origins alanına tarayıcıdaki frontend adresini ekle:
   örneğin `http://localhost:5173`. Port farklıysa gerçek portu kullan.
   Üretimde kendi HTTPS alan adını ayrıca ekle. Origin içine `/register` veya `/login`
   gibi yol yazma. localhost HTTP Google tarafından geliştirme için desteklenir;
   üretimde HTTPS zorunludur.
6. Bu uygulama popup + JavaScript callback kullanır; OAuth redirect URI eklemek gerekmez.
7. `.apps.googleusercontent.com` ile biten Client ID'yi al. Client Secret paylaşma.

## Yerel ayar

Depo kökünde, aşağıdaki örnek değeri kendi Client ID'n ile değiştir:

```powershell
dotnet user-secrets set "Google:ClientId" "CLIENT_ID.apps.googleusercontent.com" --project src/backend/Api/Favo.Books.Api
```

API'yi yeniden başlat; frontend'i `npm run dev` ile çalıştır. Kayıt ve giriş
sayfalarında resmi Google düğmesi görünür. Frontend ayrı bir Client ID ayarı istemez;
API'den yalnızca herkese açık Client ID'yi alır.

Üretim ayarı: `Google__ClientId`. Veritabanına Identity migration
`20260928063347_AddGoogleIdentity` uygulanmalıdır (yerelde uygulandı).
Refresh ve challenge çerezleri Secure/HttpOnly; localhost dışındaki test ortamında
HTTPS kullan. Vite `/api` proxy'si oturum isteklerini aynı origin altında tutar.
Popup engeli veya COOP sorunu varsa Google belgelerindeki
`Cross-Origin-Opener-Policy: same-origin-allow-popups` ayarını değerlendir.
CSP etkinleştirildiğinde resmi GIS script/frame/connect kaynaklarına izin ver.

## Davranış ve kontroller

- Sunucu Google imzasını, issuer/audience/süreyi, doğrulanmış e-postayı ve nonce'u denetler.
- Google hesabı e-postayla değil `sub` ile eşleşir; benzersiz veritabanı indeksi vardır.
- Yeni kullanıcı gizli profille oluşturulur; nickname seçmek için profile yönlendirilir.
- Aynı e-postalı mevcut hesaba otomatik bağlama yoktur. Mevcut parola girişi kullanılır;
  giriş yapılmış hesaptan Google bağlama ayrı bir gelecek geliştirmedir.
- Normal access-token/refresh-cookie oturum mekanizması kullanılır. Google credential
  localStorage'a, URL'ye veya uygulama loglarına yazılmaz.
- Giriş isteği beş dakika geçerli ve tek kullanımlıktır. Başarısız girişte Tekrar dene
  yeni challenge üretir. Challenge deposu bu ilk sürümde sunucu belleğindedir:
  çok instance yayına geçmeden önce ortak atomik depo veya sticky routing gerekir.
- Bu ilk sürüm mevcut hesaplar için parola kurtarma veya hesap bağlama eklemez.

Client ID olmadan imzalı fixture tokenlarıyla doğrulayıcı ve sahte Google doğrulayıcısı
ile veritabanı/oturum akışı test edildi. Gerçek Google popup akışı Client ID eklenince
ayrıca test edilmelidir: yeni kullanıcı, tekrar giriş, nickname seçimi, refresh,
çıkış, iptal edilen popup ve mevcut e-posta çakışması.

Resmi kaynaklar:
- https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid
- https://developers.google.com/identity/gsi/web/guides/verify-google-id-token
- https://developers.google.com/identity/gsi/web/guides/display-button
