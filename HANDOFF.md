# Handoff — durum ve devam noktası

Son güncelleme: 2026-07-30 · HEAD `42495cf`

Bu dosya, GDD'nin (`Assets/_Game/Art/References/Pixel_Flow_Clone_GDD.docx`) yazıldığı
andan bu yana olan **21 commit'lik** değişikliği kaydeder. GDD hâlâ oyunun tanımı ve
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
| "?" atıcı materyali | `Shooter.hiddenMaterial` alanı var, materyal **henüz yapılmadı**. Boşken atıcı normal materyalle çizilir, ammo yazısı yine de `?` gösterir |
| Testler | 17 test var (LaneRaycaster 7 + BoundedBuffer 10), bu dönemde koşturuldu, geçti |
| TMP Examples | Bilinçli duruyor — font çalışmaları için |
| README / development note | Yazılmadı |

---

## 4. Sıradaki iş

GDD §7.6'nın sırası şu şekilde güncellendi:

1. **"?" atıcı materyali** — tek kalan kısa iş. `M_ToonCube`'u çoğaltıp soru işareti
   verip `Shooter.prefab` → `Hidden Material` alanına bağlamak
2. **Android build + cihaz testi** (GDD A3-A4) — dört devlog'dur ertelenen en riskli
   kalem, yarım günden az
3. **8 yeni level + playtest** (E3-E4) — asıl kritik yol, 1.5-2 gün. Level Designer
   (`Tools/Level Designer`) hazır ve hızlı
4. **Juice kalanı** — D4 rescue kırmızı vinyet, D5 kazanma dalgası (kesilebilir)
5. **Gameplay videosu, README, development note** (A9-A11)

**Level 5-6 sandık, 7-8 "?" mekaniğine dayanıyor** — o yüzden 1. madde level
üretiminden önce gelmeli, yoksa o level'lar körlemesine tasarlanır.

---

## 5. Çalışma protokolü

CLAUDE.md'deki kurallar geçerli. Bu dönemde işe yarayan iki alışkanlık:

**Ölç, göz kararı geçme.** Renk, oran, glif kapsamı — hepsi referans görselden ya da
dosyadan programatik olarak ölçüldü. Lilita One'ın eksik glifleri, butonların
doygunluk artışı ve ray hızı hatası ancak böyle yakalandı.

**Sahnenin kaydedilip kaydedilmediğini kontrol et.** Unity'nin bellekteki hâli diskte
görünmez; birkaç kez eski veriye bakıp yanlış teşhis kondu. Hiyerarşi/Inspector
kontrolünden önce dosyanın `mtime`'ına bak.
