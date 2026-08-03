# Pixel Flow Clone — Claude Code bağlamı

Unity 6000.3.9f1 · URP 17.3 · Android portrait · Staj projesi.

## Önce bunu oku

**`HANDOFF.md`** — projenin GÜNCEL durumu, alınan kararlar ve sıradaki iş.
Oturuma buradan başla.

**`Assets/_Game/Art/References/Pixel_Flow_Clone_GDD.docx`** — projenin tam tanımı.
Mekanik, mimari sözleşme (KURAL 1-11), renk paleti, ekran tasarımları ve teslim
checklist'i orada.

Çelişki durumunda öncelik: **HANDOFF.md → GDD → bu dosya.** GDD'nin §6 (mevcut
durum) ve §7 (kalan iş) bölümleri eskidir; onların yerini HANDOFF.md alır. Geri
kalan bölümleri — özellikle mimari sözleşme ve palet — hâlâ bağlayıcıdır.

Referans görseller `Assets/_Game/Art/References/` altında iki klasörde:

```
Assets/_Game/Art/References/
├── Pixel_Flow_Clone_GDD.docx
├── inGameResources/          (gameplay ekran görüntüleri)
│   ├── Ekran Resmi 2026-07-27 - 23.12.08.png
│   ├── Ekran Resmi 2026-07-28 - 17.45.16.png
│   └── slotlardoldu sıradaki atıcı geldiği anda lose.PNG
└── uiResources/               (menü/panel referansları)
    ├── anamenupaneli(level seçmiyoruz yanlış anlaşılmasın oynaya basınca sıradaki level açılıyor.).PNG
    ├── anamenudeki magaza paneli (ana menudeki altınların yanındaki + ikonu veya dogrudan kendisine tıklayınca acılır.).PNG
    ├── ayarlarpaneli.PNG
    ├── losepanel.PNG
    ├── winpanel.PNG
    └── profilpaneli.PNG
```

## Python araçları

İki ayrı araç seti var, ikisi de Pillow kullanıyor:

**`Tools/uigen/`** — UI sprite'ları Figma'dan değil buradan çıkıyor (55 sprite,
`Assets/_Game/Art/Sprites/UI/`). Figma MCP bu hesapta ayda 6 çağrıyla sınırlı ve
bir oturumda tükeniyor.

```
python generate_all.py && python icons.py && python avatars.py
python import_to_unity.py     # Assets'e kopyalar + .meta ayarlarını yazar
```

Detaylar ve GDD §4.3'ten sapmanın gerekçesi: `Tools/uigen/README.md`

**`Tools/levelgen/`** — level okuma/yazma ve doğrulama:

| Dosya | İş |
|---|---|
| `levelio.py` | `LevelData.asset` oku/yaz, board'u ASCII çiz |
| `sim.py` | Level'ı oyunun kurallarıyla oyna — kazanılabilir mi, park ne kadar doluyor |
| `quantize.py` | Görseli palete kuantala (üretilen level'lar terk edildi, ölçüm için hâlâ kullanışlı) |
| `build.py` | Toplu üretim — **artık kullanılmıyor**, level'lar elle yapılıyor |

`sim.py` bir **alt sınır** verir, mutlak doğru değil: Level_5'i "kaybediliyor"
diyor ama elle rahat kazanılıyor. Bot bir sıralamayı kaçırabiliyor.

## Bağlayıcı kurallar

- **Katman ayrımı:** Core → Data → Gameplay → UI. Gameplay UI'ı ASLA bilmez;
  iletişim yalnızca `GameEvents` üzerinden (KURAL 6). Editor kodu Editor-only
  assembly'de (KURAL 11).
- **Veri odaklı:** Level'a dair hiçbir bilgi script'te yaşamaz (KURAL 1).
  Sihirli sayı yok — level'a özgü → `LevelData`, oyun geneli → `GameConfig` (KURAL 2).
- **Over-engineer etme:** DI container, Addressables, abstract factory, async/await
  mimarisi YOK. Yeni soyutlama "rule of two" olmadan eklenmez. (GDD §2.3)
- **Commit:** `type: kısa açıklama` — İngilizce, tek satır.
  Tipler: `feat` / `fix` / `chore` / `refactor` / `docs`.
- **Sapma kaydı:** Plana/GDD'ye aykırı her karar gerekçesiyle bildirilir;
  development note'a girer.
- **Doğrulama:** Üretilen asset'ler ÖLÇÜLEREK doğrulanır, gözle bakılıp geçilmez.
  Özellikle UI dokularında: şeffaflık (alpha kanalı), nötr ton, 9-slice payı, boyut.
  Ama ölçüm **taban** verir, hedef değil: `_FaceGradient` için ölçülen %12 doğruydu,
  gözle 0.6 çok daha iyi durdu.
- **Level verisi:** Renk başına ammo, küp sayısına **TAM eşit** olmalı. Eksikse
  level çözülemez, fazlaysa o rengin atıcısı hiç boşalmaz ve sahnede takılı kalır.
  `LevelData.OnValidate` ve Level Designer paneli ikisini de yakalıyor.

## Dil

Konuşma dili Türkçe. Mentora giden dokümanlar (README, development note) İngilizce.

## Editor işleri

Claude Code Unity Editor'ı süremez. Editor adımları **talimat olarak** verilir:
hangi menü, hangi alan, hangi değer — "ayarla" denmez, nasıl ayarlanacağı yazılır.
Kod tarafı (script, .meta import ayarları, prefab) Claude Code'un işidir.

## Sık düşülen tuzak

**Sahne kaydedilmiş mi kontrol et.** Unity'nin bellekteki hâli diskte görünmez.
Hiyerarşi veya Inspector değeri okumadan önce
`Assets/_Game/Scenes/MainScene.unity` dosyasının `mtime`'ına bak — bu dönemde
birkaç kez eski veriye bakılıp yanlış teşhis kondu.

## Level tasarımı

10 level var, **hepsi elle yapıldı** ve `sim.py` ile doğrulandı. Level'lara
dokunma — üretici script'ler (`build.py`) terk edildi.

Zorluğun nereden geldiği sezgiye aykırı: `TrackController` mermiyi **yalnızca
isabette** harcıyor, ıskalar bedava. Yani zorluk "mermi yetmemesi" değil — atıcı
mermisini **tek turda** bitiremezse parka düşüyor, park doluyken bir tur biterse
anında kayıp. Sonuç: **yüksek mermili atıcı kolay değil, zor.**

Level Designer'da **Kırp** butonu var: board'u dolu bölgeye daraltır. Boş kenarlar
en-boy oranını bozup hücreleri gereksiz küçültüyor.

## En riskli açık kalem

**Teslimin üç zorunlu kalemi eksik:** README, development note (İngilizce, 6
başlıklı şablon) ve gameplay videosu. Kod tarafı hazır; eksik olan dokümantasyon.

`HANDOFF.md` §1 development note'un altı başlığından dördünü doğrudan besliyor —
oradan başla, sıfırdan yazma.

İkinci risk: **GPU instancing hiç açılmadı** ve board'lar 1400 küpe çıktı. Video
çekmeden önce halledilmeli, yoksa takılmalı bir kayıt teslim edilir.
