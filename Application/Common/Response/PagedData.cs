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
        public int TotalRecords { get; set; } = 0;

        public PagedData(ICollection<T>? rows, int pageSize, int pageNumber, int totalRecords)
        {
            Rows = rows;
            PageSize = pageSize;
            PageNumber = pageNumber;
            TotalRecords = totalRecords;
        }
    }
}
