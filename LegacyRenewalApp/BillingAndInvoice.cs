using System;

namespace LegacyRenewalApp
{
    internal interface IClock
    {
        DateTime UtcNow { get; }
    }

    internal interface IRenewalInvoiceFactory
    {
        RenewalInvoice Create(Customer customer, RenewalRequest request, PricingResult pricingResult);
    }

    internal interface IBillingProcessor
    {
        void Process(RenewalInvoice invoice, Customer customer);
    }

    internal class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    internal class RenewalInvoiceFactory : IRenewalInvoiceFactory
    {
        private readonly IClock clock;

        public RenewalInvoiceFactory(IClock clock)
        {
            this.clock = clock;
        }

        public RenewalInvoice Create(Customer customer, RenewalRequest request, PricingResult pricingResult)
        {
            DateTime generatedAt = clock.UtcNow;

            return new RenewalInvoice
            {
                InvoiceNumber = $"INV-{generatedAt:yyyyMMdd}-{request.CustomerId}-{request.PlanCode}",
                CustomerName = customer.FullName,
                PlanCode = request.PlanCode,
                PaymentMethod = request.PaymentMethod,
                SeatCount = request.SeatCount,
                BaseAmount = Round(pricingResult.BaseAmount),
                DiscountAmount = Round(pricingResult.DiscountAmount),
                SupportFee = Round(pricingResult.SupportFee),
                PaymentFee = Round(pricingResult.PaymentFee),
                TaxAmount = Round(pricingResult.TaxAmount),
                FinalAmount = Round(pricingResult.FinalAmount),
                Notes = pricingResult.Notes,
                GeneratedAt = generatedAt
            };
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }

    internal class BillingProcessor : IBillingProcessor
    {
        private readonly IBillingGatewayAdapter billingGateway;

        public BillingProcessor(IBillingGatewayAdapter billingGateway)
        {
            this.billingGateway = billingGateway;
        }

        public void Process(RenewalInvoice invoice, Customer customer)
        {
            billingGateway.SaveInvoice(invoice);

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