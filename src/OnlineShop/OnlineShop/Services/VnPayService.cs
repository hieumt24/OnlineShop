using OnlineShop.Helpers;
using OnlineShop.Models.ViewModels;
using OnlineShop.Services.Interfaces;

namespace OnlineShop.Services;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;
    public VnPayService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model)
    {
        var tick = DateTime.Now.Ticks.ToString();

        var vnpay = new VnPayLibrary();
        
        vnpay.AddRequestData("vnp_Version", _configuration["VnPay:Version"]);
        vnpay.AddRequestData("vnp_Command", _configuration["VnPay:Command"]);
        vnpay.AddRequestData("vnp_TmnCode", _configuration["VnPay:TmnCode"]);
        var amount = ((long)(model.Amount * 100)).ToString();
        vnpay.AddRequestData("vnp_Amount", amount);
        vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", _configuration["VnPay:CurrCode"]);
        vnpay.AddRequestData("vnp_IpAddr", Helpers.Utils.GetIpAddress(context));
        vnpay.AddRequestData("vnp_Locale", _configuration["VnPay:Locale"]);
        
        vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + model.OrderId);
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", _configuration["VnPay:PaymentBackReturnUrl"]);
        vnpay.AddRequestData("vnp_TxnRef", tick);
        
        Console.WriteLine($"Amount: {(model.Amount * 100).ToString()}");
        Console.WriteLine($"CreateDate: {model.CreatedDate.ToString("yyyyMMddHHmmss")}");
        Console.WriteLine($"TmnCode: {_configuration["VnPay:TmnCode"]}");
        
        var paymenUrl = vnpay.CreateRequestUrl(_configuration["VnPay:BaseUrl"], _configuration["VnPay:HashSecret"]);
        return paymenUrl;
    }

    public VnPaymentResponseModel PaymentExecute(IQueryCollection collections)
    {
        var vnpay = new VnPayLibrary();
        foreach (var (key,value) in collections)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, value.ToString());
            }
        }
        var vnpay_orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef")); 
        var vnp_TransactionId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
        var vnpay_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
        var vnpay_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnpay_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
        
        bool checkSignature = vnpay.ValidateSignature(vnpay_SecureHash,_configuration["VnPay:HashSecret"] );
        if (!checkSignature)
        {
            return new VnPaymentResponseModel()
            {
                Success = false
            };
        }

        return new VnPaymentResponseModel()
        {
            Success = true,
            PaymentMethod = "VnPay",
            OrderDescription = vnpay_OrderInfo,
            OrderId = vnpay_orderId.ToString(),
            TransactionId = vnp_TransactionId.ToString(),
            Token = vnpay_SecureHash,
            VnPayResponseCode = vnpay_ResponseCode.ToString()
        };
    }
}