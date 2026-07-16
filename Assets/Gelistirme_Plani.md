# Pixel Flow-Esinli Prototip — İç Geliştirme Planı

**Amaç:** 20 iş gününde teslim edilebilir, brief'in tüm maddelerini karşılayan prototip.
**Bu doküman:** Kendi çalışma dokümanım. Mentora giden M1 planının detaylı, uygulama odaklı hali.
**Unity:** 6000.3.9f1 · URP 17.3 · Input System 1.18 · DOTween · TextMeshPro
**Repo stratejisi:** Yeni repo. `Mobile_Core`'daki `_MobileCoreScripts` klasörü (asmdef ile birlikte) kopyalanacak — submodule değil. Git geçmişi temiz başlar, commit'ler anlamlı olur.

---

## 1. Kilitlenen oyun modeli

### 1.1 Akış

```
Kuyruk (4 sütun, sadece tepeler tıklanabilir)
   │ tap → geri alınamaz taahhüt
   ▼
Konveyör / Ray (kapasite 5, dikdörtgen çevre, akıcı hareket)
   │ ilerlerken: bulunduğu şeritteki EN YAKIN küpe bakar,
   │ renk eşleşiyorsa kırar, ammo −1
   ▼
Tur tamamlandı
   ├─ ammo = 0  → despawn, ray kapasitesi açılır
   └─ ammo > 0 → Park'a iner (kapasite 5)
                    │ oyuncu istediği an geri yollayabilir
                    │ (rayda yer varsa)
                    ▼
Park dolu + rayda gelen atıcı var
   ├─ oyuncu parktan birini raya yollar (rayda yer VARSA) → kurtuldu
   └─ rayda da yer yok / yetişemedi → KAYIP
```

### 1.2 Kesin kurallar

| Kural | Detay |
|---|---|
| Seçim | 4 sütunun sadece en üstü tıklanabilir. Her an ≤4 seçenek. |
| Taahhüt | Tap geri alınamaz. Sonuç bir tur sonra gelir (commitment latency). |
| Ateş | Şerit boyunca raycast: ilk dolu hücre. Renk eşleşirse kır. Sandıksa dur (arkası o yönden kapalı). |
| Ammo | Her kırılan küp −1. 0 olunca atıcı sahneden çıkar. |
| Çıkış | Atıcı SADECE ammo=0 ile çıkar. Rengi board'da bittiyse çıkmaz — oturur, slot yer. Tuzak budur. |
| Tur | Ray uzunluğu ve hız sabit → oyuncu inişi öngörebilir. Refleks değil öngörü. |
| Park | Gelecek board durumuna bahis. Geri yollama ray kapasitesi gerektirir. |
| Kayıp | İki tampon aynı anda dolu + kurtarma penceresi kapandı. Tek sayaç değil, iki sayacın ilişkisi. |
| Kazanma | Board'daki tüm kırılabilir küpler temizlendi. |

### 1.3 Deadlock tespiti — O(1)

Board çözücüsü YOK. Kontrol tek nokta:

```
Atıcı turu bitirdi, ammo > 0:
  boş park slotu var mı?
    evet → yerleş
    hayır → kurtarma penceresi başlat (görsel/işitsel uyarı)
            pencere içinde slot açıldı → yerleş
            pencere kapandı → LOSE
```

Kurtarma penceresi süresi `LevelData`'dan ayarlanabilir (varsayılan ~1.5-2 sn, playtest ile).

---

## 2. Mobile_Core: taşınacaklar ve düzeltilecekler

### 2.1 Taşınmadan önce zorunlu düzeltmeler

| Dosya | Sorun | Düzeltme |
|---|---|---|
| `GameEvents` | `OnLevelCompleted/OnLevelFailed/OnEconomyChanged` için Trigger metodu yok → event'ler asla ateşlenemez | Her event'e Trigger metodu ekle. Bu olmadan win/lose paneli hiç açılmaz. |
| `GridManager<T>` | `GetValue` yok, bounds check yok, width/height private | `GetValue`, `IsInBounds(x,y)`, `Width/Height` property ekle. `GetXY`'ye bounds kontrolü. |
| `BasePanel` | `canvasGroup` Awake'te alınıyor → kapalı başlayan panelde NRE | Lazy init: `Show()` içinde null ise `GetComponent`. |
| `ObjectPooler` | Singleton null check yok, `ReturnToPool`'da `ContainsKey` yok, auto-expand `Peek().activeSelf`'e dayanıyor | Null check + `ContainsKey` guard. Expand mantığını sayaç tabanlı yap. |
| `ItemData` | `gridWidth/gridHeight/targetScore` item'a ait değil | Bu alanları sil; level konfigürasyonu `LevelData`'ya. |
| `LevelData` | Bu oyun için boş (id/name/icon/prefab) | Bölüm 4'teki şemaya göre yeniden yaz. |
| README | Türkçe | İngilizce'ye çevir — teslim edilen repo bu. |

### 2.2 Olduğu gibi taşınacaklar

`ISerializer` + JSON save, event-driven UI yapısı, DOTween panel animasyonları, klasör hiyerarşisi, asmdef.

### 2.3 Sıfırdan yazılacaklar (core'da yok)

- **`RaycastLane`** — mekaniğin kalbi (GridManager uzantısı)
- **`Track`** — çevre + float distance → (kenar, şerit, world pos)
- **`BoundedBuffer`** — tek sınıf, ray ve park için iki instance
- **`GameManager` / durum makinesi** — Loading → Playing → Won → Lost
- **`LevelManager`** — yükleme, retry, next, progress save entegrasyonu
- **Input soyutlaması** — Editor mouse + cihaz touch, tek arayüz
- **`AudioManager`** — placeholder sesler için basit tek nokta
- **HUD paneli** — UIManager'da yok

---

## 3. Sistem mimarisi

### 3.1 Sınıf sorumlulukları

| Sınıf | Sorumluluk | Bağımlılık |
|---|---|---|
| `GameManager` | Durum makinesi, win/lose kararı, kurtarma penceresi sayacı | GameEvents |
| `LevelManager` | LevelData yükleme, board kurulumu, retry/next, save | GameManager, SaveSystem |
| `BoardController` | `GridManager<CubeCell>` sahibi, `RaycastLane`, kalan küp sayacı | GridManager |
| `TrackController` | Atıcıların ray üzerindeki hareketi, distance→şerit eşlemesi, ateş tetikleme | BoardController, BoundedBuffer(ray) |
| `QueueController` | 4 sütun, tepe tıklama, "?" açılması, raya gönderme | BoundedBuffer(ray) |
| `ParkController` | 5 slot, yerleşme, geri yollama, doluluk sinyali | BoundedBuffer(park), TrackController |
| `Shooter` | Renk, ammo, isHidden, linkedCount; kendi görsel durumu | ObjectPooler |
| `CubeCell` | Renk / sandık bayrağı / boş | — |
| `UIManager` + paneller | Menu, HUD (level no, kalan küp, tampon dolulukları), Win, Lose, Settings | GameEvents |
| `InputRouter` | Tap algılama → QueueController / ParkController'a yönlendirme | Input System |

Kural: gameplay sınıfları UI'ı bilmez, her şey `GameEvents` üzerinden. Core'un event yapısı zaten buna uygun.

### 3.2 Kritik algoritmalar

**RaycastLane** (BoardController):
```
RaycastLane(int laneIndex, Direction dir, ColorId color):
  şerit boyunca kenardan içeri yürü
  ilk dolu hücre:
    sandık → return None (dur)
    renk == color → hücreyi kır, return Hit
    renk != color → return None
  şerit boş → return None
```

**Track eşlemesi:**
```
distance % perimeter → hangi kenar + kenar üzerindeki offset
offset → şerit indeksi (hücre genişliğine böl)
kenar → atış yönü (alt: yukarı, sol: sağa, üst: aşağı, sağ: sola)
```
Dikdörtgen olduğu için trigonometri yok, sadece aralık kontrolü + modulo.

**Ateş tetikleme:** her frame değil — atıcının şerit indeksi *değiştiğinde* bir kez `RaycastLane` çağır. Aynı şeritte beklerken sürekli ateş sorununu baştan engeller. (Tasarım sorusu: aynı şeritte birden fazla küp varsa tek geçişte kaç tane kırılır? Başlangıç kararı: şerit değişiminde 1 atış; playtest'e göre "şeritte durduğu sürece X ms'de bir" modeline geçilebilir. Bunu `LevelData`'ya koyma, global config yap.)

### 3.3 Görsel yapı

- Board: mantık 2D grid, küpler 3D mesh, ortografiğe yakın kamera. "Derinlik" hissi tamamen render.
- Atıcı hareketi: rayda akıcı (DOTween değil, `Update`'te distance artışı — DOTween'i park iniş/kalkış ve panel animasyonlarına sakla).
- Kırılma feedback'i: küp scale-down + partikül placeholder + ses placeholder. M4'te.

---

## 4. LevelData şeması

```csharp
[CreateAssetMenu]
class LevelData : ScriptableObject {
    int levelID;
    Vector2Int boardSize;          // ör. 16x16
    ColorId[] boardPixels;         // düzleştirilmiş grid; None = boş, Crate = sandık
    ColorId[] palette;             // bu levelda kullanılan 3-5 renk
    ShooterDef[] queue;            // sütun sırasına göre
    int trackCapacity = 5;
    int parkCapacity = 5;
    float trackSpeed;
    float rescueWindowSeconds = 2f;
}

struct ShooterDef {
    int column;                    // 0-3
    ColorId color;
    int ammo;
    bool isHidden;                 // "?" atıcı
    int linkedCount;               // 1 = normal, 2+ = bağlı
}
```

- Hiçbir level bilgisi script'te yaşamaz (brief'in açık şartı).
- Level tasarımı = piksel resim çizmek. Zorluk renk dağılımından doğar.
- Önce düz inspector; süre kalırsa küçük bir board-boyama editor penceresi (bölüm 7'deki "opsiyonel" listesinde).

---

## 5. Mekanik kapsamı

### 5.1 Kesin girenler

| Mekanik | Maliyet | Not |
|---|---|---|
| 4 sütunlu kuyruk | Core | Tek sıra = slot makinesi. Core fun'ın yarısı burada. |
| Ray (kapasite 5, akıcı) | Core | Oyunun imzası; kesilmez. |
| 5'lik park + geri yollama | Core | Kurtarma penceresi dahil. |
| Çift kilitli deadlock | Core | O(1) kontrol. |
| Sandık engeli | ~2 saat | Raycast'i durduran hücre. Level tasarımına bedava derinlik. |
| "?" atıcı | ~4 saat | `isHidden`; sütun tepesine gelince açılır. |
| Bağlı atıcı | ~1.5 gün | `linkedCount`; 2 slot yer. 3-4'lü veri seviyesinde bedava. |

### 5.2 Geliştirme sürecinde netleştirilecekler (açık)

Yılan tipi board engelleri ve power-up'lar (ekstra tepsi, elle seçme, karıştırma, renk patlaması). Core döngü çalışıp maliyeti netleşince karar verilecek. Takvim izin verirse en az biri girer; girmezse karar + gerekçe development note'a yazılır, sessiz düşürülmez.

### 5.3 Kapsam dışı (kesin)

Ekonomi/coin, meta progression, sezonluk içerik, canlı zorluk ayarı, reklam/IAP.

---

## 6. Level rampası (10 level)

| Level | Öğreten | İçerik |
|---|---|---|
| 1 | Tap → ateş → kazanma | Küçük board, 3 renk, bol ammo, baskı yok |
| 2 | Sıralamanın önemi | Bir renk gömülü; yanlış sırada park dolmaya başlar |
| 3 | Park + geri yollama | İlk kez park kullanmadan bitmeyen level |
| 4 | Tampon baskısı | Ray kapasitesi hissedilir; ilk gerçek deadlock riski |
| 5 | Sandık | Raycast'i kesen engeller; erişim yönü planlaması |
| 6 | Sandık + baskı | 5'in zoru |
| 7 | "?" atıcı | Belirsizlik altında risk yönetimi |
| 8 | "?" yoğun | Kuyruk okuma becerisi |
| 9 | Bağlı atıcı | 2 slot yiyen ikililer |
| 10 | Final | Her şey birlikte; buna rağmen çözülebilir ve adil |

Her level için tasarım kuralı: **kazanan bir sıralama kanıtlanabilir olmalı.** Kendi levelımı en az 3 kez baştan sona oynamadan "bitti" saymıyorum.

---

## 7. Takvim (20 iş günü)

### M1 — Gün 1-2: Analiz & Plan
- [x] Referans analizi (bu doküman + M1 planı)
- [ ] M1 planını mentora gönder, geri bildirim al
- [ ] Yeni repo aç, core scriptleri kopyala, Unity projesi kur (URP, Android module)
- [ ] Bölüm 2.1'deki core düzeltmelerini yap → **ilk anlamlı commit'ler buradan çıkar**

### M2 — Gün 3-7: Çekirdek döngü (tek level)
- Gün 3: `GridManager` uzantıları + `RaycastLane` + birim düzeyinde test sahnesi
- Gün 4: `Track` + atıcı hareketi + şerit değişiminde ateş
- Gün 5: `QueueController` (4 sütun, tap) + `BoundedBuffer`
- Gün 6: `ParkController` + geri yollama + deadlock/kurtarma penceresi
- Gün 7: Win/lose durumları, `GameManager` durum makinesi, elle kurulmuş 1 level uçtan uca
- **Checkpoint: Editor'de tek level tap→win ve tap→lose ile oynanabilir mi?**

### M3 — Gün 8-11: Level sistemi
- Gün 8: `LevelData` şeması + `LevelManager` + board'un veriden kurulması
- Gün 9: Sandık + retry/next akışı
- Gün 10: 3-4 test level, rampanın ilk yarısı
- Gün 11: "?" atıcı + save (mevcut level, `ISerializer` üzerinden)
- **Checkpoint: yeni level, koda dokunmadan eklenebiliyor mu?**

### M4 — Gün 12-15: UI & feedback
- Gün 12: Menu, HUD (level no, kalan küp, ray/park doluluk göstergeleri), result ekranları
- Gün 13: Kırılma feedback'i, kurtarma penceresi uyarısı, DOTween panel geçişleri
- Gün 14: Ses placeholder'ları (`AudioManager`), touch input cihaz testi
- Gün 15: Bağlı atıcı **veya** (erken bitmişse) 5.2'den bir eklenti
- **Checkpoint: oyun net ve tepkisel hissettiriyor mu?**

### M5 — Gün 16-20: Teslim (REZERVE — özellik girmez)
- Gün 16-17: 10 level tamamla, rampayı playtest et, ayarla
- Gün 18: Android build (APK/AAB), cihazda test, console error temizliği
- Gün 19: Gameplay videosu (≥3 level), README (EN), known issues listesi
- Gün 20: Development note (EN), son commit temizliği, submission checklist kontrolü

**Kayma kuralı:** M2-M4 taşarsa kesilen şey 5.1'in altındaki eklentiler ve 5.2, ASLA M5 değil. Eksik bağlı-atıcı beni batırmaz; eksik build/video batırır.

---

## 8. Teslim listesi (brief birebir)

- [ ] Git repo — tam Unity projesi, anlamlı commit'ler
- [ ] Android APK/AAB
- [ ] Gameplay videosu (≥3 level)
- [ ] Development note (EN) — şablon eşlemesi:
  - *Reference Understanding* → M1 planı bölüm 1 (sequencing, coupled buffers, commitment latency, park=bahis)
  - *Your Implementation* → bölüm 5 + neyi neden kestiğim (yılan/power-up kararı dahil)
  - *Architecture* → bölüm 3 + "üç parça: RaycastLane, Track, BoundedBuffer; gerisi veri"
  - *Problems Solved* → deadlock'un çözücü gerektirmemesi; şerit-değişiminde-ateş kararı; geliştirmede çıkanlar
  - *Known Issues* → dürüst liste
  - *Next Steps* → editor penceresi, kalan eklentiler, ekonomi katmanı
- [ ] Known bugs/limitations listesi
- [ ] Submission checklist'in her maddesi

## 9. Riskler

| Risk | Erken sinyal | Önlem |
|---|---|---|
| Ray hissi kötü (hız/ateş ritmi) | M2 sonunda oynanış "boş" geliyor | trackSpeed + ateş modeli config'te; playtest günü M2 içinde |
| Kurtarma penceresi ya çok kolay ya imkansız | L4 playtest | Süre LevelData'da; level bazında ayar |
| Bağlı atıcı 1.5 günü aşıyor | Gün 15 öğlen | Kes, nota yaz. 5.1'in ilk 6 satırı brief'i zaten karşılıyor |
| Android build sorunları (ilk kez bu sürümde) | — | Build'i gün 18'e bırakma; **gün 10 civarı bir kez boş build al** |
| Level 10 "zor" değil "haksız" | Kendi playtestimde çözemiyorum | Kanıtlanabilir sıralama kuralı; çözemediğim level teslim edilmez |
