using nanoFramework.WebServer;
using System.Collections;
using System.Net;
using System.Text;

namespace _0.ValidatePrerequisites
{
    public class HomeController
    {
        [Route("*")]
        [CaseSensitive]
        [Method("GET")]
        public void RoutePostTest(WebServerEventArgs e)
        {
            string route = $"The route asked is {e.Context.Request.RawUrl.TrimStart('/').Split('/')[0]}";
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, route);
        }
    }
}