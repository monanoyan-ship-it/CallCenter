namespace CallCenter.Api.Services.Payment;

/// <summary>Iyzico API credential bilgileri</summary>
public class IyzicoCredentials
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    /// <summary>Sandbox: https://sandbox-api.iyzipay.com | Prod: https://api.iyzipay.com</summary>
    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";
}

/// <summary>PayTR API credential bilgileri</summary>
public class PayTrCredentials
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string MerchantSalt { get; set; } = string.Empty;
    public bool IsSandbox { get; set; } = true;
    /// <summary>Sandbox: https://www.paytr.com | Prod: https://www.paytr.com</summary>
    public string BaseUrl { get; set; } = "https://www.paytr.com";
}

/// <summary>Param API credential bilgileri</summary>
public class ParamCredentials
{
    public string ClientCode { get; set; } = string.Empty;
    public string ClientUsername { get; set; } = string.Empty;
    public string ClientPassword { get; set; } = string.Empty;
    public string Guid { get; set; } = string.Empty;
    /// <summary>Param SOAP endpoint. test-dmz inaktif (404), tek geçerli: posws.param.com.tr prod.</summary>
    public string BaseUrl { get; set; } = "https://posws.param.com.tr/turkpos.ws/service_turkpos_prod.asmx";
}

/// <summary>Havale/EFT banka hesap bilgileri</summary>
public class BankAccountInfo
{
    public string BankName { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string? Description { get; set; }
}
