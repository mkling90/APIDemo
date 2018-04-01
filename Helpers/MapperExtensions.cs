using Library.API.Entities;
using Library.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    public static class MapperExtensions
    {

        public static AuthorDto ConvertToAuthorDto(this Author inputAuthor)
        {
            AuthorDto authorDto = new AuthorDto()
            {
                Name = $"{inputAuthor.FirstName} {inputAuthor.LastName}",
                Age = inputAuthor.DateOfBirth.GetCurrentAge(),
                Genre = inputAuthor.Genre,
                Id = inputAuthor.Id
            };
            return authorDto;
        }

        public static Author ConvertToAuthorEntity(this AuthorForCreationDto inputAuthor)
        {
            Author author = new Author()
            {
                FirstName = inputAuthor.FirstName,
                LastName = inputAuthor.LastName,
                DateOfBirth = inputAuthor.DateOfBirth,
                Genre = inputAuthor.Genre
            };
            if(inputAuthor.Books != null)
            {
                author.Books = inputAuthor.Books.ConvertToBookEntityList();
            }
            return author;
        }

        public static IEnumerable<Author> ConvertToAuthorList(this IEnumerable<AuthorForCreationDto> inputAuthorDtoList)
        {
            List<Author> authorList = new List<Author>();
            foreach(AuthorForCreationDto authorDto in inputAuthorDtoList)
            {
                authorList.Add(authorDto.ConvertToAuthorEntity());
            }
            return authorList;
        }

        public static IEnumerable<AuthorDto> ConvertToAuthorDtoList(this IEnumerable<Author> inputAuthorList)
        {
            List<AuthorDto> authorDtoList = new List<AuthorDto>();
            foreach(Author inputAuthor in inputAuthorList)
            {
                authorDtoList.Add(inputAuthor.ConvertToAuthorDto());
            }
            return authorDtoList;
        }

        public static BookDto ConvertToBookDto(this Book inputBook)
        {
            BookDto bookDto = new BookDto()
            {
                Id = inputBook.Id,
                Title = inputBook.Title,
                Description = inputBook.Description,
                AuthorId = inputBook.AuthorId
            };
            return bookDto;
        }

        public static Book ConvertToBookEntity(this BookForCreationDto inputBook)
        {
            Book bookEntity = new Book()
            {
                Title = inputBook.Title,
                Description = inputBook.Description
            };
            return bookEntity;
        }
        public static IEnumerable<BookDto> ConvertToBookDtoList(this IEnumerable<Book> inputBookList)
        {
            List<BookDto> bookDtoList = new List<BookDto>();
            foreach (Book inputBook in inputBookList)
            {
                bookDtoList.Add(inputBook.ConvertToBookDto());
            }
            return bookDtoList;
        }

        public static ICollection<Book> ConvertToBookEntityList(this ICollection<BookForCreationDto> inputBookList)
        {
            List<Book> bookList = new List<Book>();
            foreach (BookForCreationDto inputBook in inputBookList)
            {
                bookList.Add(inputBook.ConvertToBookEntity());
            }
            return bookList;
        }
    }
}
