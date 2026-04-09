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
    <title>BAgile</title>
    <style>
        body {{
            margin: 0; padding: 0;
            font-family: Arial, Helvetica, sans-serif;
            font-size: 15px;
            color: #2d2d2d;
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
            background-color: #1a2332;
            padding: 24px 36px;
            text-align: center;
        }}
        .header-wordmark {{
            display: inline-block;
            font-size: 26px;
            font-weight: 700;
            letter-spacing: -0.5px;
            color: #ffffff;
            font-family: Arial, Helvetica, sans-serif;
        }}
        .header-wordmark span {{
            color: #e8792b;
        }}
        .header-tagline {{
            font-size: 11px;
            color: #8a9bb0;
            letter-spacing: 1.5px;
            text-transform: uppercase;
            margin-top: 4px;
        }}

        /* ── Orange accent bar under header ── */
        .accent-bar {{
            height: 4px;
            background: linear-gradient(90deg, #e8792b 0%, #f5a623 100%);
        }}

        /* ── Body content ── */
        .content {{
            padding: 36px 36px 28px;
        }}
        .content p {{
            margin: 0 0 14px;
        }}
        .content h2 {{
            font-size: 18px;
            color: #1a2332;
            margin: 28px 0 6px;
            padding-bottom: 6px;
            border-bottom: 2px solid #e8792b;
        }}
        .content h3 {{
            font-size: 15px;
            color: #1a2332;
            margin: 20px 0 6px;
        }}
        .content a {{
            color: #e8792b;
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

        /* ── Course info box ── */
        .info-box {{
            background-color: #f7f8fa;
            border-left: 4px solid #e8792b;
            border-radius: 0 4px 4px 0;
            padding: 14px 18px;
            margin: 0 0 22px;
            font-size: 14px;
            line-height: 1.8;
        }}
        .info-box strong {{
            color: #1a2332;
            display: inline-block;
            min-width: 52px;
        }}

        /* ── Footer ── */
        .footer {{
            background-color: #1a2332;
            padding: 22px 36px;
            text-align: center;
        }}
        .footer p {{
            margin: 0 0 6px;
            font-size: 13px;
            color: #8a9bb0;
        }}
        .footer a {{
            color: #e8792b;
            text-decoration: none;
            font-size: 13px;
        }}
        .footer a:hover {{
            text-decoration: underline;
        }}
        .footer .divider {{
            color: #3d4f66;
            margin: 0 6px;
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

        <!-- Header -->
        <div class=""header"">
            <div class=""header-wordmark"">B<span>A</span>gile</div>
            <div class=""header-tagline"">Professional Scrum Training</div>
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
                <span style=""color:#8a9bb0"">+44 20 4552 9823</span>
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
