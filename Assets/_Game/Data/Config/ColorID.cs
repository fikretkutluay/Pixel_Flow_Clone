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

        // Kontrast uçları. Referans board'unun yarısından fazlası koyu bir
        // kütle, geri kalanı parlak vurgu — resmin okunmasını sağlayan o
        // aralık. Bizde bütün renkler orta bantta sıkışıktı (V 0.71-0.83).
        // SONA eklendi: önceki id'ler kaymıyor, mevcut level'lar bozulmuyor.
        Indigo,
        White
    }
}