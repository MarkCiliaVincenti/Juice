﻿namespace Juice.AspNetCore.Models
{
    public class DatasourceResult<T>
    {
        /// <summary>
        /// Current page number, min value is 1
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Current page size
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Data set of current page
        /// </summary>
        public IReadOnlyCollection<T> Data { get; set; } = Array.Empty<T>();

        /// <summary>
        /// Total count of data set without pagination
        /// </summary>
        public long Count { get; set; }
    }
}
