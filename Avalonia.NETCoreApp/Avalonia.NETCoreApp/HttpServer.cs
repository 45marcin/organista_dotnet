using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServerModule
{
    public interface HttpRequest
    {
        event EventHandler newRequest;
    }

    public class HttpRequestsArgs : EventArgs
    {
        public HttpListenerResponse response;
        public HttpListenerRequest request;
    }
    
    public class HttpServer : HttpRequest
    {
        
        
        public HttpServer()
        {
            Thread x = new Thread(run);
            x.Start();
        }

        public void run()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine ("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            
            
            
            
            
            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            listener.Prefixes.Add("http://*:9000/");
            
            listener.Start();
            while (true)
            {
                Console.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.
                
                newRequestHandle(new HttpRequestsArgs()
                {
                    response =  response,
                    request = request
                });
                //Avalonia.Threading.Dispatcher.UIThread.Post(() => { Play();});
            }
            listener.Stop();
        }

        public async void newRequestHandle(HttpRequestsArgs e)
        {
            OnRequest(e);
        }
        
        protected virtual async void OnRequest(HttpRequestsArgs e)
        {
            try
            {
                newRequest?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                
            }

        }
        
        
        public event EventHandler newRequest;
        
        
        
        public static async void SendRespone(HttpListenerResponse response, string body, int responseCode)
        {
            response.StatusCode = responseCode;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
            
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer,0,buffer.Length);
            output.Close();
        }
    }
    
    
}