using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository customerRepository;
        private readonly ISubscriptionPlanRepository planRepository;
        private readonly IRenewalRequestValidator requestValidator;
        private readonly IRenewalPricingCalculator pricingCalculator;
        private readonly IRenewalInvoiceFactory invoiceFactory;
        private readonly IBillingProcessor billingProcessor;

        public SubscriptionRenewalService()
            : this(
                new CustomerRepository(),
                new SubscriptionPlanRepository(),
                new RenewalRequestValidator(),
                CreatePricingCalculator(),
                new RenewalInvoiceFactory(new SystemClock()),
                new BillingProcessor(new LegacyBillingGatewayAdapter()))
        {
        }

        internal SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IRenewalRequestValidator requestValidator,
            IRenewalPricingCalculator pricingCalculator,
            IRenewalInvoiceFactory invoiceFactory,
            IBillingProcessor billingProcessor)
        {
            this.customerRepository = customerRepository;
            this.planRepository = planRepository;
            this.requestValidator = requestValidator;
            this.pricingCalculator = pricingCalculator;
            this.invoiceFactory = invoiceFactory;
            this.billingProcessor = billingProcessor;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            var request = new RenewalRequest(
                customerId,
                planCode,
                seatCount,
                paymentMethod,
                includePremiumSupport,
                useLoyaltyPoints);

            requestValidator.Validate(request);
            request.Normalize();

            var customer = customerRepository.GetById(request.CustomerId);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            var plan = planRepository.GetByCode(request.PlanCode);
            var pricingResult = pricingCalculator.Calculate(customer, plan, request);
            var invoice = invoiceFactory.Create(customer, request, pricingResult);

            billingProcessor.Process(invoice, customer);
            return invoice;
        }

        private static IRenewalPricingCalculator CreatePricingCalculator()
        {
            return new RenewalPricingCalculator(
                new DiscountCalculator(new IDiscountPolicy[]
                {
                    new CustomerSegmentDiscountPolicy(),
                    new LoyaltyYearsDiscountPolicy(),
                    new TeamSizeDiscountPolicy(),
                    new LoyaltyPointsDiscountPolicy()
                }),
                new SupportFeeCalculator(new ISupportFeePolicy[]
                {
                    new FixedSupportFeePolicy("START", 250m),
                    new FixedSupportFeePolicy("PRO", 400m),
                    new FixedSupportFeePolicy("ENTERPRISE", 700m)
                }),
                new PaymentFeeCalculator(new IPaymentFeePolicy[]
                {
                    new RatePaymentFeePolicy("CARD", 0.02m, "card payment fee"),
                    new RatePaymentFeePolicy("BANK_TRANSFER", 0.01m, "bank transfer fee"),
                    new RatePaymentFeePolicy("PAYPAL", 0.035m, "paypal fee"),
                    new FixedZeroPaymentFeePolicy("INVOICE", "invoice payment")
                }),
                new CountryTaxRateProvider());
        }
    }
}