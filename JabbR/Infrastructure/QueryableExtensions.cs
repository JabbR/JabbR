using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JabbR.Infrastructure
{
    public static class QueryableExtensions
    {
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source)
        {
            try
            {
                // HACK: ZOMG we should make this more efficient
                return System.Data.Entity.QueryableExtensions.ToListAsync(source);
            }
            catch
            {
                return TaskAsyncHelper.FromResult(source.ToList());
            }
        }
    }
}