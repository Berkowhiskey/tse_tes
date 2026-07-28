using ClosedXML.Excel;

namespace TES.Infrastructure.Services;

/// <summary>
/// Toplu stajyer ekleme için boş .xlsx şablonu üretir (ClosedXML). Yalnız başlık satırı vardır;
/// örnek veri satırı KONULMAZ ki yanlışlıkla içe aktarılmasın. İkinci sayfa talimatları içerir.
/// </summary>
public static class StajyerSablonu
{
    public const string SayfaAdi = "Stajyerler";

    /// <summary>Sütun başlıkları — sıra hem şablonu hem okuyucuyu (A..H) belirler.</summary>
    public static readonly string[] Basliklar =
    [
        "Ad",
        "Soyad",
        "T.C. Kimlik No",
        "RFID Kart No",
        "Staj Başlangıç",
        "Staj Bitiş",
        "Okul",
        "Bölüm"
    ];

    public static byte[] Uret()
    {
        using var workbook = new XLWorkbook();
        var sayfa = workbook.Worksheets.Add(SayfaAdi);

        for (var i = 0; i < Basliklar.Length; i++)
        {
            var hucre = sayfa.Cell(1, i + 1);
            hucre.Value = Basliklar[i];
            hucre.Style.Font.Bold = true;
            hucre.Style.Fill.BackgroundColor = XLColor.FromHtml("#fb2c36");
            hucre.Style.Font.FontColor = XLColor.White;
        }

        sayfa.SheetView.FreezeRows(1);
        // Tarih sütunlarını gün.ay.yıl biçiminde göster (kullanıcı tarih yazınca netlik).
        sayfa.Column(5).Style.DateFormat.Format = "dd.MM.yyyy";
        sayfa.Column(6).Style.DateFormat.Format = "dd.MM.yyyy";
        sayfa.Columns(1, Basliklar.Length).AdjustToContents();

        Talimatlar(workbook);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void Talimatlar(XLWorkbook workbook)
    {
        var sayfa = workbook.Worksheets.Add("Talimatlar");
        var satirlar = new[]
        {
            ("Talimatlar", ""),
            ("", ""),
            ("Ad", "Zorunlu."),
            ("Soyad", "Zorunlu."),
            ("T.C. Kimlik No", "Zorunlu. 11 haneli rakam. Yalnızca ilk geçici parola olur; sistemde SAKLANMAZ."),
            ("RFID Kart No", "Zorunlu. Benzersiz olmalı; başka stajyere atanmış olamaz."),
            ("Staj Başlangıç", "Zorunlu. Tarih (GG.AA.YYYY)."),
            ("Staj Bitiş", "Zorunlu. Başlangıçtan sonra olmalı."),
            ("Okul", "Opsiyonel."),
            ("Bölüm", "Opsiyonel."),
            ("Amir", "Bu şablonda amir sütunu YOKTUR. Stajyerler amirsiz eklenir; amir sonradan 'Eşleştir' ekranından atanır."),
            ("Not", "Yalnızca 'Stajyerler' sayfası içe aktarılır. Başlık satırını değiştirmeyin.")
        };

        for (var i = 0; i < satirlar.Length; i++)
        {
            sayfa.Cell(i + 1, 1).Value = satirlar[i].Item1;
            sayfa.Cell(i + 1, 2).Value = satirlar[i].Item2;
        }

        sayfa.Cell(1, 1).Style.Font.Bold = true;
        sayfa.Column(1).Style.Font.Bold = true;
        sayfa.Columns(1, 2).AdjustToContents();
    }
}
