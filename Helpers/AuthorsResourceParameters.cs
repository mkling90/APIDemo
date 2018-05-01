using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    //Helper class to manage parameters for paging, etc..
    public class AuthorsResourceParameters
    {
        const int maxPageSize = 20;
        public int PageNumber { get; set; } = 1;

        private int _PageSize = 10;
        public int PageSize
        {
            get => _PageSize;
            set
            {
                _PageSize = (value > maxPageSize) ? maxPageSize : value;
            }
        }

        public string Genre { get; set; }

        public string SearchQuery { get; set; }
    }
}
