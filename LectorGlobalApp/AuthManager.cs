using System;
using System.IO;
using System.Threading.Tasks;

namespace LectorGlobalApp
{
    public class UserAccount
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public string Provider { get; set; } // Email, Google, Apple
    }

    public static class AuthManager
    {
        public static UserAccount CurrentUser { get; private set; }

        public static event Action OnAuthStateChanged;

        static AuthManager()
        {
            _ = TryLoadSessionAsync();
        }

        private static async Task TryLoadSessionAsync()
        {
            await SupabaseManager.InitializeAsync();
            if (SupabaseManager.IsLoggedIn && SupabaseManager.CurrentUser != null)
            {
                var su = SupabaseManager.CurrentUser;
                CurrentUser = new UserAccount
                {
                    Email = su.Email,
                    DisplayName = su.Email.Split('@')[0], 
                    Provider = "Email",
                    AvatarUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(su.Email.Split('@')[0]) + "&background=9D03FB&color=fff"
                };
                OnAuthStateChanged?.Invoke();
            }
        }

        public static async Task<bool> Register(string email, string password, string displayName)
        {
            try
            {
                var session = await SupabaseManager.SignUpAsync(email, password);
                if (session != null && session.User != null)
                {
                    CurrentUser = new UserAccount
                    {
                        Email = session.User.Email,
                        DisplayName = displayName,
                        Provider = "Email",
                        AvatarUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(displayName) + "&background=9D03FB&color=fff"
                    };
                    OnAuthStateChanged?.Invoke();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Register Error: " + ex.Message);
            }
            return false;
        }

        public static async Task<bool> Login(string email, string password)
        {
            try
            {
                var session = await SupabaseManager.LoginAsync(email, password);
                if (session != null && session.User != null)
                {
                    CurrentUser = new UserAccount
                    {
                        Email = session.User.Email,
                        DisplayName = session.User.Email.Split('@')[0],
                        Provider = "Email",
                        AvatarUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(session.User.Email.Split('@')[0]) + "&background=9D03FB&color=fff"
                    };
                    OnAuthStateChanged?.Invoke();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login Error: " + ex.Message);
            }
            return false;
        }

        public static async Task<bool> OAuthLogin(string provider)
        {
            var session = await SupabaseManager.OAuthLoginAsync(provider);
            if (session != null && session.User != null)
            {
                string email = session.User.Email;
                string name = !string.IsNullOrEmpty(email) ? email.Split('@')[0] : "Usuario";
                CurrentUser = new UserAccount
                {
                    Email = email,
                    DisplayName = name,
                    Provider = provider,
                    AvatarUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(name) + "&background=9D03FB&color=fff"
                };
                OnAuthStateChanged?.Invoke();
                return true;
            }
            return false;
        }

        public static async Task Logout()
        {
            await SupabaseManager.LogoutAsync();
            CurrentUser = null;
            OnAuthStateChanged?.Invoke();
        }
    }
}
