using System.Text.Json;
using System.Text.RegularExpressions;
using InstaMailChecker.Models;

namespace InstaMailChecker
{
    public class Program
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly string art = @"
       /\       
      /  \      
     /,--.\     
    /< () >\    
   /  `--'  \   
  /          \  
 /            \ 
/______________\";

        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(art);
            Console.ResetColor();

            if (args.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Specify target mail!");
                Console.ResetColor();
                return;
            }

            var mail = args[0].Trim();
            var hash = await GetHash();
            await CheckMail(hash, mail, "@joaostack");
        }

        public static async Task<string> GetHash()
        {
            var response = await _client.GetAsync("https://api.cafeeazam.com/api/user/getHash");
            var json = await response.Content.ReadAsStringAsync();
            var hash = JsonSerializer.Deserialize<HashModel>(json);

            return hash?.hash ?? Guid.NewGuid().ToString();
        }

        public static async Task CheckMail(string hash, string mail, string testPass)
        {
            try
            {
                var guid = Guid.NewGuid().ToString();
                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
                _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                _client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                _client.DefaultRequestHeaders.Add("Connection", "keep-alive");

                var signUp = await _client.GetAsync("https://www.instagram.com/accounts/emailsignup/");
                var signUpBody = await signUp.Content.ReadAsStringAsync();
                string pattern = "\"csrf_token\":\"(.*?)\"";
                Match match = Regex.Match(signUpBody, pattern);

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add("Host", "i.instagram.com");
                _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                _client.DefaultRequestHeaders.Add("User-Agent", "Instagram 6.12.1 Android (29/10; 480dpi; 1080x2137; HUAWEI/HONOR; JSN-L22; HWJSN-H; kirin710; en_SA)");
                _client.DefaultRequestHeaders.Add("Accept-Language", "en-SA, en-US");
                _client.DefaultRequestHeaders.Add("X-IG-Connection-Type", "WIFI");
                _client.DefaultRequestHeaders.Add("X-IG-Capabilities", "AQ==");

                var version = Environment.GetEnvironmentVariable("Version") ?? string.Empty;
                _client.DefaultRequestHeaders.Add("Cookie2", version + "=1");

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

                    var login = await _client.PostAsync("https://i.instagram.com/api/v1/accounts/login/", content);
                    var response = await login.Content.ReadAsStringAsync();

                    if (response.Contains("bad_password"))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[+] REGISTERED => {mail}");
                    }
                    else if (response.Contains("invalid_user"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[-] NOT REGISTERED => {mail}");
                    }
                    else if (response.Contains("Please wait a few minutes before you try again."))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"ERROR: Blocked by instagram, try later!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {response}");
                    }
                }
                else
                {
                    Console.WriteLine("No csrf token found ):");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}