using System;
using System.Threading.Tasks;
using Supabase;
using Supabase.Gotrue;

namespace LectorGlobalApp
{
    public class SupabaseManager
    {
        private const string SUPABASE_URL = "https://dmkytwayhuoizesibcun.supabase.co";
        private const string SUPABASE_KEY = "sb_publishable_TE4NFcSOCHwELdDIeB6IFQ_sqe466HQ";

        public static Supabase.Client Client { get; private set; }

        public static async Task InitializeAsync()
        {
            if (Client == null)
            {
                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = true
                };
                Client = new Supabase.Client(SUPABASE_URL, SUPABASE_KEY, options);
                await Client.InitializeAsync();
            }
        }

        public static async Task<Session?> LoginAsync(string email, string password)
        {
            if (Client == null) await InitializeAsync();
            return await Client.Auth.SignIn(email, password);
        }

        public static async Task<Session?> SignUpAsync(string email, string password)
        {
            if (Client == null) await InitializeAsync();
            return await Client.Auth.SignUp(email, password);
        }

        public static async Task LogoutAsync()
        {
            if (Client != null && Client.Auth.CurrentSession != null)
            {
                await Client.Auth.SignOut();
            }
        }

        public static async Task<Session?> OAuthLoginAsync(string providerStr)
        {
            if (Client == null) await InitializeAsync();
            try
            {
                var provider = Supabase.Gotrue.Constants.Provider.Google;
                if (providerStr.ToLower() == "apple") provider = Supabase.Gotrue.Constants.Provider.Apple;

                string redirectUrl = "http://localhost:54321/auth/callback/";
                var options = new SignInOptions { RedirectTo = redirectUrl };
                var authState = await Client.Auth.SignIn(provider, options);
                
                if (authState != null && authState.Uri != null)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = authState.Uri.ToString(),
                        UseShellExecute = true
                    });

                    string tokenData = await LocalAuthServer.ListenForAuthCallbackAsync(redirectUrl);
                    
                    if (!string.IsNullOrEmpty(tokenData))
                    {
                        var (access, refresh) = ParseTokens(tokenData);
                        if (!string.IsNullOrEmpty(access) && !string.IsNullOrEmpty(refresh))
                        {
                            return await Client.Auth.SetSession(access, refresh);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("OAuth Error: " + ex.Message);
            }
            return null;
        }

        private static (string access, string refresh) ParseTokens(string urlFragment)
        {
            string access = null, refresh = null;
            var parts = urlFragment.TrimStart('#', '?').Split('&');
            foreach(var part in parts)
            {
                var pair = part.Split('=');
                if (pair.Length == 2)
                {
                    if (pair[0] == "access_token") access = Uri.UnescapeDataString(pair[1]);
                    if (pair[0] == "refresh_token") refresh = Uri.UnescapeDataString(pair[1]);
                }
            }
            return (access, refresh);
        }
        
        public static bool IsLoggedIn => Client?.Auth?.CurrentSession != null;
        public static User? CurrentUser => Client?.Auth?.CurrentUser;
    }
}
