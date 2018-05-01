using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Models;
using Library.API.Services;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    //Best practice is to always implement paging on collections to avoid performance issues
    // Paging should be the default, even if not requested by client
    // parameters passed through query string

    [Route("api/authorcollections")]
    public class AuthorCollectionsController : Controller
    {
        private ILibraryRepository _libraryRepository;
        public AuthorCollectionsController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        [HttpPost]
        public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            if (authorCollection == null)
                return BadRequest();

            var authorEntities = authorCollection.ConvertToAuthorList();
            foreach(var author in authorEntities)
            {
                _libraryRepository.AddAuthor(author);
            }
            _libraryRepository.Save();

            var authorCollectionToReturn = authorEntities.ConvertToAuthorDtoList();
            var idsAsString = String.Join(",", authorCollectionToReturn.Select(a => a.Id));
            return CreatedAtRoute("GetAuthorCollection", new { id = idsAsString }, authorCollectionToReturn);  // return a route with a list of id's needs the collection return
            //return Ok();
        }

        // (key1, key2, ..)
        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
                return BadRequest();
            var authorEntities = _libraryRepository.GetAuthors(ids);
            if (ids.Count() != authorEntities.Count())
                return NotFound();

            var authorsToReturn = authorEntities.ConvertToAuthorDtoList();
            return Ok(authorsToReturn);
        }

        [HttpDelete()]
        public IActionResult DeleteAuthors()
        {
            //rarely implemented because it can be very destructive
            return NoContent();
        }
    }
}
