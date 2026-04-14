using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.Payment;

/// <summary>
/// Param (eski Paraü/Türkpos) odeme gateway implementasyonu.
/// Param API'si SOAP + XML response döner. BaseUrl örneği:
///   https://posws.param.com.tr/turkpos.ws/service_turkpos_prod.asmx  (prod)
///   https://test-dmz.param.com.tr/turkpos.ws/service_turkpos_test.asmx  (test)
/// Dokumasyon: https://dev.param.com.tr/
/// </summary>
public class ParamGateway : IPaymentGateway
{
    private readonly ParamCredentials _credentials;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private const string SoapNs = "https://turkpos.com.tr/";

    public int ProviderTypeId => PaymentProviders.Ids.Param;

    public ParamGateway(ParamCredentials credentials)
    {
        _credentials = credentials;
        _http = new HttpClient { BaseAddress = new Uri(credentials.BaseUrl) };
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<PaymentInitResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var totalAmount = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var installment = request.Installment <= 0 ? 1 : request.Installment;

            // Param TP_WMD_UCD imza: SHA2B64(clientCode + guid + tutar + taksit + siparisId)
            var hashData = _credentials.ClientCode + _credentials.Guid + totalAmount
                + installment.ToString() + request.ConversationId + totalAmount
                + (request.CallbackUrl ?? "https://corplynk.com/payment/success")
                + (request.CallbackUrl ?? "https://corplynk.com/payment/fail");
            var hash = ComputeSha2B64(hashData);

            // SOAP XML gövdesi — TP_WMD_UCD metodu
            var body =
                "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                "<soap:Body>" +
                $"<TP_WMD_UCD xmlns=\"{SoapNs}\">" +
                "<G>" +
                $"<CLIENT_CODE>{X(_credentials.ClientCode)}</CLIENT_CODE>" +
                $"<CLIENT_USERNAME>{X(_credentials.ClientUsername)}</CLIENT_USERNAME>" +
                $"<CLIENT_PASSWORD>{X(_credentials.ClientPassword)}</CLIENT_PASSWORD>" +
                "</G>" +
                $"<GUID>{X(_credentials.Guid)}</GUID>" +
                $"<KK_Sahibi>{X(request.CardHolderName ?? "")}</KK_Sahibi>" +
                $"<KK_No>{X(request.CardNumber ?? "")}</KK_No>" +
                $"<KK_SK_Ay>{X(request.ExpireMonth ?? "")}</KK_SK_Ay>" +
                $"<KK_SK_Yil>{X(request.ExpireYear ?? "")}</KK_SK_Yil>" +
                $"<KK_CVC>{X(request.Cvc ?? "")}</KK_CVC>" +
                $"<KK_Sahibi_GSM>{X(request.BuyerPhone ?? "")}</KK_Sahibi_GSM>" +
                $"<Hata_URL>{X(request.CallbackUrl ?? "https://corplynk.com/payment/fail")}</Hata_URL>" +
                $"<Basarili_URL>{X(request.CallbackUrl ?? "https://corplynk.com/payment/success")}</Basarili_URL>" +
                $"<Siparis_ID>{X(request.ConversationId)}</Siparis_ID>" +
                $"<Siparis_Aciklama>{X(request.Description ?? "Corplynk Odeme")}</Siparis_Aciklama>" +
                $"<Taksit>{installment}</Taksit>" +
                $"<Islem_Tutar>{totalAmount}</Islem_Tutar>" +
                $"<Toplam_Tutar>{totalAmount}</Toplam_Tutar>" +
                $"<Islem_Hash>{X(hash)}</Islem_Hash>" +
                "<Islem_Guvenlik_Tip>NS</Islem_Guvenlik_Tip>" +
                "<Islem_ID/><IPAdr>" + X(request.BuyerIp ?? "127.0.0.1") + "</IPAdr>" +
                "<Ref_URL/><Data1/><Data2/><Data3/><Data4/><Data5/>" +
                "</TP_WMD_UCD>" +
                "</soap:Body></soap:Envelope>";

            var req = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = new StringContent(body, Encoding.UTF8, "text/xml")
            };
            req.Headers.TryAddWithoutValidation("SOAPAction", $"\"{SoapNs}TP_WMD_UCD\"");
            var response = await _http.SendAsync(req, ct);
            var xml = await response.Content.ReadAsStringAsync(ct);

            var (sonuc, dekontId, ucdRef, ucdMd, sonucStr) = ParseTP_WMD_UCD(xml);

            if (sonuc == "1")
            {
                return PaymentInitResult.Ok(
                    string.IsNullOrEmpty(dekontId) ? request.ConversationId : dekontId,
                    ucdRef);
            }
            return PaymentInitResult.Fail(string.IsNullOrWhiteSpace(sonucStr) ? (ucdMd ?? "Param ödeme başarısız.") : sonucStr!);
        }
        catch (Exception ex)
        {
            return PaymentInitResult.Fail($"Param hatası: {ex.Message}");
        }
    }

    private static string X(string s)
        => System.Security.SecurityElement.Escape(s ?? "") ?? "";

    private static (string? Sonuc, string? DekontId, string? UcdRefNo, string? UcdMd, string? SonucStr) ParseTP_WMD_UCD(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var ns = (XNamespace)SoapNs;
            // Response: <TP_WMD_UCDResponse><TP_WMD_UCDResult><Sonuc>...<Sonuc_Str>...</...>
            var result = doc.Descendants(ns + "TP_WMD_UCDResult").FirstOrDefault()
                         ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "TP_WMD_UCDResult");
            string? get(string name)
            {
                var el = result?.Descendants().FirstOrDefault(e => e.Name.LocalName == name);
                return el?.Value;
            }
            return (get("Sonuc"), get("Dekont_ID"), get("UCD_Ref_No"), get("UCD_MD"), get("Sonuc_Str"));
        }
        catch
        {
            return (null, null, null, null, "Param XML parse hatasi");
        }
    }

    public async Task<PaymentVerifyResult> VerifyPaymentAsync(string providerTransactionId, CancellationToken ct = default)
    {
        return await Task.FromResult(PaymentVerifyResult.Ok(providerTransactionId));
    }

    public async Task<PaymentRefundResult> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default)
    {
        try
        {
            var totalAmount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var body =
                "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                "<soap:Body>" +
                $"<TP_Islem_Iptal_Iade_Kismi xmlns=\"{SoapNs}\">" +
                "<G>" +
                $"<CLIENT_CODE>{X(_credentials.ClientCode)}</CLIENT_CODE>" +
                $"<CLIENT_USERNAME>{X(_credentials.ClientUsername)}</CLIENT_USERNAME>" +
                $"<CLIENT_PASSWORD>{X(_credentials.ClientPassword)}</CLIENT_PASSWORD>" +
                "</G>" +
                $"<GUID>{X(_credentials.Guid)}</GUID>" +
                $"<Durum>IADE</Durum>" +
                $"<Siparis_ID>{X(providerTransactionId)}</Siparis_ID>" +
                $"<Tutar>{totalAmount}</Tutar>" +
                "</TP_Islem_Iptal_Iade_Kismi>" +
                "</soap:Body></soap:Envelope>";

            var req = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = new StringContent(body, Encoding.UTF8, "text/xml")
            };
            req.Headers.TryAddWithoutValidation("SOAPAction", $"\"{SoapNs}TP_Islem_Iptal_Iade_Kismi\"");
            var response = await _http.SendAsync(req, ct);
            var xml = await response.Content.ReadAsStringAsync(ct);

            var doc = XDocument.Parse(xml);
            string? get(string n) => doc.Descendants().FirstOrDefault(e => e.Name.LocalName == n)?.Value;
            var sonuc = get("Sonuc");
            var sonucStr = get("Sonuc_Str");

            if (sonuc == "1")
                return PaymentRefundResult.Ok(providerTransactionId);
            return PaymentRefundResult.Fail(sonucStr ?? "İade başarısız.");
        }
        catch (Exception ex)
        {
            return PaymentRefundResult.Fail($"Param iade hatası: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            // SOAP TP_Islem_Odeme_OnProv_KK ya da basit BIN sorgulama — hafif: asmx'e HEAD/GET vurup 200 bekleriz.
            // Gercek Param endpoint kimlik dogrulamasi SOAP icinde yapildigi icin, baglanti testi olarak
            // asmx WSDL sayfasinin acilmasi credentials'tan bagimsiz — yalnizca URL dogrulanir.
            var req = new HttpRequestMessage(HttpMethod.Get, "?WSDL");
            var response = await _http.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
                return (false, $"Param endpoint ulasilamadi (HTTP {(int)response.StatusCode}).");

            var text = await response.Content.ReadAsStringAsync(ct);
            if (!text.Contains("TP_WMD_UCD", StringComparison.OrdinalIgnoreCase))
                return (false, "Param endpoint dogrulanamadi (TP_WMD_UCD metodu yok).");
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string ComputeSha2B64(string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(bytes);
    }

}
