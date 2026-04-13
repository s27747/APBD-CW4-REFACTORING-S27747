namespace LegacyRenewalApp;

public class LegacyBillingGatewayAdapter : IBillingGatewayAdapter
{
    public void SaveInvoice(RenewalInvoice invoice)
    {
        LegacyBillingGateway.SaveInvoice(invoice);
    }

    public void SendEmail(string email, string subject, string body)
    {
        LegacyBillingGateway.SendEmail(email, subject, body);
    }
}