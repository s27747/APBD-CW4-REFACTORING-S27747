using System;

namespace LegacyRenewalApp
{
    public class RenewalRequestValidator : IRenewalRequestValidator
    {
        public void Validate(RenewalRequest request)
        {
            if (request.CustomerId <= 0)
            {
                throw new ArgumentException("Customer id must be positive");
            }

            if (string.IsNullOrWhiteSpace(request.PlanCode))
            {
                throw new ArgumentException("Plan code is required");
            }

            if (request.SeatCount <= 0)
            {
                throw new ArgumentException("Seat count must be positive");
            }

            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                throw new ArgumentException("Payment method is required");
            }
        }
    }
}