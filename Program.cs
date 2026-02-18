namespace InstaMailChecker;

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.RegularExpressions;

class Program
{
    static async Task Main(string[] args)
    {
        var app = new CommandApp<Commands>();
        await app.RunAsync(args);
    }
}

public class Commands : AsyncCommand<Commands.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-e|--email", isRequired: true)]
        [Description("Target e-mail")]
        public required string TargetMail { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var targetMail = settings.TargetMail;
            var httpClient = new HttpClient();
            var instagram = new Instagram(httpClient);
            var randomPass = new Random().Next(100000000);
            var responseStatus = await instagram.CheckMail(targetMail, randomPass.ToString());

            switch (responseStatus)
            {
                case 200:
                    AnsiConsole.MarkupLine($"[bold blue][[+]][/] [bold green]E-mail '{targetMail}' is UP![/]");
                    break;
                case 429:
                    AnsiConsole.MarkupLine("[bold blue][[-]][/] [bold red]Error 429 (try use different IP address)[/]");
                    break;
                case 404:
                    AnsiConsole.MarkupLine($"[bold blue][[-]][/] [bold red]E-mail '{targetMail}' is DOWN![/]");
                    break;
                case 400:
                    AnsiConsole.MarkupLine("[bold blue][[-]][/] [bold red]Error 400 CSRF Token not found![/]");
                    break;
                case 500:
                    AnsiConsole.MarkupLine("[bold blue][[-]][/] [bold red]Error 500 on instgram[/]");
                    break;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return 0;
    }
}

public class Instagram
{
    private static HttpClient _httpClient { get; set; }

    public Instagram(HttpClient httpClient)
    {
        _httpClient = httpClient;

        // first headers section
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");

        // second headers section
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Host", "i.instagram.com");
        _httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Instagram 6.12.1 Android (29/10; 480dpi; 1080x2137; HUAWEI/HONOR; JSN-L22; HWJSN-H; kirin710; en_SA)");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-SA, en-US");
        _httpClient.DefaultRequestHeaders.Add("X-IG-Connection-Type", "WIFI");
        _httpClient.DefaultRequestHeaders.Add("X-IG-Capabilities", "AQ==");

    }

    public async Task<int> CheckMail(string mail, string testPass)
    {
        try
        {
            // GUIDs
            var guid = Guid.NewGuid().ToString();
            var hash = Guid.NewGuid().ToString();

            // Grep CSRF Token
            var signUp = await _httpClient.GetAsync("https://www.instagram.com/accounts/emailsignup/");
            var signUpBody = await signUp.Content.ReadAsStringAsync();
            var pattern = "\"csrf_token\":\"(.*?)\"";
            Match match = Regex.Match(signUpBody, pattern);
            var version = Environment.GetEnvironmentVariable("Version") ?? string.Empty;
            _httpClient.DefaultRequestHeaders.Add("Cookie2", version + "=1");

            if (match.Success)
            {
                var csrfToken = match.Groups[1].Value;
                var data = new Dictionary<string, string>
                {
                    { "username", mail },
                    { "password", testPass },
                    { "device_id", hash },
                    { "guid", guid },
                    { "_csrftoken", csrfToken }
                };

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("ig_sig_key_version", "4"),
                    new KeyValuePair<string, string>("signed_body", $"{Guid.NewGuid().ToString("N")}.{System.Text.Json.JsonSerializer.Serialize(data)}")
                });

                var login = await _httpClient.PostAsync("https://i.instagram.com/api/v1/accounts/login/", content);
                var response = await login.Content.ReadAsStringAsync();

                if (response.Contains("bad_password"))
                {
                    return 200;
                }

                if (response.Contains("invalid_user"))
                {
                    return 404;
                }
                else if (response.Contains("Please wait a few minutes before you try again."))
                {
                    return 429;
                }
                else
                {
                    return 500;
                }
            }
            else
            {
                return 400;
            }
        }
        catch
        {
            throw;
        }
    }
}
