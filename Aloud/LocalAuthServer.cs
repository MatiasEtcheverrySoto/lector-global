using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LectorGlobalApp
{
    public static class LocalAuthServer
    {
        public static async Task<string> ListenForAuthCallbackAsync(string redirectUri, int timeoutSeconds = 300)
        {
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(redirectUri.EndsWith("/") ? redirectUri : redirectUri + "/");
                listener.Start();

                var tcs = new TaskCompletionSource<string>();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        string tokenData = "";
                        while (listener.IsListening)
                        {
                            var context = await listener.GetContextAsync();
                            var request = context.Request;
                            var response = context.Response;

                            if (request.Url.AbsolutePath.Contains("/api/token"))
                            {
                                if (request.HttpMethod == "POST")
                                {
                                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                                    {
                                        tokenData = await reader.ReadToEndAsync();
                                    }
                                    byte[] okBuffer = Encoding.UTF8.GetBytes("OK");
                                    response.ContentLength64 = okBuffer.Length;
                                    await response.OutputStream.WriteAsync(okBuffer, 0, okBuffer.Length);
                                    response.Close();
                                }
                                else if (request.HttpMethod == "GET")
                                {
                                    string responseString = @"
                                    <html>
                                    <head>
                                        <title>Autenticación Completada</title>
                                        <style>
                                            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0F172A; color: white; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }
                                            .card { background-color: #1E293B; padding: 40px; border-radius: 12px; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1); text-align: center; }
                                            h2 { color: #10B981; margin-bottom: 10px; }
                                            p { color: #94A3B8; }
                                        </style>
                                    </head>
                                    <body>
                                        <div class='card'>
                                            <h2>✅ Autenticación Exitosa</h2>
                                            <p>Ya puedes cerrar esta pestaña y volver a la aplicación Lector Global.</p>
                                        </div>
                                        <script>setTimeout(function(){window.close();}, 2000);</script>
                                    </body>
                                    </html>";
                                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                                    response.ContentLength64 = buffer.Length;
                                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                    response.Close();

                                    if (!string.IsNullOrEmpty(tokenData))
                                    {
                                        tcs.TrySetResult(tokenData);
                                    }
                                }
                            }
                            else
                            {
                                string html = @"
                                <html>
                                <head>
                                    <title>Autorizando...</title>
                                    <style>
                                        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0F172A; color: white; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }
                                        .card { background-color: #1E293B; padding: 40px; border-radius: 12px; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1); text-align: center; }
                                        h2 { color: #3B82F6; margin-bottom: 10px; }
                                    </style>
                                </head>
                                <body>
                                    <div class='card'>
                                        <h2>⏳ Iniciando sesión...</h2>
                                        <p style='color: #94A3B8;'>Por favor, espera un momento.</p>
                                    </div>
                                    <script>
                                        const hash = window.location.hash;
                                        const query = window.location.search;
                                        const data = hash ? hash : query;
                                        fetch('api/token', {
                                            method: 'POST',
                                            headers: { 'Content-Type': 'text/plain' },
                                            body: data
                                        }).then(() => {
                                            window.location.href = 'api/token?success=1';
                                        });
                                    </script>
                                </body>
                                </html>";
                                byte[] buffer = Encoding.UTF8.GetBytes(html);
                                response.ContentLength64 = buffer.Length;
                                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                response.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutSeconds * 1000));
                listener.Stop();

                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    throw new TimeoutException("La autenticación expiró por inactividad.");
                }
            }
        }
    }
}
