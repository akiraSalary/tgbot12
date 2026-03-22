using System.Collections.Generic;
using System.Linq;

namespace ToDoListBot.Helpers
{
    public static class EnumerableExtension
    {
        /// <summary>
        /// Возвращает подмножество элементов из последовательности (пагинация)
        /// </summary>
        /// <typeparam name="T">Тип элементов</typeparam>
        /// <param name="source">Исходная коллекция</param>
        /// <param name="batchSize">Размер одной страницы</param>
        /// <param name="batchNumber">Номер страницы (начиная с 0)</param>
        /// <returns>Элементы текущей страницы</returns>
        public static IEnumerable<T> GetBatch<T>(this IEnumerable<T> source, int batchSize, int batchNumber)
        {
            return source
                .Skip(batchNumber * batchSize)
                .Take(batchSize);
        }
    }
}