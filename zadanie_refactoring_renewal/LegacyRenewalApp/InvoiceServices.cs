using System;

namespace LegacyRenewalApp
{
    public class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public class InvoiceFactory : IInvoiceFactory
    {
        private readonly IClock _clock;

        public InvoiceFactory(IClock clock)
        {
            _clock = clock;
        }

        public RenewalInvoice Create(
            Customer customer,
            RenewalRequest request,
            decimal baseAmount,
            decimal discountAmount,
            decimal supportFee,
            decimal paymentFee,
            decimal taxAmount,
            decimal finalAmount,
            string notes)
        {
            var generatedAt = _clock.UtcNow;

            return new RenewalInvoice
            {
                InvoiceNumber = $"INV-{generatedAt:yyyyMMdd}-{request.CustomerId}-{request.PlanCode}",
                CustomerName = customer.FullName,
                PlanCode = request.PlanCode,
                PaymentMethod = request.PaymentMethod,
                SeatCount = request.SeatCount,
                BaseAmount = Round(baseAmount),
                DiscountAmount = Round(discountAmount),
                SupportFee = Round(supportFee),
                PaymentFee = Round(paymentFee),
                TaxAmount = Round(taxAmount),
                FinalAmount = Round(finalAmount),
                Notes = notes.Trim(),
                GeneratedAt = generatedAt
            };
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }

    public class LegacyBillingGatewayAdapter : IBillingGateway
    {
        public void SaveInvoice(RenewalInvoice invoice)
        {
            LegacyBillingGateway.SaveInvoice(invoice);
        }

        public void SendEmail(string email, string subject, string body)
        {
            LegacyBillingGateway.SendEmail(email, subject, body);
        }
    }

    public class InvoiceNotificationService : IInvoiceNotificationService
    {
        public void SendInvoiceNotification(Customer customer, RenewalInvoice invoice, IBillingGateway billingGateway)
        {
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                return;
            }

            string subject = "Subscription renewal invoice";
            string body =
                $"Hello {customer.FullName}, your renewal for plan {invoice.PlanCode} " +
                $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

            billingGateway.SendEmail(customer.Email, subject, body);
        }
    }
}