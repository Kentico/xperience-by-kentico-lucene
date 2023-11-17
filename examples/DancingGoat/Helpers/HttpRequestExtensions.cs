
using System;

using Microsoft.AspNetCore.Http;

namespace DancingGoat.Helpers
{
    public static class HttpRequestExtensions
    {
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Headers != null && request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return true;
            }

            return false;
        }
    }
}
