using System;
using System.Collections.Generic;

namespace LegacyRenewalApp
{
    internal interface IRenewalPricingCalculator
    {
        PricingResult Calculate(Customer customer, SubscriptionPlan plan, RenewalRequest request);
    }

    internal interface IDiscountCalculator
    {
        DiscountResult Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints);
    }

    internal interface IDiscountPolicy
    {
        DiscountComponent Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints);
    }

    internal interface ISupportFeeCalculator
    {
        FeeResult Calculate(bool includePremiumSupport, string normalizedPlanCode);
    }

    internal interface ISupportFeePolicy
    {
        bool CanHandle(string normalizedPlanCode);
        decimal Fee { get; }
    }

    internal interface IPaymentFeeCalculator
    {
        FeeResult Calculate(string normalizedPaymentMethod, decimal amountBase);
    }

    internal interface IPaymentFeePolicy
    {
        bool CanHandle(string normalizedPaymentMethod);
        FeeResult Calculate(decimal amountBase);
    }

    internal interface ITaxRateProvider
    {
        decimal GetTaxRate(string country);
    }

    internal class DiscountComponent
    {
        public DiscountComponent(decimal amount, string note)
        {
            Amount = amount;
            Note = note;
        }

        public decimal Amount { get; }
        public string Note { get; }
    }

    internal class DiscountResult
    {
        public DiscountResult(decimal amount, IReadOnlyList<string> notes)
        {
            Amount = amount;
            Notes = notes;
        }

        public decimal Amount { get; }
        public IReadOnlyList<string> Notes { get; }
    }

    internal class FeeResult
    {
        public FeeResult(decimal amount, string note)
        {
            Amount = amount;
            Note = note;
        }

        public decimal Amount { get; }
        public string Note { get; }
    }

    internal class PricingResult
    {
        public PricingResult(
            decimal baseAmount,
            decimal discountAmount,
            decimal supportFee,
            decimal paymentFee,
            decimal taxAmount,
            decimal finalAmount,
            string notes)
        {
            BaseAmount = baseAmount;
            DiscountAmount = discountAmount;
            SupportFee = supportFee;
            PaymentFee = paymentFee;
            TaxAmount = taxAmount;
            FinalAmount = finalAmount;
            Notes = notes;
        }

        public decimal BaseAmount { get; }
        public decimal DiscountAmount { get; }
        public decimal SupportFee { get; }
        public decimal PaymentFee { get; }
        public decimal TaxAmount { get; }
        public decimal FinalAmount { get; }
        public string Notes { get; }
    }

    internal class DiscountCalculator : IDiscountCalculator
    {
        private readonly IEnumerable<IDiscountPolicy> policies;

        public DiscountCalculator(IEnumerable<IDiscountPolicy> policies)
        {
            this.policies = policies;
        }

        public DiscountResult Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            decimal totalAmount = 0m;
            var notes = new List<string>();

            foreach (var policy in policies)
            {
                var component = policy.Calculate(customer, plan, baseAmount, seatCount, useLoyaltyPoints);

                if (component.Amount <= 0m)
                {
                    continue;
                }

                totalAmount += component.Amount;

                if (!string.IsNullOrWhiteSpace(component.Note))
                {
                    notes.Add(component.Note);
                }
            }

            return new DiscountResult(totalAmount, notes);
        }
    }

    internal class CustomerSegmentDiscountPolicy : IDiscountPolicy
    {
        public DiscountComponent Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (customer.Segment == "Silver")
            {
                return new DiscountComponent(baseAmount * 0.05m, "silver discount");
            }

            if (customer.Segment == "Gold")
            {
                return new DiscountComponent(baseAmount * 0.10m, "gold discount");
            }

            if (customer.Segment == "Platinum")
            {
                return new DiscountComponent(baseAmount * 0.15m, "platinum discount");
            }

            if (customer.Segment == "Education" && plan.IsEducationEligible)
            {
                return new DiscountComponent(baseAmount * 0.20m, "education discount");
            }

            return new DiscountComponent(0m, string.Empty);
        }
    }

    internal class LoyaltyYearsDiscountPolicy : IDiscountPolicy
    {
        public DiscountComponent Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (customer.YearsWithCompany >= 5)
            {
                return new DiscountComponent(baseAmount * 0.07m, "long-term loyalty discount");
            }

            if (customer.YearsWithCompany >= 2)
            {
                return new DiscountComponent(baseAmount * 0.03m, "basic loyalty discount");
            }

            return new DiscountComponent(0m, string.Empty);
        }
    }

    internal class TeamSizeDiscountPolicy : IDiscountPolicy
    {
        public DiscountComponent Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (seatCount >= 50)
            {
                return new DiscountComponent(baseAmount * 0.12m, "large team discount");
            }

            if (seatCount >= 20)
            {
                return new DiscountComponent(baseAmount * 0.08m, "medium team discount");
            }

            if (seatCount >= 10)
            {
                return new DiscountComponent(baseAmount * 0.04m, "small team discount");
            }

            return new DiscountComponent(0m, string.Empty);
        }
    }

    internal class LoyaltyPointsDiscountPolicy : IDiscountPolicy
    {
        public DiscountComponent Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (!useLoyaltyPoints || customer.LoyaltyPoints <= 0)
            {
                return new DiscountComponent(0m, string.Empty);
            }

            int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
            return new DiscountComponent(pointsToUse, $"loyalty points used: {pointsToUse}");
        }
    }

    internal class SupportFeeCalculator : ISupportFeeCalculator
    {
        private readonly IEnumerable<ISupportFeePolicy> policies;

        public SupportFeeCalculator(IEnumerable<ISupportFeePolicy> policies)
        {
            this.policies = policies;
        }

        public FeeResult Calculate(bool includePremiumSupport, string normalizedPlanCode)
        {
            if (!includePremiumSupport)
            {
                return new FeeResult(0m, string.Empty);
            }

            foreach (var policy in policies)
            {
                if (policy.CanHandle(normalizedPlanCode))
                {
                    return new FeeResult(policy.Fee, "premium support included");
                }
            }

            return new FeeResult(0m, "premium support included");
        }
    }

    internal class FixedSupportFeePolicy : ISupportFeePolicy
    {
        private readonly string planCode;

        public FixedSupportFeePolicy(string planCode, decimal fee)
        {
            this.planCode = planCode;
            Fee = fee;
        }

        public decimal Fee { get; }

        public bool CanHandle(string normalizedPlanCode)
        {
            return normalizedPlanCode == planCode;
        }
    }

    internal class PaymentFeeCalculator : IPaymentFeeCalculator
    {
        private readonly IEnumerable<IPaymentFeePolicy> policies;

        public PaymentFeeCalculator(IEnumerable<IPaymentFeePolicy> policies)
        {
            this.policies = policies;
        }

        public FeeResult Calculate(string normalizedPaymentMethod, decimal amountBase)
        {
            foreach (var policy in policies)
            {
                if (policy.CanHandle(normalizedPaymentMethod))
                {
                    return policy.Calculate(amountBase);
                }
            }

            throw new ArgumentException("Unsupported payment method");
        }
    }

    internal class RatePaymentFeePolicy : IPaymentFeePolicy
    {
        private readonly string paymentMethod;
        private readonly decimal rate;
        private readonly string note;

        public RatePaymentFeePolicy(string paymentMethod, decimal rate, string note)
        {
            this.paymentMethod = paymentMethod;
            this.rate = rate;
            this.note = note;
        }

        public bool CanHandle(string normalizedPaymentMethod)
        {
            return normalizedPaymentMethod == paymentMethod;
        }

        public FeeResult Calculate(decimal amountBase)
        {
            return new FeeResult(amountBase * rate, note);
        }
    }

    internal class FixedZeroPaymentFeePolicy : IPaymentFeePolicy
    {
        private readonly string paymentMethod;
        private readonly string note;

        public FixedZeroPaymentFeePolicy(string paymentMethod, string note)
        {
            this.paymentMethod = paymentMethod;
            this.note = note;
        }

        public bool CanHandle(string normalizedPaymentMethod)
        {
            return normalizedPaymentMethod == paymentMethod;
        }

        public FeeResult Calculate(decimal amountBase)
        {
            return new FeeResult(0m, note);
        }
    }

    internal class CountryTaxRateProvider : ITaxRateProvider
    {
        private readonly Dictionary<string, decimal> ratesByCountry = new Dictionary<string, decimal>
        {
            { "Poland", 0.23m },
            { "Germany", 0.19m },
            { "Czech Republic", 0.21m },
            { "Norway", 0.25m }
        };

        public decimal GetTaxRate(string country)
        {
            if (ratesByCountry.TryGetValue(country, out decimal taxRate))
            {
                return taxRate;
            }

            return 0.20m;
        }
    }

    internal class RenewalPricingCalculator : IRenewalPricingCalculator
    {
        private readonly IDiscountCalculator discountCalculator;
        private readonly ISupportFeeCalculator supportFeeCalculator;
        private readonly IPaymentFeeCalculator paymentFeeCalculator;
        private readonly ITaxRateProvider taxRateProvider;

        public RenewalPricingCalculator(
            IDiscountCalculator discountCalculator,
            ISupportFeeCalculator supportFeeCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxRateProvider taxRateProvider)
        {
            this.discountCalculator = discountCalculator;
            this.supportFeeCalculator = supportFeeCalculator;
            this.paymentFeeCalculator = paymentFeeCalculator;
            this.taxRateProvider = taxRateProvider;
        }

        public PricingResult Calculate(Customer customer, SubscriptionPlan plan, RenewalRequest request)
        {
            decimal baseAmount = (plan.MonthlyPricePerSeat * request.SeatCount * 12m) + plan.SetupFee;

            var notes = new List<string>();

            var discountResult = discountCalculator.Calculate(
                customer,
                plan,
                baseAmount,
                request.SeatCount,
                request.UseLoyaltyPoints);

            notes.AddRange(discountResult.Notes);

            decimal subtotalAfterDiscount = baseAmount - discountResult.Amount;

            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes.Add("minimum discounted subtotal applied");
            }

            var supportFeeResult = supportFeeCalculator.Calculate(request.IncludePremiumSupport, request.PlanCode);
            AddNote(notes, supportFeeResult.Note);

            var paymentFeeResult = paymentFeeCalculator.Calculate(
                request.PaymentMethod,
                subtotalAfterDiscount + supportFeeResult.Amount);
            AddNote(notes, paymentFeeResult.Note);

            decimal taxBase = subtotalAfterDiscount + supportFeeResult.Amount + paymentFeeResult.Amount;
            decimal taxAmount = taxBase * taxRateProvider.GetTaxRate(customer.Country);
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes.Add("minimum invoice amount applied");
            }

            return new PricingResult(
                baseAmount,
                discountResult.Amount,
                supportFeeResult.Amount,
                paymentFeeResult.Amount,
                taxAmount,
                finalAmount,
                NoteFormatter.Build(notes));
        }

        private static void AddNote(List<string> notes, string note)
        {
            if (!string.IsNullOrWhiteSpace(note))
            {
                notes.Add(note);
            }
        }
    }

    internal static class NoteFormatter
    {
        public static string Build(IReadOnlyCollection<string> notes)
        {
            if (notes.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("; ", notes) + ";";
        }
    }
}