using Library.API.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    // To support root document so consumer can learn to interact with api

    //Use Col (Code on Demand) to adapt to changes in media types or resource representations -> not always realistic though
    [Route("api")]
    public class RootController : Controller
    {
        private IUrlHelper _urlHelper;

        public RootController(IUrlHelper urlHelper)
        {
            _urlHelper = urlHelper;
        }

        [HttpGet(Name ="GetRoot")]
        public IActionResult GetRoot([FromHeader(Name ="Accept")]string mediaType)
        {
            if(mediaType == "application/vnd.mike.hateoas+json")
            {
                //generate links to document itself,and to actions that can happen to uri's at root level
                var links = new List<LinkDto>();

                links.Add(new LinkDto(_urlHelper.Link("GetRoot", new { })
                    , "self",
                    "GET"
                    ));
                links.Add(new LinkDto(_urlHelper.Link("GetAuthors", new { })
                    , "authors",
                    "GET"
                    ));

                //add the create authors, there isn't another place to put that
                links.Add(new LinkDto(_urlHelper.Link("CreateAuthor", new { })
                   , "create_author",
                   "POST"
                   ));
                return Ok(links);
            }
            return NoContent(); // did not request media type for links
        }
    }
}
