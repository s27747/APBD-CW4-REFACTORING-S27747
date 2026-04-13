namespace LegacyRenewalApp;

public interface IBillingGatewayAdapter
{
    void SaveInvoice(RenewalInvoice invoice);
    void SendEmail(string email, string subject, string body);
}