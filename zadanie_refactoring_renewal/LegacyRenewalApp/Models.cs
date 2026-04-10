namespace LegacyRenewalApp
{
    public class RenewalRequest
    {
        public RenewalRequest(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            CustomerId = customerId;
            PlanCode = planCode;
            SeatCount = seatCount;
            PaymentMethod = paymentMethod;
            IncludePremiumSupport = includePremiumSupport;
            UseLoyaltyPoints = useLoyaltyPoints;
        }

        public int CustomerId { get; }
        public string PlanCode { get; }
        public int SeatCount { get; }
        public string PaymentMethod { get; }
        public bool IncludePremiumSupport { get; }
        public bool UseLoyaltyPoints { get; }
    }

    public class PricingComponentResult
    {
        public PricingComponentResult(decimal amount, string notes)
        {
            Amount = amount;
            Notes = notes;
        }

        public decimal Amount { get; }
        public string Notes { get; }
    }
}