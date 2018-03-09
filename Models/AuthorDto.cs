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
}
