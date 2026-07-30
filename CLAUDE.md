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

## UI asset üretimi

UI sprite'ları Figma'dan değil, **`Tools/uigen/`** altındaki Python (Pillow)
generator'ından çıkıyor — 55 sprite `Assets/_Game/Art/Sprites/UI/` altında.
Yeni bir sprite gerekiyorsa oraya bak; Figma MCP bu hesapta ayda 6 çağrıyla
sınırlı ve bir oturumda tükeniyor.

```
python generate_all.py && python icons.py && python avatars.py
python import_to_unity.py     # Assets'e kopyalar + .meta ayarlarını yazar
```

Detaylar ve GDD §4.3'ten sapmanın gerekçesi: `Tools/uigen/README.md`

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

## Sık düşülen tuzak

**Sahne kaydedilmiş mi kontrol et.** Unity'nin bellekteki hâli diskte görünmez.
Hiyerarşi veya Inspector değeri okumadan önce
`Assets/_Game/Scenes/MainScene.unity` dosyasının `mtime`'ına bak — bu dönemde
birkaç kez eski veriye bakılıp yanlış teşhis kondu.

## En riskli açık kalem

**Android build hâlâ hiç alınmadı.** Build sahne listesi ve player settings
düzeltildi (GDD A1-A2 bitti), geriye APK alıp cihaza kurmak kaldı (A3-A4).
Dört devlog'dur erteleniyor ve GDD §9.5 bunu "asla kesilmeyecekler" listesine
yönelik doğrudan tehdit olarak işaretliyor.
