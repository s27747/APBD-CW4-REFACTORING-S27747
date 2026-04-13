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
        public string PlanCode { get; private set; }
        public int SeatCount { get; }
        public string PaymentMethod { get; private set; }
        public bool IncludePremiumSupport { get; }
        public bool UseLoyaltyPoints { get; }

        public void Normalize()
        {
            PlanCode = PlanCode.Trim().ToUpperInvariant();
            PaymentMethod = PaymentMethod.Trim().ToUpperInvariant();
        }
    }
}