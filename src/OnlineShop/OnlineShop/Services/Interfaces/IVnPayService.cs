using OnlineShop.Models.ViewModels;

namespace OnlineShop.Services.Interfaces;

public interface IVnPayService
{
    string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model);
    VnPaymentResponseModel PaymentExecute(IQueryCollection collections);
}