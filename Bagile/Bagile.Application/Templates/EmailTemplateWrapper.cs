namespace Bagile.Application.Templates;

/// <summary>
/// Wraps email body content in a consistent branded HTML shell.
/// All send handlers call Wrap() on their final htmlBody before passing to IEmailService.
/// Templates provide the content; this class provides the branding.
/// </summary>
public static class EmailTemplateWrapper
{
    public static string Wrap(string bodyContent, string signOff = "b-agile Team")
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; color: #333333; line-height: 1.6; background-color: #f0f0f0; }}
        .wrapper {{ max-width: 650px; margin: 20px auto; background-color: #ffffff; border-radius: 4px; overflow: hidden; }}
        .header {{ background-color: #1a2332; padding: 24px 30px; text-align: center; }}
        .header img {{ max-width: 180px; height: auto; display: block; margin: 0 auto; }}
        .content {{ padding: 32px 30px; }}
        .content h2, .content h3 {{ color: #1a2332; margin-top: 24px; margin-bottom: 8px; }}
        .content a {{ color: #e8792b; text-decoration: none; }}
        .content a:hover {{ text-decoration: underline; }}
        .content ul {{ padding-left: 20px; margin: 8px 0; }}
        .content li {{ margin-bottom: 4px; }}
        .footer {{ background-color: #f5f5f5; border-top: 1px solid #e0e0e0; padding: 24px 30px; text-align: center; font-size: 13px; color: #666666; }}
        .footer p {{ margin: 4px 0; }}
        .footer a {{ color: #e8792b; text-decoration: none; }}
        .footer a:hover {{ text-decoration: underline; }}
        .footer .links {{ margin-top: 12px; }}
        .footer .links a {{ margin: 0 6px; }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""header"">
            <img src=""https://www.bagile.co.uk/wp-content/uploads/2021/04/bagile-logo-white.png""
                 alt=""b-agile""
                 onerror=""this.alt='b-agile'"" />
        </div>
        <div class=""content"">
            {bodyContent}
        </div>
        <div class=""footer"">
            <p>Regards<br><strong>{System.Net.WebUtility.HtmlEncode(signOff)}</strong></p>
            <p style=""margin-top: 10px;"">
                <a href=""mailto:info@bagile.co.uk"">info@bagile.co.uk</a> &nbsp;|&nbsp;
                <a href=""https://www.bagile.co.uk"">www.bagile.co.uk</a> &nbsp;|&nbsp;
                +44 20 4552 9823
            </p>
            <p class=""links"">
                <a href=""https://www.linkedin.com/company/bagile"">LinkedIn</a> &middot;
                <a href=""https://www.bagile.co.uk/our-courses/"">Our Courses</a>
            </p>
        </div>
    </div>
</body>
</html>";
    }
}
