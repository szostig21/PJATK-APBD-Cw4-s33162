using System;
using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public class PremiumSupportFeeCalculator : ISupportFeeCalculator
    {
        private static readonly IReadOnlyDictionary<string, decimal> SupportFeesByPlan =
            new Dictionary<string, decimal>
            {
                ["START"] = 250m,
                ["PRO"] = 400m,
                ["ENTERPRISE"] = 700m
            };

        public PricingComponentResult Calculate(RenewalRequest request, decimal subtotalAfterDiscount)
        {
            if (!request.IncludePremiumSupport)
            {
                return new PricingComponentResult(0m, string.Empty);
            }

            decimal amount = SupportFeesByPlan.TryGetValue(request.PlanCode, out var fee) ? fee : 0m;
            return new PricingComponentResult(amount, "premium support included; ");
        }
    }

    public class CardPaymentFeePolicy : IPaymentFeePolicy
    {
        public bool IsMatch(string normalizedPaymentMethod) => normalizedPaymentMethod == "CARD";
        public PricingComponentResult Calculate(decimal feeBase) =>
            new PricingComponentResult(feeBase * 0.02m, "card payment fee; ");
    }

    public class BankTransferPaymentFeePolicy : IPaymentFeePolicy
    {
        public bool IsMatch(string normalizedPaymentMethod) => normalizedPaymentMethod == "BANK_TRANSFER";
        public PricingComponentResult Calculate(decimal feeBase) =>
            new PricingComponentResult(feeBase * 0.01m, "bank transfer fee; ");
    }

    public class PaypalPaymentFeePolicy : IPaymentFeePolicy
    {
        public bool IsMatch(string normalizedPaymentMethod) => normalizedPaymentMethod == "PAYPAL";
        public PricingComponentResult Calculate(decimal feeBase) =>
            new PricingComponentResult(feeBase * 0.035m, "paypal fee; ");
    }

    public class InvoicePaymentFeePolicy : IPaymentFeePolicy
    {
        public bool IsMatch(string normalizedPaymentMethod) => normalizedPaymentMethod == "INVOICE";
        public PricingComponentResult Calculate(decimal feeBase) =>
            new PricingComponentResult(0m, "invoice payment; ");
    }

    public class PaymentFeeCalculator : IPaymentFeeCalculator
    {
        private readonly IEnumerable<IPaymentFeePolicy> _policies;

        public PaymentFeeCalculator(IEnumerable<IPaymentFeePolicy> policies)
        {
            _policies = policies;
        }

        public PricingComponentResult Calculate(string normalizedPaymentMethod, decimal feeBase)
        {
            foreach (var policy in _policies)
            {
                if (!policy.IsMatch(normalizedPaymentMethod))
                {
                    continue;
                }

                return policy.Calculate(feeBase);
            }

            throw new ArgumentException("Unsupported payment method");
        }
    }

    public class CountryTaxRateProvider : ITaxRateProvider
    {
        private static readonly IReadOnlyDictionary<string, decimal> TaxRatesByCountry =
            new Dictionary<string, decimal>
            {
                ["Poland"] = 0.23m,
                ["Germany"] = 0.19m,
                ["Czech Republic"] = 0.21m,
                ["Norway"] = 0.25m
            };

        public decimal GetRateFor(string country)
        {
            return TaxRatesByCountry.TryGetValue(country, out var rate) ? rate : 0.20m;
        }
    }
}