namespace Application.WebApi.Models;

public class NetsuiteAPISetupModel
{
    public string AccountId { get; set; }
    public string ClientCredentialsCertificateId { get; set; }
    public string ApiConsumerKey { get; set; }
    public string PrivateKeyPem { get; set; }
}
