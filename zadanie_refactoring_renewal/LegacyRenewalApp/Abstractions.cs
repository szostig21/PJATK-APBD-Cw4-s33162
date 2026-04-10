using System;
using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public interface ICustomerRepository
    {
        Customer GetById(int customerId);
    }

    public interface ISubscriptionPlanRepository
    {
        SubscriptionPlan GetByCode(string code);
    }

    public interface IRenewalRequestValidator
    {
        RenewalRequest ValidateAndCreate(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints);
    }

    public interface IDiscountCalculator
    {
        PricingComponentResult Calculate(
            Customer customer,
            SubscriptionPlan plan,
            RenewalRequest request,
            decimal baseAmount);
    }

    public interface ISegmentDiscountPolicy
    {
        bool IsApplicable(Customer customer, SubscriptionPlan plan);
        PricingComponentResult Calculate(decimal baseAmount);
    }

    public interface ILoyaltyDiscountPolicy
    {
        bool IsApplicable(Customer customer);
        PricingComponentResult Calculate(decimal baseAmount);
    }

    public interface ISeatCountDiscountPolicy
    {
        bool IsApplicable(int seatCount);
        PricingComponentResult Calculate(decimal baseAmount);
    }

    public interface ISupportFeeCalculator
    {
        PricingComponentResult Calculate(RenewalRequest request, decimal subtotalAfterDiscount);
    }

    public interface IPaymentFeeCalculator
    {
        PricingComponentResult Calculate(string normalizedPaymentMethod, decimal feeBase);
    }

    public interface IPaymentFeePolicy
    {
        bool IsMatch(string normalizedPaymentMethod);
        PricingComponentResult Calculate(decimal feeBase);
    }

    public interface ITaxRateProvider
    {
        decimal GetRateFor(string country);
    }

    public interface IInvoiceFactory
    {
        RenewalInvoice Create(
            Customer customer,
            RenewalRequest request,
            decimal baseAmount,
            decimal discountAmount,
            decimal supportFee,
            decimal paymentFee,
            decimal taxAmount,
            decimal finalAmount,
            string notes);
    }

    public interface IBillingGateway
    {
        void SaveInvoice(RenewalInvoice invoice);
        void SendEmail(string email, string subject, string body);
    }

    public interface IInvoiceNotificationService
    {
        void SendInvoiceNotification(Customer customer, RenewalInvoice invoice, IBillingGateway billingGateway);
    }

    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}