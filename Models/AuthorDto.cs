﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public class AuthorDto
    {
        public Guid Id { get; set; }

        public String Name { get; set; }

        public int Age { get; set; }

        public String Genre { get; set; }
    }

    public class AuthorForCreationDto
    {
        public String FirstName { get; set; }

        public String LastName { get; set; }

        public DateTimeOffset DateOfBirth { get; set; }

        public String Genre { get; set; }

        public ICollection<BookForCreationDto> Books { get; set; }
            = new List<BookForCreationDto>();
    }
    //new dto after versioning
    public class AuthorForCreationWithDateOfDeathDto
    {
        public String FirstName { get; set; }

        public String LastName { get; set; }

        public DateTimeOffset DateOfBirth { get; set; }

        public DateTimeOffset DateOfDeath { get; set; }

        public String Genre { get; set; }

        public ICollection<BookForCreationDto> Books { get; set; }
            = new List<BookForCreationDto>();
    }
}
