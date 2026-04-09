namespace Bagile.Application.Templates;

/// <summary>
/// Wraps email body content in a consistent branded HTML shell.
/// All send handlers call Wrap() on their final htmlBody before passing to IEmailService.
/// Templates provide the content (including any sign-off); this class provides the chrome.
/// The footer carries only contact/social links — no duplicate "Regards" sign-off.
/// </summary>
public static class EmailTemplateWrapper
{
    public static string Wrap(string bodyContent)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>b-agile</title>
    <link href=""https://fonts.googleapis.com/css2?family=Montserrat:wght@300;400;600&display=swap"" rel=""stylesheet"">
    <style>
        body {{
            margin: 0; padding: 0;
            font-family: 'Montserrat', Arial, Helvetica, sans-serif;
            font-size: 15px;
            font-weight: 300;
            color: #212121;
            line-height: 1.65;
            background-color: #eef0f3;
        }}
        .outer {{
            padding: 28px 16px;
        }}
        .wrapper {{
            max-width: 620px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 6px;
            overflow: hidden;
            box-shadow: 0 2px 8px rgba(0,0,0,0.08);
        }}

        /* ── Header ── */
        .header {{
            background-color: #003366;
            padding: 24px 36px;
            text-align: center;
        }}
        .header img {{
            display: block;
            margin: 0 auto;
            max-width: 160px;
            height: auto;
        }}
        /* Fallback wordmark shown if logo fails to load */
        .header-wordmark {{
            display: none;
            font-family: 'Montserrat', Arial, sans-serif;
            font-size: 24px;
            font-weight: 600;
            color: #ffffff;
            letter-spacing: -0.5px;
        }}
        .header-wordmark .dot {{ color: #F7741C; }}

        /* ── Blue accent bar under header ── */
        .accent-bar {{
            height: 4px;
            background-color: #007BFF;
        }}

        /* ── Body content ── */
        .content {{
            padding: 36px 36px 28px;
        }}
        .content p {{
            margin: 0 0 14px;
        }}
        .content h2 {{
            font-family: 'Montserrat', Arial, sans-serif;
            font-size: 17px;
            font-weight: 600;
            color: #003366;
            margin: 28px 0 8px;
            padding-bottom: 6px;
            border-bottom: 2px solid #007BFF;
        }}
        .content h3 {{
            font-size: 15px;
            font-weight: 600;
            color: #003366;
            margin: 20px 0 6px;
        }}
        .content a {{
            color: #007BFF;
            text-decoration: none;
        }}
        .content a:hover {{
            text-decoration: underline;
        }}
        .content ul {{
            padding-left: 20px;
            margin: 6px 0 14px;
        }}
        .content li {{
            margin-bottom: 6px;
        }}

        /* ── Course info box (orange left border matches ticket email style) ── */
        .info-box {{
            background-color: #f7f8fa;
            border-left: 4px solid #F7741C;
            border-radius: 0 4px 4px 0;
            padding: 14px 18px;
            margin: 0 0 22px;
            font-size: 14px;
            line-height: 1.9;
        }}
        .info-box strong {{
            color: #003366;
            font-weight: 600;
            display: inline-block;
            min-width: 88px;
        }}

        /* ── Footer ── */
        .footer {{
            background-color: #003366;
            padding: 22px 36px;
            text-align: center;
        }}
        .footer p {{
            margin: 0 0 6px;
            font-size: 13px;
            color: rgba(255,255,255,0.65);
        }}
        .footer a {{
            color: #ffffff;
            text-decoration: none;
            font-size: 13px;
        }}
        .footer a:hover {{
            text-decoration: underline;
        }}
        .footer .divider {{
            color: rgba(255,255,255,0.25);
            margin: 0 8px;
        }}
        .footer .links {{
            margin-top: 10px;
        }}
        .footer .links a {{
            margin: 0 4px;
        }}
    </style>
</head>
<body>
<div class=""outer"">
    <div class=""wrapper"">

        <!-- Header with b-agile logo -->
        <div class=""header"">
            <img src=""https://www.bagile.co.uk/wp-content/uploads/2023/07/bagile-logo-white-01.svg""
                 alt=""b-agile""
                 width=""160""
                 onerror=""this.style.display='none';this.nextElementSibling.style.display='block'"" />
            <div class=""header-wordmark"">b<span class=""dot"">·</span>agile</div>
        </div>
        <div class=""accent-bar""></div>

        <!-- Body -->
        <div class=""content"">
            {bodyContent}
        </div>

        <!-- Footer — contact info only, no duplicate sign-off -->
        <div class=""footer"">
            <p>
                <a href=""mailto:info@bagile.co.uk"">info@bagile.co.uk</a>
                <span class=""divider"">|</span>
                <a href=""https://www.bagile.co.uk"">www.bagile.co.uk</a>
                <span class=""divider"">|</span>
                <span style=""color:#7a9cc0"">+44 20 4552 9823</span>
            </p>
            <p class=""links"">
                <a href=""https://www.linkedin.com/company/bagile"">LinkedIn</a>
                <span class=""divider"">&middot;</span>
                <a href=""https://www.bagile.co.uk/our-courses/"">Our Courses</a>
            </p>
        </div>

    </div>
</div>
</body>
</html>";
    }
}
