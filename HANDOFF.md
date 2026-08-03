# Handoff — durum ve devam noktası

Son güncelleme: 2026-08-03 · HEAD `0ac08a4` · etiket `m5-levels`

Bu dosya, GDD'nin (`Assets/_Game/Art/References/Pixel_Flow_Clone_GDD.docx`) yazıldığı
andan bu yana olan değişikliği kaydeder (toplam 77 commit). GDD hâlâ oyunun tanımı ve
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

### Level üretimi ve doğrulama

**10 level elle yapıldı, ama bir simülatörle doğrulanıyor.** `Tools/levelgen/sim.py`
oyunun kurallarını birebir taklit ediyor (lane peeling, tur, park, kapasiteler,
bitiş koşusunda sandık kalkması) ve üç farklı oyuncu politikasıyla level'ı oynuyor.
Bu, GDD'nin "her level ≥3 tam oynanışla kanıtlanmalı" şartının otomatik hâli.

Simülatör yazılırken çıkan asıl bulgu, zorluğun nereden geldiğiydi:
`TrackController` mermiyi **yalnızca isabette** harcıyor, yani ıskalar bedava.
Dolayısıyla zorluk "mermi yetmemesi" değil — atıcı turunu **tek turda**
bitiremezse parka düşüyor, park doluyken bir tur biterse anında kayıp.
Bunun sezgiye aykırı sonucu: **yüksek mermili atıcı kolay değil, zor.**

Bundan çıkan level tasarım kuralları:

- Renk başına ammo, küp sayısına **TAM eşit** olmalı. Eksikse level çözülemez;
  fazlaysa o rengi taşıyan atıcı hiç boşalmaz ve rayda/parkta sonsuza dek takılı
  kalır. `LevelData.OnValidate` ve Level Designer paneli ikisini de yakalıyor
  (fazlalık uyarısı Level_2'deki DarkGray +10 hatasından sonra eklendi).
- Sandık yerleşimi tehlikeli: bir renk dört yönden birden sandıkla kapanırsa,
  o renk ancak bitiş koşusunda açığa çıkar — ama bitiş koşusu sahada ≤5 atıcı
  kalmasını beklediği için kilitlenme oluşabiliyor.

**Level Designer'a "Kırp" eklendi.** Geniş açıp ortasını doldurmak yaygın bir
alışkanlık ama boş kenarlar board'un en-boy oranını bozuyor, o da board alanına
sığdırma hesabında hücreleri gereksiz küçültüyor (20×20 → 18×18 kırpınca küpler
%11 büyüyor).

**Kampanya — 10 level, hepsi elle yapıldı ve simülatörde doğrulandı:**

| Level | Board | Küp | Renk | Atıcı | Sandık | Not |
|---|---|---|---|---|---|---|
| L1 | 18×10 | 120 | 2 | 8 | — | öğretici |
| L2 | 16×15 | 240 | 4 | 12 | — | |
| L3 | 20×19 | 380 | 6 | 22 | — | 11 gizli "?" |
| L4 | 20×20 | 400 | 3 | 16 | — | 3 gizli |
| L5 | 24×24 | 560 | 6 | 21 | 16 | 3 gizli |
| L6 | 30×30 | 890 | 6 | 34 | 8 | |
| L7 | 16×16 | 200 | 6 | 12 | — | |
| L8 | 20×20 | 370 | 7 | 17 | 12 | sandık köşe kilidi bilinçli |
| L9 | 35×41 | 700 | 6 | 30 | — | piksel-art portre |
| L10 | 40×35 | 1400 | 7 | 44 | — | final, en yoğun |

Palet 19 renge çıktı: ilk 14'ü referanstan kalibre edildi, portreler için
`Pink · Orange · Flesh · Brawn · LightBrawn` sonradan elle eklendi (bunlar
kalibrasyondan geçmedi, doygunlukları yüksek).

### Görsel kalibrasyon (bu dönem)

**Küp yüzeyi düzdü, referansta değil.** Ölçüm: bizim küpün her pikseli aynı
(`[196 97 91]`), referansta yüzey yukarıdan aşağı ~%12 açılıyor. `_FaceGradient`
eklendi; ölçülen %12 bir **taban** çıktı, gözle 0.6'ya çekildi.

**Küpler arası çizgi kayboluyordu.** `cubeGap` bir yüzdeydi; hücre küçüldükçe
mutlak boşluk 1px altına düşüp anti-aliasing'de eriyordu (Level_10'da dikey boşluk
0.76px). Konturu kalınlaştırmak bunu **çözmez**: iki küp gerçekten değiyorsa her
birinin konturu komşusunun ön yüzeyine karşı derinlik testini kaybediyor.

Referansta iki farklı yoğunluk ölçüldü — çizgi ne sabit ne orantılı:

```
küp 20.0px -> dikiş 3.0px (%15.0)
küp 50.8px -> dikiş 5.0px ( %9.9)
```

Küp 2.5 kat büyürken dikiş 1.67 kat büyümüş. İki noktadan çözülen **afin model**
(taban + hücrenin yüzdesi) her iki uçta da tutuyor ve şu an kullanılan model bu.
Her iki terim de dünya birimi; kamera sabit dünya genişliğine oturtulduğu için
çözünürlükten bağımsız sabit ekran oranı veriyor.

**Kamera dik değil, -25° eğik** (ortografik). Referansın da ortografik olduğu
board'un üst ve alt sıralarındaki küp genişliklerini ölçerek doğrulandı: 22.2px vs
22.0px, yani perspektif yakınsaması yok.

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

**Havuzlar:** `Shooter` 28 · `Cube` 1450 · `Tracer` 12 · `Crate` 20

Cube havuzu 20'den 1450'ye çıktı: kampanyanın en yoğun board'u (Level_10) aynı anda
1400 küp gösteriyor ve havuz küçük kaldığında `ObjectPooler` her level açılışında
yüzlerce senkron `Instantiate` yapıyordu.

**Post-process:** `MainScene/GameplayVolumeProfile` — Bloom (eşik 1, şiddet 0.25),
Color Adjustments (+15 doygunluk, +8 kontrast), Tonemapping **Neutral**. ACES
bilinçli seçilmedi: referansa göre kalibre ettiğimiz paleti donuklaştırıyor.
Vinyet denendi ve **kaldırıldı** — üst HUD'u okunmaz hale getiriyordu, referansta da
yok (arka plan parlaklığı orada her yerde 84-97 aralığında düz).

---

## 3. Bilinen açıklar

| Konu | Durum |
|---|---|
| **README / development note** | **Yazılmadı** — teslimin en büyük eksiği |
| **Gameplay videosu** | Çekilmedi (en az 3 level isteniyor) |
| **Bilinen hatalar listesi** | Yazılmadı |
| **Android build** | APK alındı, arkadaşın cihazında oynandı, çalışıyor (A3-A4 bitti). Hafif FPS düşüşü gözlendi — ama o testten sonra board'lar 1400 küpe çıktı, tekrar ölçüm gerekiyor. Teslim paketine APK konmadı |
| **GPU instancing** | `M_ToonCube` hâlâ `m_EnableInstancingVariants: 0`. Board'lar 1400 küpe çıktı, kontur pass'i draw call'ı ikiye katlıyor. Cihazda bir kez hafif FPS düşüşü görüldü — ama o testten sonra board'lar büyüdü, yeniden ölçüm borçlu |
| `rescueWarningClip` | Boş — park uyarısı sessiz |
| `backgroundMusic` | Boş |
| `ToonCube_SG.shadergraph` | Ölü — hiçbir materyal kullanmıyor, silinebilir |
| TMP Examples | 6.2 MB, hâlâ duruyor. Font işi bittiği için artık gerekçesi yok |
| Testler | 17 test (LaneRaycaster 7 + BoundedBuffer 10). `BoundedBuffer.Clear()` eklendi, tekrar koşturulmalı |
| Simülatör vs. gerçek oyun | `sim.py` Level_5'i "3/3 politika kaybediyor" diyor ama elle rahat kazanılıyor. Bot bir **alt sınır**, mutlak doğru değil |

---

## 4. Sıradaki iş

1. **README + development note + bilinen hatalar** (A10-A11) — ödevin zorunlu
   teslim kalemleri, üçü de eksik. Bu dosyanın §1'i development note'un altı
   başlığından dördünü doğrudan besliyor
2. **Gameplay videosu** (A9) — en az 3 level. Instancing'den SONRA çekilmeli,
   yoksa takılmalı bir kayıt teslim edilir
3. **GPU instancing** — 1400 küplük board'larda artık ertelenebilir değil
4. **Temizlik**: TMP Examples sil, `ToonCube_SG` sil, testleri koştur
5. **Cihazda son bir tur** — instancing sonrası FPS doğrulaması

---

## 5. Çalışma protokolü

CLAUDE.md'deki kurallar geçerli. Bu dönemde işe yarayan iki alışkanlık:

**Ölç, göz kararı geçme.** Renk, oran, glif kapsamı — hepsi referans görselden ya da
dosyadan programatik olarak ölçüldü. Lilita One'ın eksik glifleri, butonların
doygunluk artışı ve ray hızı hatası ancak böyle yakalandı.

Ama ölçüm **hedef değil taban** verir: `_FaceGradient` için ölçülen %12 doğruydu,
gözle 0.6 çok daha iyi durdu. Ölçüm nereye bakılacağını söylüyor, ne kadar
gideceğini değil.

**Ölçümün sınırını da bil.** Video karesinden alınan referanslar (level6.png,
level200.png) JPEG sıkıştırması yüzünden aynı görselin farklı satırlarında %17-33
arası sonuç veriyordu — bu spread ölçümün kendi hatası. Telefon ekran görüntüleri
(sıkıştırmasız, 1179px) güvenilir dayanak; video kareleri sadece doğrulama için.

**Sahnenin kaydedilip kaydedilmediğini kontrol et.** Unity'nin bellekteki hâli diskte
görünmez; birkaç kez eski veriye bakıp yanlış teşhis kondu. Hiyerarşi/Inspector
kontrolünden önce dosyanın `mtime`'ına bak.
