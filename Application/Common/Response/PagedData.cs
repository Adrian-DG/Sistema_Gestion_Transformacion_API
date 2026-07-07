using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Response
{
    public class PagedData<T> where T : class
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public ICollection<T>? Rows { get; set; }

        public PagedData(ICollection<T>? rows, int pageSize, int pageNumber)
        {
            Rows = rows;
            PageSize = pageSize;
            PageNumber = pageNumber;
        }
    }
}
