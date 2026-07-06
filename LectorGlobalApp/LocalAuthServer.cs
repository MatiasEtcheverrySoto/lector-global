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
                        while (listener.IsListening)
                        {
                            var context = await listener.GetContextAsync();
                            var request = context.Request;
                            var response = context.Response;

                            if (request.Url.AbsolutePath.Contains("/api/token"))
                            {
                                string tokenData = "";
                                if (request.HttpMethod == "POST")
                                {
                                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                                    {
                                        tokenData = await reader.ReadToEndAsync();
                                    }
                                }
                                
                                string responseString = "<html><body><h2>Autenticación completada. Ya puedes volver a la aplicación Lector Global.</h2><script>setTimeout(function(){window.close();}, 2000);</script></body></html>";
                                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                                response.ContentLength64 = buffer.Length;
                                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                response.Close();

                                if (!string.IsNullOrEmpty(tokenData))
                                {
                                    tcs.TrySetResult(tokenData);
                                }
                            }
                            else
                            {
                                string html = @"
                                <html>
                                <head><title>Autorizando...</title></head>
                                <body>
                                    <h2>Iniciando sesión...</h2>
                                    <script>
                                        const hash = window.location.hash;
                                        const query = window.location.search;
                                        const data = hash ? hash : query;
                                        fetch('/api/token', {
                                            method: 'POST',
                                            headers: { 'Content-Type': 'text/plain' },
                                            body: data
                                        }).then(() => {
                                            window.location.href = '/api/token?success=1';
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
