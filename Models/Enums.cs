namespace GelirGiderTakip.API.Models
{
    /// <summary>
    /// Para birimi türleri
    /// </summary>
    public enum CurrencyType
    {
        TL = 0,
        USD = 1,
        EUR = 2,
        GBP = 3,  // İngiliz Sterlini
        CHF = 4,  // İsviçre Frangı
        JPY = 5,  // Japon Yeni
        AUD = 6,  // Avustralya Doları
        CAD = 7,  // Kanada Doları
        RUB = 8   // Rus Rublesi
    }

    /// <summary>
    /// İşlem tipi (Gelir veya Gider)
    /// </summary>
    public enum TransactionType
    {
        Income = 0,   // Gelir
        Expense = 1,  // Gider
        TransferOut = 2, // Giden Transfer
        TransferIn = 3   // Gelen Transfer
    }
}
