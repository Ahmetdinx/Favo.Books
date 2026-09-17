# KitapKivo: roman keşfi ve sanal kütüphane

Karar tarihi: 2026-09-17. Durum: ürün kapsamı kabul edildi; aşağıdaki yeni
davranışlar henüz uygulanmadı. Bu belge önceki sosyal ürün planının yerini alır.

## Ürün sözü ve sınırlar

Okuduğun romanları ekle, nelerinden keyif aldığını değerlendir, sana uygun sıradaki
romanı bul. İstersen sanal kütüphaneni nickname ile paylaş.

- Yalnızca Türkçe baskısı ve o baskıya ait kapak bilgisi bulunan romanlar.
- Tarih araştırmaları, finans, kişisel gelişim, şiir, oyun ve öykü derlemeleri kapsam dışı.
  Tarihî romanlar kapsam içi: konu etiketi ile edebî biçim birbirinden ayrılır.
- Öncelik çok okunan Türk ve dünya klasikleri, ardından talep gören çağdaş romanlar.
  100 bin uzun vadeli katalog ölçeğidir; lansman için doldurulacak kota değildir.
- Yedi kriter ve mevcut ağırlıkları korunur. Kütüphane işlemi değerlendirmeyi gerektirmez.
- Her kullanıcıya kendi okuma geçmişi, değerlendirmeleri ve tercihleriyle öneriler.
- Takma adla profil arama, isteğe bağlı açık profil, okuma ve değerlendirme başarıları.
- Post, mesaj, sohbet, yorumlaşma, takip akışı ve herkese aynı öneri ana sayfası yok.
  Eski inceleme/yorum backlog'u kaldırılır. Mevcut kişisel notlar otomatik yayımlanmaz.
- Film/dizi, mobil uygulama ve gelişmiş sosyal özellikler bu yayın kapsamına girmez.

## Mevcut kod ile fark

Katalog, kişisel kütüphane, yedi kriter, tercih kaydı, öneri gerekçeleri ve öneriye
ilgi/gizleme geri bildirimi mevcut. Bunlar korunur.

SubjectCategoryMapper içindeki genel `fiction` eşlemesi roman kanıtı değildir.
Book modelinde doğrulanmış edebî biçim yok. Kayıt yalnızca e-posta/parola alır;
nickname, profil görünürlüğü, kamuya açık profil sorguları ve başarı sistemi yok.
Mevcut öneri sıralama puanı yüzde değildir; doğrudan 0–100 aralığına esnetilmeyecek.
Öneri sorguları tüm katalog ve değerlendirmeleri belleğe alıyor; sınırlı aday havuzu
ve önceden hesaplanan kitap özetleriyle değiştirilmesi gerekiyor.

## 1. Roman kataloğu ve güvenli geçiş

İlk geliştirme paketi:

1. Roman / roman değil / belirsiz ayrımı için sınıflandırma durumu, kaynak kanıtı,
   karar tarihi ve kural sürümü tasarla. Konu/tür sınıflandırmasını ayrı tut.
2. Mevcut 9.858 kitap üzerinde salt okunur rapor üret. Belirsizliği roman diye doldurma.
   Tarihî roman, öykü derlemesi, kurgu dışı, yalnızca fiction etiketli kayıtları test et.
3. Bilinen romanlardan küçük, elle kontrol edilmiş bir başlangıç kümesi oluştur;
   bu küme olmadan filtreyi açıp kataloğu boşaltma.
4. Sınıflandırmayı ve uygunluk filtresini katalog, arama, detay, kategoriler, öneri
   sözleşmesi ve yeni kütüphane ekleme yollarında birlikte uygula.
5. Daha önce eklenmiş uygun olmayan kitapların kullanıcı kayıtlarını koru; sahibine
   arşiv/kapsam dışı durumu göster, açık profil ve keşif yüzeylerinden çıkar.
   Fiziksel silme otomatik migration davranışı olmayacak.
6. Tüm yazma yollarında (manuel, external resolve, seed, background import)
   roman + kapaklı Türkçe baskı şartını merkezî olarak uygula. Belirsiz adaylar
   yayımlanmadan inceleme kuyruğunda kalsın. Mevcut kaydı yeniden kullanma yolu da
   uygunluğu atlayamasın.

Kabul: Konusu tarih olan bir roman kalır; tarih araştırması ve fiction etiketi
haricinde kanıtı olmayan kayıt kendiliğinden yayımlanmaz. Kullanıcı verisi kaybolmaz.

İçe aktarma sırası kürasyonla belirlenir. Klasik olma ile popüler olma ayrı alanlardır;
popülerlik kaynağı/tarihi kaydedilir, sağlayıcı sırası Türkiye okurluğu sayılmaz.
Eser düzeyinde tekilleştirme yapılır; farklı baskılar kitap sayısını şişirmez.
Şu an yeni toplu kitap ekleme istenmedi: durdurulmuş import yeniden başlatılmaz.

## 2. Katalogdan başlayan kolay deneyim

- Katalog kayıt olmadan gezilebilir; karttan okudum / okuyorum / okuyacağım işlemi.
- Üyelik gerekirse seçilen kitap ve işlem korunur, girişten sonra tamamlanır.
- İsteğe bağlı toplu geçmiş ekleme; her kitabın ayrıntı sayfasını açmak gerekmez.
- Okudum işaretlemek tek işlemdir; geri alma sunulur. Puanlama ayrı, isteğe bağlıdır.
- Yedi kriter kısa açıklamalarla sunulur; varsayılan sahte puan yok. Taslak korunur,
  fakat tamamlanmamış taslak ortalamaya ve başarı sayısına girmez.
- Başarı mesajları kısa ve kapatılabilirdir; her işlemde modal/animasyon yok.
  Hareket azaltma tercihi desteklenir.
- İlk değerlendirmeden sonra başarı ve öneriye geçiş görünür; yüzlerce değerlendirme
  giriş için ön koşul değildir. Çok veri daha ayrıntılı profil sağlayabilir, garanti değildir.

Kabul: Mobilde kullanıcı bir romanı detay açmadan kütüphanesine alır, geri alır;
değerlendirmeyi erteleyebilir ve taslağı kaybetmeden geri döner.

## 3. Nickname ve açık/gizli profil

Nickname kamuya açık kimliktir; gerçek ad, soyad, biyografi zorunlu değildir.
Mevcut e-posta/parola güvenli giriş ve kurtarma için korunur, e-posta yayımlanmaz.
Yalnız nickname bilmek hesabı kullanmaya yetmez.

- Identity benzersiz, normalize edilmiş nickname ve çakışma kontrolünü sahiplenir.
  Türkçe harf/büyük-küçük harf, URL ve ayrılmış ad kuralları uygulamadan önce belirlenir.
- Profiles görünürlük ve kamuya açık profil sunumunu sahiplenir. Varsayılan gizli.
- Mevcut kullanıcılar nickname seçmeye davet edilir; e-postadan nickname türetilmez.
- `/u/{nickname}` açık profil: roman rafları, yayımlanabilir kriter değerlendirmeleri,
  başarılar ve kitapların hangi yönlerinin beğenildiğini gösterir.
- Nickname araması yalnız açık profilleri, sayfalı ve sınırlı sonuçlarla döndürür.
- Gizlilik sunucuda uygulanır; direkt URL/API, arama, önbellek, rozet ve sıralamalar
  üzerinden özel profil verisi sızmaz. Gizliye dönüşte uygulama önbellekleri temizlenir.
- Açığa alma öncesi hangi alanların yayımlanacağı gösterilir. Mevcut kişisel notlar,
  e-posta, özel tercihler ve öneri geri bildirimleri bu kapsama dahil edilmez.
- Önceden paylaşılmış ekran görüntülerinin geri alınamayacağı anlaşılır biçimde belirtilir.

Kabul: Anonim ziyaretçi açık kütüphaneyi görebilir; gizli hesabın kitaplarını hiçbir
okuma uç noktasından alamaz. Görünürlük değişimi arama ve sıralamalara da yansır.

## 4. Başarılar ve isteğe bağlı okur köşesi

İlk sürüm önerisi: okunan farklı roman sayısı için 1/10/50/100/500/1000;
tamamlanmış farklı değerlendirmeler için 1/5/25/100/500/1000 eşikleri.
İsimler ve eşikler taslak ürün ayarıdır; kullanılabilirlik testiyle sonlandırılır.

- Okuma ve değerlendirme başarıları ayrı; profil sonraki eşiğe ilerlemeyi gösterir.
- Kullanıcı beyanı olan okuma sayısı doğrulanmış okuma gibi sunulmaz.
- Aynı eserin başka baskısı, yeniden ekleme, puan düzenleme yeni başarı sayılmaz.
- Geçerli benzersiz kayıtlar üzerinden yeniden hesaplanabilir; silme/durum değişimi
  sayacı ve ilgili başarıyı günceller, aynı eşikte bildirim tekrar tekrar gösterilmez.
- Rozet sayısı başkasının oy ağırlığını artırmaz; az değerlendirme yapanın oyu değersizleşmez.
- Günlük seri baskısı ve değerlendirme hızına ödül yok.
- En çok okuyan/değerlendiren alanı ana değer akışından sonra, yalnız açık profillerin
  ayrıca katılmayı seçmesiyle eklenebilir. Zaman aralığı, eşitlik sırası ve kötüye kullanım
  kuralları tanımlanmadan yayımlanmaz. İki aylık tarihte gerekirse ertelenir.

## 5. Kişisel öneriler ve uyum bantları

Genel kitap puanı (mevcut sabit ağırlıklar), kişisel eşleşme ve kanıt yeterliliği ayrı
değerlerdir. Kullanıcının tempo tercih etmesi kitabın kamuya açık puanını değiştirmez.
Yüksek tempo puanı akıcı bulmayı ölçer; otomatik olarak hızlı olay örgüsü demek değildir.

Girdiler: açık tür/kriter tercihleri, değerlendirmeler, beğenilen ve beğenilmeyen
romanlar, okuma durumları ve öneri geri bildirimi. Sadece okunmuş olmak beğeni sayılmaz.

Öneri bantları için taslak sınırlar:

| Eşleşme endeksi | Sunum |
| --- | --- |
| 95–100 | Mutlaka göz at |
| 80–95 altı | Kaçırmamanı öneririz |
| 60–80 altı | Şans verebilirsin |
| 60 altı | Ana öneri rafları dışında; ayrı keşif seçeneği olabilir |

Endeks beğenme olasılığı değildir. Formül, ağırlıklar ve eksik-veri davranışı
sürümlenir; yeterli kanıt olmadan sayı veya güçlü öneri etiketi gösterilmez.
Her kitabın gerekçesi ve kanıt yeterliliği ayrı sunulur. Bir oyla %95 iddiası yok.
Mevcut sıralama skorunu listedeki en yüksek puana bölerek yüzde üretmek yasaktır.

Yeni kullanıcı boş ekran görmez: seçtiği roman/türlere dayalı başlangıç önerileri
ve veri eksikliği açıklaması görür. Hiç seçim yapmadıysa ana sayfa seçim akışıdır.
Hiçbir kullanıcı yapay olarak her öneri bandını doldurmak zorunda değildir.
Kişisel raflar yazar/tür çeşitliliği sağlar; aynı kitap farklı raflarda tekrarlanmaz.
Okunmuş/okunmakta olan ve gizlenmiş romanlar önerilmez; yarım bırakma ve bekletme
durumları için açık politika/test gerekir. Okuma listesinde olanlar işaretlenir.

Ölçüm: öneriden listeye ekleme, okumaya başlama, daha sonraki değerlendirme,
gizleme oranı ve veri miktarına göre kalite. Eski değerlendirmeyi tahmin ederken
gelecekteki veriyi kullanmayan çevrimdışı kontrol ve küçük beta geri bildirimi.
1 ve 1000 değerlendirme arasındaki fark: görünür katkı/başarı, daha çok tercih kanıtı
ve daha çeşitli açıklamalar; doğruluk garantisi veya üstün oy hakkı değil.

## Sekiz haftalık teslim sırası

| Hafta | Teslim |
| --- | --- |
| 1 | Roman sınıflandırma raporu, kontrollü başlangıç kümesi, merkezi uygunluk tasarımı; 5 kullanıcıyla katalog akışı |
| 2 | Uygunluk geçişi, hızlı kütüphane işlemleri, kolay değerlendirme ve taslak; erken test ortamı |
| 3 | Nickname, profil gizliliği, açık profil/arama/paylaşım ve izolasyon testleri |
| 4 | Tekilleştirilmiş başarılar; 15–20 kişilik beta ve davranış ölçümü |
| 5 | Sınırlı öneri adayları, çeşitlilik, gerekçeler, kanıt yeterliliği; uyum endeksi doğrulaması |
| 6 | Kriter bazlı sıralama, veri kalite düzeltmeleri, hesap kurtarma, güvenlik ve yayın provası |
| 7 | 30–50 kişilik beta; E2E, erişilebilirlik, gerçekçi yük, yedekten geri dönüş |
| 8 | Kritik düzeltmeler, kontrollü halka açık beta; gerekirse sıralama köşesi sonraya |

Yaklaşık hedef 17 Kasım 2026. Kapasite yetmezse uyum yüzdesi yerine gerekçeli raflar
yayımlanır; rozet çeşitleri ve liderlik alanı ertelenir. Gizlilik/veri güvenliği ertelenmez.

## Yayın koşulları ve ilk büyüme

- Kayıt/kurtarma, kütüphane, değerlendirme, öneri ve açık/gizli profil E2E kontrolleri.
- Yönetim yetkileri, istek sınırları, güvenli oturumlar, sır yönetimi, CI ve migration provası.
- Yedek ve geri yükleme testi, veritabanı readiness, hata/maliyet izleme, destek kanalı.
- Mobil ve klavye erişilebilirliği, gizlilik/hesap silme süreçleri ve veri kullanım şartları.
- Beta kabul hedefi: 5 okurdan 4'ü yardım almadan roman ekleyip öneri kaydedebilir.
- Ana ölçüt: haftalık önerilerden roman kaydeden kullanıcı; sonraki okuma ve dört
  haftalık geri dönüşle birlikte izlenir. Rozet tıklaması ürün başarısı sayılmaz.
- İlk kullanıcılar roman kulüpleri ve klasik/roman odaklı küçük içerik üreticileriyle
  izinli denemelerden; açık kütüphane bağlantıları doğal paylaşım yolu olur.
  Kullanıcı tutunması görülmeden büyük reklam veya toplu katalog büyütmesi yok.

Bu plan uygulanırken modüller arası DbContext erişimi eklenmez. Catalog uygunluğu,
Reading durumları, Community değerlendirme/öneriyi, Profiles görünürlük/sunumu,
Identity giriş ve nickname benzersizliğini sahiplenir. Başarılar salt okunur modül
sözleşmeleriyle hesaplanır; modül sınırları mimari testlerle korunur.
