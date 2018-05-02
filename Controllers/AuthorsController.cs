using Library.API.Models;
using Library.API.Services;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Library.API.Entities;

namespace Library.API.Controllers
{
    //Best practice is to always implement paging on collections to avoid performance issues
    // Paging should be the default, even if not requested by client
    // parameters passed through query string

    //Additional options:  expanding child resources, complex filters - don't need to implement options unless needed though

    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private ILibraryRepository _libraryRepository;
        private IUrlHelper _urlHelper;
        private IPropertyMappingService _propertyMappingService;
        private ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository, IUrlHelper urlHelper, 
                IPropertyMappingService propertyMappingService,
                ITypeHelperService typeHelperService)
        {
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }         

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters)  //will look for properties within the class to map to
        {
            //validate order by mappings
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
                return BadRequest();

            if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
                return BadRequest();

            //need to return metadata for pagination as well
            // should not be in the body, or the response body will not match the accept header and will break REST
            //when requesting application/json, paging metadata isn't part of resource representation
            var authorsFromRepo = _libraryRepository.GetAuthors(authorsResourceParameters);

            //check if there is a previous or next page...if so create the link
            //add all parameters (searching, filtering, etc..  to the create uri method as well)
            var previousPageLinkUri = authorsFromRepo.HasPrevious ?
                    CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) : null;

            var nextPageLinkUri = authorsFromRepo.HasNext ?
                    CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) : null;

            //Create the paging metadata
            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink = previousPageLinkUri,
                nextPageLink = nextPageLinkUri
            };

            //add metadata as custom header to response
            Response.Headers.Add("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

            var authors = authorsFromRepo.ConvertToAuthorDtoList();
            //return Ok(authors);
            //add data shaping to avoid unnecessary fields
            return Ok(authors.ShapeData(authorsResourceParameters.Fields));
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id, [FromQuery]string fields)  //don't need the full parameter object for only one query value
        {
            // Note -> if allowing data shaping without HATEOS, you could violate REST if user can omit the uri
            //validate fields
            if (!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
                return BadRequest();

            //No need for try/catch with global exception handling in configure startup
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            }
            var author = authorFromRepo.ConvertToAuthorDto();
            //return Ok(author);
            return Ok(author.ShapeData(fields));

        }

        [HttpPost()]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
                return BadRequest();

            var authorEntity = author.ConvertToAuthorEntity();
            _libraryRepository.AddAuthor(authorEntity);
            if(!_libraryRepository.Save())
            {
                throw new Exception();  //with global exception handling, we can throw exception
                //return StatusCode(500, "problem");
            }
            var authorToReturn = authorEntity.ConvertToAuthorDto();
            //need a name on the get method call to use it here
            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn); 
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            //Post to this URI should not create a resource, but if the guid exists we should return a conflict
            if (_libraryRepository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            else return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorToDelete = _libraryRepository.GetAuthor(id);
            if (authorToDelete == null)
                return NotFound();
            _libraryRepository.DeleteAuthor(authorToDelete);
            if(!_libraryRepository.Save())
            {
                throw new Exception("error deleting");
            }
            return NoContent();
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters,
             ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors",
                      new
                      {
                          fields = authorsResourceParameters.Fields,
                          orderBy = authorsResourceParameters.OrderBy,
                          searchQuery = authorsResourceParameters.SearchQuery,
                          genre = authorsResourceParameters.Genre,
                          pageNumber = authorsResourceParameters.PageNumber - 1,
                          pageSize = authorsResourceParameters.PageSize
                      });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                      new
                      {
                          fields = authorsResourceParameters.Fields,
                          orderBy = authorsResourceParameters.OrderBy,
                          searchQuery = authorsResourceParameters.SearchQuery,
                          genre = authorsResourceParameters.Genre,
                          pageNumber = authorsResourceParameters.PageNumber + 1,
                          pageSize = authorsResourceParameters.PageSize
                      });

                default:
                    return _urlHelper.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                        orderBy = authorsResourceParameters.OrderBy,
                        searchQuery = authorsResourceParameters.SearchQuery,
                        genre = authorsResourceParameters.Genre,
                        pageNumber = authorsResourceParameters.PageNumber,
                        pageSize = authorsResourceParameters.PageSize
                    });
            }
        }
    }
}
