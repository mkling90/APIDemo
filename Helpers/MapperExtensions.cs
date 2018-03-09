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

        public static IEnumerable<AuthorDto> ConvertToAuthorDtoList(this IEnumerable<Author> inputAuthorList)
        {
            List<AuthorDto> authorDtoList = new List<AuthorDto>();
            foreach(Author inputAuthor in inputAuthorList)
            {
                authorDtoList.Add(inputAuthor.ConvertToAuthorDto());
            }
            return authorDtoList;
        }


    }
}
