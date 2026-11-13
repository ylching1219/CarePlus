using System.Net;

namespace CarePlus.API.Services.EmailServices;

/// <summary>Shared dark card HTML for CarePlus transaction emails (new login, OTP).</summary>
public static class CarePlusEmailHtml
{
    private const string Tagline = "Patient portal for clinic visits.";

    public static string NewLogin(string name, string device, string ipAddress)
    {
        var body = $"""
            <p style="margin:0 0 12px 0;font-size:15px;color:#f5f5f5">Hi {Encode(name)},</p>
            <p style="margin:0 0 20px 0;font-size:15px;color:#f5f5f5;line-height:1.55">A sign-in to your CarePlus account was detected from a new or unrecognized device.</p>
            {InfoBox(("Device", device), ("IP address", string.IsNullOrWhiteSpace(ipAddress) ? "Unknown" : ipAddress))}
            <p style="margin:0;font-size:13px;color:#a3a3a3;line-height:1.55">If this was you, no action is needed. If you do not recognize this sign-in, change your password and review account activity in Settings → Security.</p>
            """;
        return Wrap(body);
    }

    public static string Otp(string title, string intro, string code, int expiresMinutes, string ignoreHint)
    {
        var body = $"""
            <p style="margin:0 0 8px 0;font-size:13px;letter-spacing:0.06em;text-transform:uppercase;color:#a3a3a3">{Encode(title)}</p>
            <p style="margin:0 0 20px 0;font-size:15px;color:#f5f5f5;line-height:1.55">{Encode(intro)}</p>
            {OtpBox(code)}
            <p style="margin:16px 0 0 0;font-size:13px;color:#a3a3a3;line-height:1.55">This code expires in {expiresMinutes} minutes. {Encode(ignoreHint)}</p>
            """;
        return Wrap(body);
    }

    private static string Wrap(string innerHtml) => $"""
        <!DOCTYPE html>
        <html>
          <body style="margin:0;padding:0;background:#0a0a0a;font-family:Arial,Helvetica,sans-serif;color:#f5f5f5">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#0a0a0a;padding:32px 16px">
              <tr>
                <td align="center">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:480px;background:#171717;border-radius:16px;padding:28px 24px">
                    <tr>
                      <td align="center" style="padding:0 0 20px 0">
                        <p style="margin:0;font-size:18px;font-weight:700;letter-spacing:0.04em;color:#ffffff">CarePlus Clinic</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="text-align:left">{innerHtml}</td>
                    </tr>
                  </table>
                  <p style="margin:20px 0 0 0;font-size:12px;color:#737373;text-align:center">{Tagline}</p>
                </td>
              </tr>
            </table>
          </body>
        </html>
        """;

    private static string InfoBox(params (string Label, string Value)[] rows)
    {
        var parts = new System.Text.StringBuilder();
        parts.Append("""<div style="background:#0a0a0a;border-radius:12px;padding:16px 18px;margin:0 0 20px 0">""");
        for (var i = 0; i < rows.Length; i++)
        {
            var (label, value) = rows[i];
            var bottom = i < rows.Length - 1 ? "14px" : "0";
            parts.Append($"""
                <p style="margin:0 0 4px 0;font-size:12px;color:#a3a3a3">{Encode(label)}</p>
                <p style="margin:0 0 {bottom} 0;font-size:16px;font-weight:700;color:#ffffff">{Encode(value)}</p>
                """);
        }
        parts.Append("</div>");
        return parts.ToString();
    }

    private static string OtpBox(string code) => $"""
        <div style="background:#0a0a0a;border-radius:12px;padding:20px 18px;margin:0;text-align:center">
          <p style="margin:0 0 8px 0;font-size:12px;color:#a3a3a3">Verification code</p>
          <p style="margin:0;font-size:32px;letter-spacing:10px;font-weight:700;line-height:1.2;color:#ffffff">{Encode(code)}</p>
        </div>
        """;

    private static string Encode(string value) => WebUtility.HtmlEncode(value ?? "");
}
