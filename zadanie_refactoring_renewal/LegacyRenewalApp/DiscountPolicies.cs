using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public abstract class PercentageDiscountPolicyBase
    {
        protected static PricingComponentResult Create(decimal baseAmount, decimal percentage, string notes)
        {
            return new PricingComponentResult(baseAmount * percentage, notes);
        }
    }

    public class SilverSegmentDiscountPolicy : PercentageDiscountPolicyBase, ISegmentDiscountPolicy
    {
        public bool IsApplicable(Customer customer, SubscriptionPlan plan) => customer.Segment == "Silver";
        public PricingComponentResult Calculate(decimal baseAmount) => Create(baseAmount, 0.05m, "silver discount; ");
    }

    public class GoldSegmentDiscountPolicy : PercentageDiscountPolicyBase, ISegmentDiscountPolicy
    {
        public bool IsApplicable(Customer customer, SubscriptionPlan plan) => customer.Segment == "Gold";
        public PricingComponentResult Calculate(decimal baseAmount) => Create(baseAmount, 0.10m, "gold discount; ");
    }

    public class PlatinumSegmentDiscountPolicy : PercentageDiscountPolicyBase, ISegmentDiscountPolicy
    {
        public bool IsApplicable(Customer customer, SubscriptionPlan plan) => customer.Segment == "Platinum";
        public PricingComponentResult Calculate(decimal baseAmount) => Create(baseAmount, 0.15m, "platinum discount; ");
    }

    public class EducationSegmentDiscountPolicy : PercentageDiscountPolicyBase, ISegmentDiscountPolicy
    {
        public bool IsApplicable(Customer customer, SubscriptionPlan plan) =>
            customer.Segment == "Education" && plan.IsEducationEligible;

        public PricingComponentResult Calculate(decimal baseAmount) =>
            Create(baseAmount, 0.20m, "education discount; ");
    }

    public class LongTermLoyaltyDiscountPolicy : PercentageDiscountPolicyBase, ILoyaltyDiscountPolicy
    {
        public bool IsApplicable(Customer customer) => customer.YearsWithCompany >= 5;
        public PricingComponentResult Calculate(decimal baseAmount) =>
            Create(baseAmount, 0.07m, "long-term loyalty discount; ");
    }

    public class BasicLoyaltyDiscountPolicy : PercentageDiscountPolicyBase, ILoyaltyDiscountPolicy
    {
        public bool IsApplicable(Customer customer) => customer.YearsWithCompany >= 2;
        public PricingComponentResult Calculate(decimal baseAmount) =>
            Create(baseAmount, 0.03m, "basic loyalty discount; ");
    }

    public class LargeTeamDiscountPolicy : PercentageDiscountPolicyBase, ISeatCountDiscountPolicy
    {
        public bool IsApplicable(int seatCount) => seatCount >= 50;
        public PricingComponentResult Calculate(decimal baseAmount) =>
            Create(baseAmount, 0.12m, "large team discount; ");
    }

    public class MediumTeamDiscountPolicy : PercentageDiscountPolicyBase, ISeatCountDiscountPolicy
    {
        public bool IsApplicable(int seatCount) => seatCount >= 20;
        public PricingComponentResult Calculate(decimal baseAmount) =>
            Create(baseAmount, 0.08m, "medium team discount; ");
    }

    public class SmallTeamDiscountPolicy : PercentageDiscountPolicyBase, ISeatCountDiscountPolicy
    {
        public bool IsApplicable(int seatCount) => seatCount >= 10;
        public PricingComponentResult Calculate(decimal baseAmount) =>
            Create(baseAmount, 0.04m, "small team discount; ");
    }

    public class LoyaltyPointsDiscountPolicy
    {
        public PricingComponentResult Calculate(Customer customer, bool useLoyaltyPoints)
        {
            if (!useLoyaltyPoints || customer.LoyaltyPoints <= 0)
            {
                return new PricingComponentResult(0m, string.Empty);
            }

            int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
            return new PricingComponentResult(pointsToUse, $"loyalty points used: {pointsToUse}; ");
        }
    }

    public class DiscountCalculator : IDiscountCalculator
    {
        private readonly IEnumerable<ISegmentDiscountPolicy> _segmentPolicies;
        private readonly IEnumerable<ILoyaltyDiscountPolicy> _loyaltyPolicies;
        private readonly IEnumerable<ISeatCountDiscountPolicy> _seatCountPolicies;
        private readonly LoyaltyPointsDiscountPolicy _loyaltyPointsPolicy;

        public DiscountCalculator(
            IEnumerable<ISegmentDiscountPolicy> segmentPolicies,
            IEnumerable<ILoyaltyDiscountPolicy> loyaltyPolicies,
            IEnumerable<ISeatCountDiscountPolicy> seatCountPolicies,
            LoyaltyPointsDiscountPolicy loyaltyPointsPolicy)
        {
            _segmentPolicies = segmentPolicies;
            _loyaltyPolicies = loyaltyPolicies;
            _seatCountPolicies = seatCountPolicies;
            _loyaltyPointsPolicy = loyaltyPointsPolicy;
        }

        public PricingComponentResult Calculate(
            Customer customer,
            SubscriptionPlan plan,
            RenewalRequest request,
            decimal baseAmount)
        {
            decimal amount = 0m;
            string notes = string.Empty;

            foreach (var policy in _segmentPolicies)
            {
                if (!policy.IsApplicable(customer, plan))
                {
                    continue;
                }

                var result = policy.Calculate(baseAmount);
                amount += result.Amount;
                notes += result.Notes;
                break;
            }

            foreach (var policy in _loyaltyPolicies)
            {
                if (!policy.IsApplicable(customer))
                {
                    continue;
                }

                var result = policy.Calculate(baseAmount);
                amount += result.Amount;
                notes += result.Notes;
                break;
            }

            foreach (var policy in _seatCountPolicies)
            {
                if (!policy.IsApplicable(request.SeatCount))
                {
                    continue;
                }

                var result = policy.Calculate(baseAmount);
                amount += result.Amount;
                notes += result.Notes;
                break;
            }

            var loyaltyPointsResult = _loyaltyPointsPolicy.Calculate(customer, request.UseLoyaltyPoints);
            amount += loyaltyPointsResult.Amount;
            notes += loyaltyPointsResult.Notes;

            return new PricingComponentResult(amount, notes);
        }
    }
}
