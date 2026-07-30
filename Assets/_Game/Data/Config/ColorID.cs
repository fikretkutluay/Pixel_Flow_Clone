namespace Game
{
    public enum ColorId
    {
        None,
        Crate,
        Red,
        Blue,
        Green,
        Yellow,
        Purple,

        // Kontrast uçları ve ara tonlar. Referans board'unun yarısından fazlası
        // koyu bir kütle, geri kalanı parlak vurgu — resmin okunmasını sağlayan
        // o aralık. İlk altı renk orta bantta sıkışıktı (V 0.71-0.83), bunlarla
        // 0.12-0.95'e açıldı.
        //
        // HEPSİ SONA eklendi: önceki id'ler kaymıyor, mevcut level'lar bozulmuyor.
        // Bir renk SİLİNMEMELİ, sonrasındaki her id kayar ve tüm level'lar bozulur.
        Navy,
        White,
        Khaki,
        Maroon,
        DarkPurple,
        DarkGray,
        LightGray,
        Black
    }
}