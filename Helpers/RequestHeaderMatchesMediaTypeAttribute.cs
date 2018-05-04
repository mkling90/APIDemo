using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System;

namespace Library.API.Controllers
{
    //action constraint, selection action based on content type
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]  //options you can configure on the attribute tage
    internal class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
    {
        private readonly string[] _mediaTypes;
        private readonly string _requestHeaderToMatch;

        public RequestHeaderMatchesMediaTypeAttribute(string requestHeaderToMatch,
            string[] mediaTypes)
        {
            _mediaTypes = mediaTypes;
            _requestHeaderToMatch = requestHeaderToMatch;
        }

        // order constraint for what stage the action constraint is part of, all constraints in the same order run together
        public int Order => 0;

        public bool Accept(ActionConstraintContext context)
        {
            //return true when matches
            var requestHeaders = context.RouteContext.HttpContext.Request.Headers;

            if (!requestHeaders.ContainsKey(_requestHeaderToMatch))
            {
                return false;
            }

            // if one of the media types matches, return true
            foreach (var mediaType in _mediaTypes)
            {
                var mediaTypeMatches = string.Equals(requestHeaders[_requestHeaderToMatch].ToString(),
                    mediaType, StringComparison.OrdinalIgnoreCase);

                if (mediaTypeMatches)
                {
                    return true;
                }
            }

            return false;
        }
    }
}