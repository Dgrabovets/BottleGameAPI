namespace BottleGame.Application.Common
{
    /// <summary>
    /// Page of a list that is read in parts
    /// </summary>
    /// <param name="Page">Number of the page, starting from 1</param>
    /// <param name="PageSize">Number of items on a page</param>
    public sealed record PageRequest(int Page, int PageSize)
    {
        /// <summary>
        /// Largest allowed page size
        /// </summary>
        public const int MAX_PAGE_SIZE = 200;

        /// <summary>
        /// Page size used when only the page number is given
        /// </summary>
        public const int DEFAULT_PAGE_SIZE = 20;

        /// <summary>
        /// Number of items before the first item of the page
        /// </summary>
        public int Skip => (Page - 1) * PageSize;

        /// <summary>
        /// Builds a page request from the query parameters
        /// </summary>
        /// <param name="page">Page number from the query</param>
        /// <param name="pageSize">Page size from the query</param>
        /// <returns>The page request, or null when no paging was asked for and the whole list is wanted</returns>
        public static PageRequest? Create(int? page, int? pageSize)
        {
            if (page is null && pageSize is null)
            {
                return null;
            }

            return new PageRequest(
                Math.Max(page ?? 1, 1),
                Math.Clamp(pageSize ?? DEFAULT_PAGE_SIZE, 1, MAX_PAGE_SIZE));
        }
    }
}
