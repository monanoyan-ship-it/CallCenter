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
    /// <summary>Test: test-dmz.param.com.tr/turkpos.ws/service_turkpos_test.asmx | Prod: posws.param.com.tr/turkpos.ws/service_turkpos_prod.asmx</summary>
    public string BaseUrl { get; set; } = "https://test-dmz.param.com.tr/turkpos.ws/service_turkpos_test.asmx";
}

/// <summary>Havale/EFT banka hesap bilgileri</summary>
public class BankAccountInfo
{
    public string BankName { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string? Description { get; set; }
}
