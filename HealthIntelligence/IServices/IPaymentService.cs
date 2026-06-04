using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.PaymentDto;

namespace HealthIntelligence.IServices
{
    public interface IPaymentService
    {
        Task<ResultResponseDto<CheckoutSessionResponse>> CreateCheckoutSession(CreateCheckoutSessionDto request);
        Task<ResultResponseDto<VerifySessionResponse>> VerifySession(VerifySessionDto request);
        Task<ResultResponseDto<string>> StripeWebhook();
    }
}
