using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IRenewalRequestValidator _validator;
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ISupportFeeCalculator _supportFeeCalculator;
        private readonly IPaymentFeeCalculator _paymentFeeCalculator;
        private readonly ITaxRateProvider _taxRateProvider;
        private readonly IInvoiceFactory _invoiceFactory;
        private readonly IBillingGateway _billingGateway;
        private readonly IInvoiceNotificationService _notificationService;

        public SubscriptionRenewalService()
            : this(
                new CustomerRepositoryAdapter(new CustomerRepository()),
                new SubscriptionPlanRepositoryAdapter(new SubscriptionPlanRepository()),
                new RenewalRequestValidator(),
                new DiscountCalculator(
                    new ISegmentDiscountPolicy[]
                    {
                        new SilverSegmentDiscountPolicy(),
                        new GoldSegmentDiscountPolicy(),
                        new PlatinumSegmentDiscountPolicy(),
                        new EducationSegmentDiscountPolicy()
                    },
                    new ILoyaltyDiscountPolicy[]
                    {
                        new LongTermLoyaltyDiscountPolicy(),
                        new BasicLoyaltyDiscountPolicy()
                    },
                    new ISeatCountDiscountPolicy[]
                    {
                        new LargeTeamDiscountPolicy(),
                        new MediumTeamDiscountPolicy(),
                        new SmallTeamDiscountPolicy()
                    },
                    new LoyaltyPointsDiscountPolicy()),
                new PremiumSupportFeeCalculator(),
                new PaymentFeeCalculator(
                    new IPaymentFeePolicy[]
                    {
                        new CardPaymentFeePolicy(),
                        new BankTransferPaymentFeePolicy(),
                        new PaypalPaymentFeePolicy(),
                        new InvoicePaymentFeePolicy()
                    }),
                new CountryTaxRateProvider(),
                new InvoiceFactory(new SystemClock()),
                new LegacyBillingGatewayAdapter(),
                new InvoiceNotificationService())
        {
        }

        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IRenewalRequestValidator validator,
            IDiscountCalculator discountCalculator,
            ISupportFeeCalculator supportFeeCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxRateProvider taxRateProvider,
            IInvoiceFactory invoiceFactory,
            IBillingGateway billingGateway,
            IInvoiceNotificationService notificationService)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _validator = validator;
            _discountCalculator = discountCalculator;
            _supportFeeCalculator = supportFeeCalculator;
            _paymentFeeCalculator = paymentFeeCalculator;
            _taxRateProvider = taxRateProvider;
            _invoiceFactory = invoiceFactory;
            _billingGateway = billingGateway;
            _notificationService = notificationService;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            var request = _validator.ValidateAndCreate(
                customerId,
                planCode,
                seatCount,
                paymentMethod,
                includePremiumSupport,
                useLoyaltyPoints);

            var customer = _customerRepository.GetById(request.CustomerId);
            var plan = _planRepository.GetByCode(request.PlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * request.SeatCount * 12m) + plan.SetupFee;
            var discountResult = _discountCalculator.Calculate(customer, plan, request, baseAmount);

            decimal subtotalAfterDiscount = baseAmount - discountResult.Amount;
            string notes = discountResult.Notes;

            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }

            var supportFeeResult = _supportFeeCalculator.Calculate(request, subtotalAfterDiscount);
            notes += supportFeeResult.Notes;

            var paymentFeeResult = _paymentFeeCalculator.Calculate(
                request.PaymentMethod,
                subtotalAfterDiscount + supportFeeResult.Amount);

            notes += paymentFeeResult.Notes;

            decimal taxRate = _taxRateProvider.GetRateFor(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportFeeResult.Amount + paymentFeeResult.Amount;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = _invoiceFactory.Create(
                customer,
                request,
                baseAmount,
                discountResult.Amount,
                supportFeeResult.Amount,
                paymentFeeResult.Amount,
                taxAmount,
                finalAmount,
                notes);

            _billingGateway.SaveInvoice(invoice);
            _notificationService.SendInvoiceNotification(customer, invoice, _billingGateway);

            return invoice;
        }
    }
}