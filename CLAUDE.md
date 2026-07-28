# Pixel Flow Clone — Claude Code bağlamı

Unity 6000.3.9f1 · URP 17.3 · Android portrait · Staj projesi.

## Önce bunu oku

**`Assets/_Game/Art/References/Pixel_Flow_Clone_GDD.docx`** — projenin tam tanımı.
Mekanik, mimari sözleşme (KURAL 1-11), renk paleti, ekran tasarımları, mevcut
durum, kalan iş planı ve teslim checklist'i orada. Bu dosya sadece bir özettir;
çelişki olursa GDD kazanır.

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

UI taban dokuları (pill/panel/circle/iconframe) henüz yok — şeffaflık sorunu
yüzünden yeniden üretilecek (GDD §4.3, doğrulama kontrol listesi). Bir görev
bu dosyaları gerektiriyorsa önce var olup olmadığı kontrol edilir, yoksa
üretim GDD §4.3'teki dört kısıtla (şeffaflık, nötr ton, 9-slice payı, boyut)
yapılır ve ölçülerek doğrulanır.

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

## Dil

Konuşma dili Türkçe. Mentora giden dokümanlar (README, development note) İngilizce.

## Editor işleri

Claude Code Unity Editor'ı süremez. Editor adımları **talimat olarak** verilir:
hangi menü, hangi alan, hangi değer — "ayarla" denmez, nasıl ayarlanacağı yazılır.
Kod tarafı (script, .meta import ayarları, prefab) Claude Code'un işidir.

## Acil durum

GDD §6.4'e göre **build şu an çalışmaz**: `EditorBuildSettings` var olmayan bir
sahneyi (`Assets/Scenes/SampleScene.unity`) işaret ediyor, gerçek sahne
`Assets/_Game/Scenes/MainScene.unity`. Ayrıca Android player settings hiç
yapılmamış (package name boş, orientation AutoRotation, scripting backend boş
ama ARM64 IL2CPP gerektiriyor). Bu, dört devlog'dur ertelenen en yüksek riskli
açık kalem — GDD §7.1'deki A1-A2-A3 yarım günden az sürer.
