using Library.API.Services;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Models;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private ILibraryRepository _libraryRepository;

        public BooksController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        [HttpGet()]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);
            var booksForAuthor = booksForAuthorFromRepo.ConvertToBookDtoList();

            return Ok(booksForAuthor);
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
            return Ok(retBook);
        }

        [HttpPost()]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
                return BadRequest();
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();
            var bookEntity = book.ConvertToBookEntity();
            _libraryRepository.AddBookForAuthor(authorId, bookEntity);
            _libraryRepository.Save();
            var bookToReturn = bookEntity.ConvertToBookDto();
            return CreatedAtRoute("GetBook", new {authorId = bookToReturn.AuthorId, id = bookToReturn.Id }, bookToReturn);
        }
    }
}
