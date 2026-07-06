using System;
using System.Threading.Tasks;
using Supabase;
using Supabase.Gotrue;

namespace LectorGlobalApp
{
    public class TestOAuth
    {
        public static async Task Test()
        {
            var options = new SignInOptions { RedirectTo = "http://localhost:54321/auth/callback/" };
            var state = await SupabaseManager.Client.Auth.SignIn(Supabase.Gotrue.Constants.Provider.Google, options);
            Console.WriteLine(state.Uri);
        }
    }
}
