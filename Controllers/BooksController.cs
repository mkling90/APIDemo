using Library.API.Services;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private ILibraryRepository _libraryRepository;
        ILogger<BooksController> _logger;
        private IUrlHelper _urlHelper; // to build links for hateoas

        public BooksController(ILibraryRepository libraryRepository, 
            ILogger<BooksController> logger,
            IUrlHelper urlHelper)
        {
            _logger = logger;
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetBooks")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);
            var booksForAuthor = booksForAuthorFromRepo.ConvertToBookDtoList();

            booksForAuthor = booksForAuthor.Select(book =>
                {
                    book = CreateLinksForBook(book);
                    return book;
                });
            var wrapper = new LinkedCollectionResourceWrapperDto<BookDto>(booksForAuthor);

            //return Ok(booksForAuthor);
            //need links on both the individual books, as well as the links
            return Ok(CreateLinksForBooks(wrapper));
        }

        [HttpGet("{id}", Name ="GetBook")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var book = _libraryRepository.GetBookForAuthor(authorId, id);
            if (book == null)
                return NotFound();
            var retBook = book.ConvertToBookDto();
            //return Ok(retBook);
            return Ok(CreateLinksForBook(retBook)); // with the hateoas links
        }

        [HttpPost(Name = "CreateBookForAuthor")]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
                return BadRequest();
            //custom validation logic
            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForCreationDto), "Descript and Title must be different");
            }
            //validation
            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState); //422

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookEntity = book.ConvertToBookEntity();
            _libraryRepository.AddBookForAuthor(authorId, bookEntity);
            _libraryRepository.Save();
            var bookToReturn = bookEntity.ConvertToBookDto();
            //return CreatedAtRoute("GetBook", new {authorId = bookToReturn.AuthorId, id = bookToReturn.Id }, bookToReturn);
            return CreatedAtRoute("GetBook", new { authorId = bookToReturn.AuthorId, id = bookToReturn.Id }, CreateLinksForBook(bookToReturn));
        }

        [HttpDelete("{id}", Name = "DeleteBook")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();
            
            var book = _libraryRepository.GetBookForAuthor(authorId, id);
            if (book == null)
                return NotFound();

            _libraryRepository.DeleteBook(book);
            if (!_libraryRepository.Save())
                throw new Exception("Error deleting book");  //api wide exception handling will handle exception

            _logger.LogInformation(100, $"Deleted book{id}");

            return NoContent();
        }

        //Use 'Put' for a full update, fields not set go to default values
        // Note, put is not used often, 'Patch' methods typically used so consumer doesn't have to send over all the fields
        [HttpPut("{id}", Name = "UpdateBook")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book )
        {
            if (book == null)
                return BadRequest();
            // ok for now, but should look for real code at FluentValidation or something similar to put validation in the same place
            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto), "Descript and Title must be different");
            }
            //validation
            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState); //422

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();
            var bookFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookFromRepo == null)
            {
                // return NotFound(); <- if you do not allow upserting, use this
                var bookToAdd = book.ConvertToBookEntity();
                bookToAdd.Id = id;  //use the id from the URI for upserting the resource
                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);
                if (!_libraryRepository.Save())
                    throw new Exception("Error creating book");
                var bookToReturn = bookToAdd.ConvertToBookDto();
                return CreatedAtRoute("GetBook", new { authorId = bookToReturn.AuthorId, id = bookToReturn.Id }, bookToReturn);
            }
            //update
            bookFromRepo.UpdateBookEntity(book);
            // always have a set of methods matching the functionality, even if the current implementation doesn't do anything
            _libraryRepository.UpdateBookForAuthor(bookFromRepo);
            if(!_libraryRepository.Save())
                throw new Exception("Error updating book");  //api wide exception handling will handle exception
            return NoContent();  //could return OK() also

        }

        //Use patch for partial updates
        [HttpPatch("{id}", Name = "PartiallyUpdateBook")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();
            var bookFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookFromRepo == null)
            {
                //return NotFound(); <- if not upserting
                var bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto);
                var bookToAdd = bookDto.ConvertToBookEntity();
                bookToAdd.Id = id;
                if (!_libraryRepository.Save())
                    throw new Exception("Error creating book");
                var bookToReturn = bookToAdd.ConvertToBookDto();
                return CreatedAtRoute("GetBook", new { authorId = bookToReturn.AuthorId, id = bookToReturn.Id }, bookToReturn);
            }
            var bookToPatch = bookFromRepo.ConvertToBookForUpdateDto();

            //If we pass in ModelState, errors in the patchdoc will make the model state invalid
            patchDoc.ApplyTo(bookToPatch, ModelState);
            //patchDoc.ApplyTo(bookToPatch);

            //add validation
            //Need to explicitly call TryValidateModel, because it came in as a PatchDoc, not as a dto, so the data annotations werent evaluated
            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            //update entity
            bookFromRepo.UpdateBookEntity(bookToPatch);

            _libraryRepository.UpdateBookForAuthor(bookFromRepo);
            if (!_libraryRepository.Save())
                throw new Exception("Error updating book");  //api wide exception handling will handle exception
            return NoContent();  //could return OK() also
        }

        //take in a bookdto, and add the links to it
        private BookDto CreateLinksForBook(BookDto book)
        {
            //custom logic to decide what links should be included.
            //first should be link to itself.  other links should be for all the functionality the api exposes
            // here, when returning a single book, the api allows deleting, updating, and partial patching
            //all the links  must have names!

            //link to itself
            book.Links.Add(new LinkDto(
                _urlHelper.Link("GetBook", new { id = book.Id } ),  //href
                "self",  //rel
                "GET"));  //method

            book.Links.Add(new LinkDto(
                _urlHelper.Link("DeleteBook", new { id = book.Id }),  //href
                "delete_book",  //rel
                "DELETE"));  //method

            book.Links.Add(new LinkDto(
                _urlHelper.Link("UpdateBook", new { id = book.Id }),  //href
                "update_book",  //rel
                "PUT"));  //method

            book.Links.Add(new LinkDto(
                _urlHelper.Link("PartiallyUpdateBook", new { id = book.Id }),  //href
                "partially_update_book",  //rel
                "PATCH"));  //method

            return book;
        }

        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
        {
            //Same concept, add links to itself, plus anything else you do to collection
            //link to itself
            booksWrapper.Links.Add(new LinkDto(
                _urlHelper.Link("GetBooks", new { }),  //href
                "self",  //rel
                "GET"));  //method

            return booksWrapper;
        }
    }
}
