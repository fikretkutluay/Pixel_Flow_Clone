# Handoff — durum ve devam noktası

Son güncelleme: 2026-07-30 · HEAD `07be446`

Bu dosya, GDD'nin (`Assets/_Game/Art/References/Pixel_Flow_Clone_GDD.docx`) yazıldığı
andan bu yana olan **25 commit'lik** değişikliği kaydeder. GDD hâlâ oyunun tanımı ve
mimari sözleşmesi için birincil kaynak, ama **§6 (mevcut durum) ve §7 (kalan iş)
bölümleri artık geçersiz** — onların yerine bu dosya geçer.

---

## 1. GDD'den bu yana ne değişti

### Mekanik kararları

**Kurtarma penceresi kaldırıldı.** GDD §1.6'daki `rescueWindowSeconds` sistemi
referans oyunda yok. Park doluyken bir atıcı turunu tamamlarsa **anında kayıp**.
`rescueTimers`, `rescueWindowSeconds` ve `OnRescueEnded` silindi.

**Yerine uyarı atımı geldi.** Park slotları iki durumda kısa süre kırmızı yanıp
söner: (a) park dolduğu anda, (b) park doluyken bir atıcı turunun son
`warnLapFraction`'ına (%22) girdiğinde. İkincisi asıl olan — oyuncunun slot
boşaltmak için elindeki pencere tam olarak orası. Sürekli yanıp sönme denendi ve
başarısız oldu: göz kısa sürede alışıp uyarıyı görmez oluyor.

**Ray hızı board boyutundan bağımsızlaştı.** Mesafe hücre biriminde sayılıyor ama
ray dünyada sabit bir dikdörtgen, dolayısıyla büyük board aynı fiziksel turu daha
çok "hücre" sayıyor ve görsel hız düşüyordu (Level_2 bunu elle iki kat hızla telafi
ediyormuş). Artık hız **tur süresi** olarak yazılıyor ve `Init`'te çevriliyor:

```
baseSpeed = path.Perimeter / trackLapSeconds
```

Perimeter sadeleştiği için her level aynı görsel hızda akıyor. Bu hızı level'a özgü
olmaktan çıkardığı için `GameConfig`'e taşındı, `LevelData.trackSpeed` silindi.

> **GDD §7.5'teki E1 ve E2 bu yüzden listeden düştü.** Standart board boyutuna gerek
> yok (ray sabit, board içine oturuyor, küpler küçülüyor) ve Level Designer'a tur
> süresi göstergesi de gerekmiyor — telafi tuzağı ortadan kalktı.

**Baskı hızlanması.** Ray + park toplamı `tensionShooterThreshold`'a (7) ulaşınca ray
`tensionSpeedMultiplier` (1.25×) hızlanıyor, `tensionRampSeconds` içinde yumuşayarak.
Bilerek **rastgele değil**: GDD §1.4 oyuncunun iniş anını öngörebilmesine dayanıyor,
rastgele hız o hesabı bozar ve kaybı adaletsiz hissettirir.

**Bitiş koşusu.** Kuyruk + ray + park toplamı `endgameShooterThreshold`'a (5) inince
kaybetmek imkânsız hâle geliyor. O anda: atıcılar park etmeyi bırakıp turlamaya devam
ediyor, parktakiler raya geri salınıyor, ray `endgameSpeedMultiplier` (1.6×)
hızlanıyor ve sandıklar animasyonla kalkıyor (bir şeridi kapatmanın anlamı kalmadı).

**Bitiş koşusu artık parkı ve kuyruğu boşaltmıyor.** İlk hâli, eşiğe inildiğinde
parktaki atıcıları otomatik raya salıyordu. Oyuncunun elinde kalan son nişan alma
kararını da alıyor ve level'ı kendi kendine bitiriyordu. Artık **sadece o an rayda
olanlar** turlamaya devam ediyor; park ve kuyruk oyuncuda kalıyor. Parktaki atıcıya
dokunmak zaten raya yolluyor (`ParkController.HandleTap`), dolayısıyla kimse kilitli
kalmıyor.

**Bağlı atıcı (linkedCount)** GDD §6.3'teki kararla kesilmiş durumda, değişmedi.

### Görsel sistem

**UI dokuları koddan üretiliyor.** `Tools/uigen/` altında Python + Pillow ile
parametrik üretim. `Assets/_Game/Art/Sprites/UI/` altındaki **55 sprite** oradan
çıkıyor. Yeniden üretim:

```
python generate_all.py    # panel, buton, pill, circle, iconframe, ribbon
python icons.py           # 24 ikon
python avatars.py         # 9 avatar
python import_to_unity.py # Assets'e kopyalar + .meta import ayarlarını yazar
```

`import_to_unity.py` mevcut `.meta`'lara dokunmaz (Unity'nin bastığı sprite ID'leri
korunur) ve GUID'leri asset yolundan türetir, yani tekrar çalıştırmak referansları
bozmaz.

**GDD §4.3'ten sapma — ölçümle gerekçeli.** GDD "her şey nötr gri + tint" diyordu.
Referansı ölçünce buton tonlarının **doygunluk artışıyla** koyulaştığı görüldü
(sarı `#F9D160`→`#F7B92A`, yeşil `#67EF77`→`#16E651`). Unity'nin çarpımsal tint'i tüm
kanalları eşit ölçekler — yani parlaklığı düşürür, doygunluğa dokunamaz; tam tersi
işlem. Bu yüzden **panel butonları ön-renklendirilmiş** ürünler. HUD öğeleri nötr
kalıp runtime'da tint'leniyor, çünkü referans da onları level temasına göre yeniden
renklendiriyor.

**Gölgelendirme elle yazılmış bir shader'a taşındı.** `ToonCube_SG.shadergraph`
yerini `Assets/_Game/Art/Shaders/ToonCube.shader` aldı (**GDD'den sapma**, gerekçe:
kontur için ters-kabuk gerekiyor ve Shader Graph tek graph'tan iki pass üretemiyor.
Alternatifi her renderer'a ikinci materyal yuvası açmaktı — daha çok parça, daha çok
prefab dokunuşu. Ayrıca `.shadergraph` JSON'unu elle düzenlemek gözden geçirilemez).
Eski graph repoda duruyor ama artık hiçbir materyal kullanmıyor; silinebilir.

Üç davranış referans ekran görüntülerinden **ölçülerek** kalibre edildi:

| | Formül | Referansta ölçülen |
|---|---|---|
| Turuncu gövde gölgesi | (140, 102, 53) | (158, 99, 58) |
| Işık bandı tepesi | (255, 255, 166) | (255, 255, 174) |
| Beyaz gövde kenarı | 0.53 | 135/255 = 0.53 |

Kritik olan: gölge **grileşmiyor, doygunlaşıyor** (nötr çarpım bunu veremez), ışık
bandı beyaza değil **sarıya** kayıyor ve toplamsal, kenarlar parlamıyor
**koyulaşıyor**. Eski graph kenarlara sıcak fresnel *ekliyordu* — yıkanmış
görüntünün sebebi buydu.

**Kontur** ters-kabuk, `SRPDefaultUnlit` pass'i. Derinlik itmesi clip-space'te
`UNITY_REVERSED_Z`'ye bakılarak yazılıyor; render state'teki `Offset` ters-Z'de
yanlış yöne çalışıp gövdelerde siyah benek bırakıyordu. Sert normalli mesh'lerde
(küp, sandık) kabuk normal yerine konumdan şişiriliyor, yoksa köşelerde yırtılıyor.

**Küp yüzeyleri artık düz değil.** Ölçüm: bizim küpün her pikseli `[196 97 91]`
iken referansta yüzey aşağı doğru %12 açılıyor (kahverengi küp 139→156, mor küp
77→87). Yan yana dizilen düz saf renk alanları "cırtlak" okunmasının ana sebebiydi.
`_FaceGradient` eğri gövdelerde 0, yalnızca board parçalarında açık.

**Palet referansa göre yeniden kalibre edildi — GDD §4.1'in ham hex'leri geçersiz.**
İki ayrı fark ölçüldü:

- *Doygunluk*: referans atıcı gövdeleri S ≈ 0.52, bizimkiler 0.57-0.92 (ort 0.72).
- *Parlaklık aralığı*: referans board küpleri V 0.42-1.00'e yayılıyor ve alanın
  yarısından fazlası koyu bir kütle; bizimkiler 0.71-0.83'te sıkışıktı. Referans
  sakinliğini parlaklığı kısarak değil, **aralık** açarak elde ediyor.

Bunun için `ColorId`'ye **sona** iki giriş eklendi: `Indigo` (#3C2F70, koyu kütle)
ve `White` (#E9EDF2, parlak vurgu). Sona eklendiği için önceki id'ler kaymadı,
mevcut level'lar bozulmadı. Yayılım 0.12 → 0.51 (referans 0.58). Ayırt edilebilirlik
kontrol edildi: en yakın çift CIELAB'da dE 36.4, karışma eşiğinin çok üstünde.

> `Indigo`'yu arka plan kütlesi olarak kullanan bir level'ın kuyruğunda **Indigo
> atıcı** da olmalı — küp ancak eşleşen renkle kırılıyor. Referans da böyle yapıyor.

Palet üç yerde kopyalıydı ve kalibrasyondan sonra ikisi eskide kaldı;
`LevelDesignerColors` oyunda görünmeyen renkleri gösteriyordu. Artık doğrudan
`ColorPalette` asset'ini okuyor.

**"?" atıcı deseni koddan üretiliyor.** `Tools/uigen/pattern_hidden.py` → 512×512
seamless, 16 rastgele döndürülmüş "?" glifi (Baloo 2 ExtraBold), beyaz-üzerine-şeffaf;
tint shader'da, yani tek doku her gövde rengine hizmet ediyor. Kaplama %26.6
(referansta ölçülen %26.1), dikiş gradyanı iç gradyandan düşük.

Ayırt edici olan **renk değil desen** — palet ileride koyu yeşil/siyah atıcılarla
büyüyünce nötr bir gövde rengi "?" ile karışırdı. Gövdeye **object-space triplanar**
projeksiyonla uygulanıyor: `Cubic_Dog.fbx` ikili ve UV'leri doğrulanamıyor,
triplanar UV istemiyor; object space olması atıcı ateş yönüne dönerken desenin
gövdede sabit kalmasını sağlıyor.

**Font: Baloo 2 ExtraBold** (GDD §4.2 Lilita One diyordu). Lilita One'ın TTF'i 225
glif içeriyor ve **Ğ ğ İ Ş ş yok** — cmap tablosundan doğrulandı. Türkçe arayüzde
kabul edilemez. Baloo 2'de 856 glif, tam destek. `OFL.txt` repoda.

**Maskot.** Projedeki `Cubic_Dog` karakteri 2B işarete çevrildi; 9 avatar (aynı
maskot, 9 renk, 3 ifade) ve Oyna sekmesi ikonu ondan geliyor. Referans oyunun kendi
karakterleri kopyalanmadı.

### Juice

Hepsi DOTween, `ShooterAnimator` / `CubeView` / `Tracer` bileşenlerinde:

- Raydan parka **zıplayarak** iniş (`DOJump` + iniş ezilmesi)
- Parktan raya **esneme** — pozisyon tween'i **yapılamaz**, ray o atıcının konumunu
  her karede kendisi yazıyor
- Kuyrukta öndeki gidince arkadakiler **dalga hâlinde** ilerliyor
- Küp kırılma: şişip **Z ekseninde** burkularak sıfıra çöküyor (Y dönüşü kamera Z'ye
  baktığı için ezilme gibi okunuyordu)
- Mermisi biten atıcı kendi ekseninde dönüp küçülerek yok oluyor
- **Tracer**: ağızdan çıkan beyaz top, arkasında atıcının renginde iz. Küp anında
  kırılıyor (kazanma kontrolü sunum beklemez), sadece kırılma animasyonu merminin
  varışına geciktiriliyor
- **Chevron**: oklar rayda fiziksel olarak akıyor, dünya yay uzunluğuna göre eşit
  dizili. Distance ekseninde dizilseydi board'a göre ok sayısı değişirdi
- **"?" açılması**: kuyruk tepesinde desen sönerken sıklığı da düşüyor, yani
  glifler büyüyerek dağılıyor — "üstünden kalkıyor" gibi okunuyor. Gövde rengi
  aynı anda gerçek renge geçiyor, `ShooterAnimator.PunchReveal()` eşlik ediyor.
  Materyal MPB ile sürülüyor, `sharedMaterial` animasyon boyunca gizli materyalde
  kalıyor; tween hem `OnDisable`'da hem `Init`'te öldürülüyor (havuz güvenliği)

**Küp oranı.** `BoardController` artık prefabın `localScale`'ini **ezmiyor, çarpıyor**
— `1:1:2` yazan bir prefab her board boyutunda o oranı koruyor. Ayrıca eninden derin
parçalar kameraya doğru kaydırılıyor, yoksa yarısı board düzlemine gömülü kalıyor.

---

## 2. Sahne yapısı

```
MANAGERS          Object Pooler · GameManager · LevelManager · InputRouter · AudioManager
CONTROLLERS       TrackController · BoardController · QueueController · ParkController
                  TrackRail (TrackRailAnchor) · TrackChevrons
Canvas            UIManager
  MenuPanel       TopBar (profil/can/altın/ayarlar) · PlayButton · TabBar
  HudPanel        SettingsButton · LevelPill · CubePill · TrackPill · PowerUpBar
  WinPanel        Body → Ribbon · RewardBox · NextLevel · (INACTIVE)
  LosePanel       Body → Ribbon · BrokenHeart · ReasonText · RetryLevel · CloseButton (INACTIVE)
  SettingsPanel   Body → Header · Rows (3) · Buttons (4) · VersionText (INACTIVE)
  StorePanel      OverlayPanel — gövdesi bilinçli boş (GDD 5.7) (INACTIVE)
  ProfilePanel    9 avatar ızgarası + isim + Kaydet — **AKTİF kalmalı**
  LoadingScreen   Canvas'ın SON çocuğu olmalı
Main Camera       CameraFitter · LayoutGizmos
```

**İki incelik:**

- `ProfilePanel` sahnede **aktif** başlar. `Awake`'in açılışta çalışması gerekiyor ki
  menüdeki profil butonu kayıtlı avatarı göstersin; kendini `Start`'ta animasyonsuz
  gizliyor. Diğer overlay'ler pasif başlar.
- `LoadingScreen` kendini **deaktive etmez** — sonraki geçişi dinlemeye devam etmesi
  gerekiyor.

**Havuzlar:** `Shooter` 20 · `Cube` 20 · `Tracer` 12 · `Crate` 20

---

## 3. Bilinen açıklar

| Konu | Durum |
|---|---|
| **Android build** | Hiç alınmadı. `EditorBuildSettings` ve player settings düzeltildi (A1-A2 bitti), APK adımı duruyor |
| **Cihazda touch testi** | Yapılmadı |
| **Level sayısı** | **2/10** — Level_1 (10×10), Level_2 (20×20, 9 sandıklı) |
| `rescueWarningClip` | Boş — park uyarısı sessiz |
| `backgroundMusic` | Boş |
| `Indigo` / `White` kullanan level | Yok. Palet aralığı açıldı ama Level_1/2 hâlâ sadece canlı renklerden oluşuyor, kazanç ancak yeni level'larda görünür |
| `ToonCube_SG.shadergraph` | Ölü — hiçbir materyal kullanmıyor, silinebilir |
| Testler | 17 test var (LaneRaycaster 7 + BoundedBuffer 10), bu dönemde koşturuldu, geçti |
| TMP Examples | Bilinçli duruyor — font çalışmaları için |
| README / development note | Yazılmadı |

---

## 4. Sıradaki iş

GDD §7.6'nın sırası şu şekilde güncellendi:

1. **Android build + cihaz testi** (GDD A3-A4) — dört devlog'dur ertelenen en riskli
   kalem, yarım günden az. Ayrıca konturun gerçek cihazdaki draw call maliyeti
   ancak burada ölçülebilir
2. **8 yeni level + playtest** (E3-E4) — asıl kritik yol, 1.5-2 gün. Level Designer
   (`Tools/Level Designer`) hazır ve hızlı. Artık `Indigo`/`White` de var: koyu
   kütle + parlak vurgu kurgusu referansın okunabilirliğinin temeli
3. **Juice kalanı** — D4 rescue kırmızı vinyet, D5 kazanma dalgası (kesilebilir)
4. **Gameplay videosu, README, development note** (A9-A11)

**Level 5-6 sandık, 7-8 "?" mekaniğine dayanıyor** — ikisi de artık görsel olarak
hazır, o level'lar körlemesine tasarlanmayacak.

---

## 5. Çalışma protokolü

CLAUDE.md'deki kurallar geçerli. Bu dönemde işe yarayan iki alışkanlık:

**Ölç, göz kararı geçme.** Renk, oran, glif kapsamı — hepsi referans görselden ya da
dosyadan programatik olarak ölçüldü. Lilita One'ın eksik glifleri, butonların
doygunluk artışı ve ray hızı hatası ancak böyle yakalandı.

**Sahnenin kaydedilip kaydedilmediğini kontrol et.** Unity'nin bellekteki hâli diskte
görünmez; birkaç kez eski veriye bakıp yanlış teşhis kondu. Hiyerarşi/Inspector
kontrolünden önce dosyanın `mtime`'ına bak.
