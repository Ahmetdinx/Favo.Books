# Okur ağı

Profil aramasından açılan herkese açık profiller takip edilebilir. Takip etmek için okurun kendi profilini de ayarlardan herkese açması gerekir; gizlilik ayarı otomatik değiştirilmez. Gizli profiller arama, bağlantı listeleri ve sosyal akışlarda gösterilmez. Profil yeniden açılırsa mevcut takip ilişkileri tekrar görünür.

Profilimde **Okur çevrem** bölümündeki takipçi/takip sayıları 20 kişilik sayfalarla listeyi açar. Ana sayfanın sol akışı takip edilenlerin halen `Reading` durumundaki kitaplarını, sağ akışı takipçilerin son puanlarını gösterir. Her akış en son güncelleme sırasıyla en fazla 12 kayıt döndürür. Okumadan çıkarılan veya puanı silinen kitap sonraki yenilemede akıştan çıkar. Sayfaya dönme, görünür sekmeye geçiş ve takip değişikliği akışları yeniler; sürekli canlı bildirim sistemi değildir.

Kimlik modülü ilişkileri `identity.reader_follows` tablosunda yönetir. Host, Reading ve Community modüllerinin toplu okuma modellerini birleştirir; modüller birbirinin tablolarını okumaz. Feed yalnızca nickname, avatar, kitap kimliği, tarih ve puan döndürür. E-posta, token ve özel notlar paylaşılmaz. İlişkiler kullanıcı kimliğine bağlı olduğundan nickname değişikliği ilişkiyi bozmaz. Tekil çift anahtar, kendini takip etmeyi engelleyen CHECK ve iki kullanıcıya bağlı foreign key bulunur. PUT/DELETE idempotenttir.

## Migration

Kitap repository kökünde, API kapalıyken:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/backend/Modules/Identity/Favo.Books.Modules.Identity --startup-project src/backend/Api/Favo.Books.Api --context IdentityDbContext
```

`20261001070334_AddReaderFollows` yalnızca yeni takip tablosunu ve indeksini ekler. Avatar kimlikleri aynı kaldığı için kayıtlı seçimler için veri dönüşümü gerekmez.

## Entegrasyon kontrolü

Docker Desktop açıkken Kitap repository kökünde:

```powershell
dotnet run --project tests/Integration/Favo.Books.SocialChecks
```

Bu çalıştırılabilir kontrol projesi ayrı, geçici PostgreSQL container kullanır. Gerçek veritabanına bağlanmaz. 26 kontrol; yetkisiz erişim, gizlilik, kendini takip, eşzamanlı/idempotent takip, yönlü ilişkiler, takipçi listeleri, sayfalama, iki akışın yönü, özel alanların dışlanması ve takipten çıkış kalıcılığını kapsar. `dotnet test` keşfiyle çalışmaz.

İsteğe bağlı `-- --serve` test API'sini 5118 üzerinde açık tutar. Yalnızca test ortamında kullanılmalıdır. Ctrl+C test sunucusunu kapatır ve geçici container'ı temizler.

Doğrulama: backend build, frontend build/lint, 26 PostgreSQL API kontrolü; tarayıcıda takip et/çık, takipçi listesi, avatar kaydetme/yenileme, 390px mobil akış görünümü ve taşma kontrolü.
