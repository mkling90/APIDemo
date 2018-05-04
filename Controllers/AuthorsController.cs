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
    // Typically should return links either statically OR dynamically, but not both.  Example API includes both for reference.
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
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters,  //will look for properties within the class to map to
            [FromHeader(Name = "Accept")] string mediaType)  //need to get the accept header to determine if hateoas links should be returned or not  
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
            //can be removed after adding these links to the hateoas links
            /*
            var previousPageLinkUri = authorsFromRepo.HasPrevious ?
                    CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) : null;

            var nextPageLinkUri = authorsFromRepo.HasNext ?
                    CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) : null;
            */
            //Create the paging metadata
            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages
                //previousPageLink = previousPageLinkUri,
                //nextPageLink = nextPageLinkUri
            };

            //add metadata as custom header to response
            Response.Headers.Add("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

            var authors = authorsFromRepo.ConvertToAuthorDtoList();

            //create links
            //need to configure a formatter for the custom media type though
            if (mediaType == "application/vnd.mike.hateoas+json")  //check media type to decide if we should return hateoas links or not
            {
                var links = CreateLinksForAuthors(authorsResourceParameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);

                //shape the data
                var shapedAuthors = authors.ShapeData(authorsResourceParameters.Fields);
                //add individual links to each object
                var shapedAuthorsWithLinks = shapedAuthors.Select(a =>
                    {
                    //each shaped author gets its specific links
                    var authorDict = a as IDictionary<string, object>;
                        var authorLinks = CreateLinksForAuthor((Guid)authorDict["Id"], authorsResourceParameters.Fields);
                        authorDict.Add("links", authorLinks);
                        return authorDict;
                    });
                //create wrapper to return links and collection
                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links = links
                };
                return Ok(linkedCollectionResource);
            }
            else
            {
                //non hateoas code
                //in prod code should also change the pagination data to include the next/previous, since that was changed to be in the hateoas links section
                return Ok(authors.ShapeData(authorsResourceParameters.Fields));
            }
            //return Ok(authors);
            //add data shaping to avoid unnecessary fields
            //return Ok(authors.ShapeData(authorsResourceParameters.Fields));
            //after links
  
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
            // using links here prevents consumer from omitting the uri, even if you omit the id value, you will get the links
            var links = CreateLinksForAuthor(id, fields);
            //to add the links to the dynamic object
            var linkedResourceToReturn = author.ShapeData(fields)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);
            //return Ok(author);
            //return Ok(author.ShapeData(fields));  //after data shaping
            return Ok(linkedResourceToReturn);  //after hateoas
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-type", new[] { "application/vnd.mike.author.full+json" })]  //with versioning media types
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
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            //add links to post author, convert authordto to expandoobject
            var linkedResourceToReturn = authorToReturn.ShapeData() as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", links);

            //need a name on the get method call to use it here
            //return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn); 
            //with links
            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
        }


        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-type", new[] { "application/vnd.mike.author.authorwithdateofdeath+json" })]  //action constraint, selection action based on content type
        public IActionResult CreateAuthorWithDateOfDeath([FromBody] AuthorForCreationWithDateOfDeathDto author)
        {
            if (author == null)
                return BadRequest();

            var authorEntity = author.ConvertToAuthorEntity();
            _libraryRepository.AddAuthor(authorEntity);
            if (!_libraryRepository.Save())
            {
                throw new Exception();  //with global exception handling, we can throw exception
                //return StatusCode(500, "problem");
            }
            var authorToReturn = authorEntity.ConvertToAuthorDto();
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            //add links to post author, convert authordto to expandoobject
            var linkedResourceToReturn = authorToReturn.ShapeData() as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", links);

            //need a name on the get method call to use it here
            //return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn); 
            //with links
            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
        }
        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            //Post to this URI should not create a resource, but if the guid exists we should return a conflict
            if (_libraryRepository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            else return NotFound();
        }

        [HttpDelete("{id}", Name = "DeleteAuthor")]
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
                case ResourceUriType.Current:
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

        //For dynamic HATEOAS approach
        //parameters should match the input parameters of GetAuthor (or whatever method returns the dynamic object
        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            //need variable to hold the links
            var links = new List<LinkDto>();

            //create self link first
            if(string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(
                _urlHelper.Link("GetAuthor", new {id = id }),  //href
                "self",  //rel
                "GET"));  //method
            }
            else
            {
                links.Add(new LinkDto(
                _urlHelper.Link("GetAuthor", new { id = id , fields = fields}),  //href
                "self",  //rel
                "GET"));  //method
            }

            //add other operations (delete, etc...)
            links.Add(new LinkDto(
                _urlHelper.Link("DeleteAuthor", new { id = id }),  //href
                "delete_author",  //rel
                "DELETE"));  //method

            //create book for an author?  
            //To drive the application state, even though it's in another controller, we can add the functionality to the links here
            links.Add(new LinkDto(
                _urlHelper.Link("CreateBookForAuthor", new { authorId = id }),  //href
                "create_book_for__author",  //rel
                "POST"));  //method

            //can add other links to help the consumer navigate throught the api
            links.Add(new LinkDto(
                _urlHelper.Link("GetBook", new { authorId = id }),  //href
                "books",  //rel
                "GET"));  //method

            return links;
        }

        //helper method for author collection
        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters,
            bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();

            //already have a help method to create a uri, so should reuse it
            //self links
            links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.Current),
                "self",
                "GET"));
            //Previously the header metadata for paging had the next/previous links.  These should go here though.
            if(hasNext)
            {
                links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage),
                "nextPage",
                "GET"));
            }

            if(hasPrevious)
            {
                links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage),
                "previousPage",
                "GET"));
            }


            return links;
        }
    }
}
