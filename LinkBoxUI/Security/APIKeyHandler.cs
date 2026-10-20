using LinkBoxUI.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace LinkBoxUI.Security
{
    public class APIKeyHandler : DelegatingHandler
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage
            request, CancellationToken cancellationToken)
        {
            bool isValidAPIKey = false;
            IEnumerable<string> Headers;

            var checApiKeyExists = request.Headers.TryGetValues("x-api-key", out Headers);

            if (!request.RequestUri.ToString().ToLower().Contains("apikey"))
                return await base.SendAsync(request, cancellationToken);

            if (checApiKeyExists)
            {
                string apiKeyClient = Headers.FirstOrDefault();
                isValidAPIKey = _context.Users.Count(x => x.IsActive == true && x.UserAPIKey.Equals(apiKeyClient)) > 0;
            }

            if (!isValidAPIKey)
                return request.CreateResponse(HttpStatusCode.Forbidden, "Unauthorized API key");

            var response = await base.SendAsync(request, cancellationToken);

            return response;
        }
    }
}