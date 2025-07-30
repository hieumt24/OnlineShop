using System.Globalization;

namespace OnlineShop.Utils;

public static class CurrencyConverter
{
    public static decimal ConvertToVND(decimal amount, decimal exchangeRate)
    {
        return amount * exchangeRate;
    }

    // Hàm định dạng hiển thị tiền VND
    public static string FormatToVND(decimal amount)
    {
        return amount.ToString("C0", CultureInfo.GetCultureInfo("vi-VN"));
    }
}