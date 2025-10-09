using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Models.DTOs;

namespace PawVerseAPI.Helpers
{
    public static class PaginationHelper
    {
        public static async Task<PagedResult<T>> CreateAsync<T>(
            IQueryable<T> query,
            int pageNumber,
            int pageSize)
        {
            // Ensure valid pagination parameters
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 20 : pageSize;
            pageSize = pageSize > 100 ? 100 : pageSize; // Max page size limit

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>(items, totalItems, pageNumber, pageSize);
        }
    }
}
