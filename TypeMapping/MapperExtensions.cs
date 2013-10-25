using System;
using System.Collections.Generic;

namespace TypeMapping
{
    /// <summary>
    /// Provides extension methods to help define mappings.
    /// </summary>
    public static class MapperExtensions
    {
        /// <summary>
        /// The given setter function will be called for each item in the source.
        /// </summary>
        /// <typeparam name="TFrom">The type of the source object.</typeparam>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <typeparam name="TValue">The type of the values in the source.</typeparam>
        /// <param name="definition">The mapping definition to extend.</param>
        /// <param name="setter">A function that maps the source values to the destination objects.</param>
        /// <returns>The current mapper.</returns>
        /// <exception cref="System.ArgumentNullException">The definition is null.</exception>
        public static IMapperDefinition<TFrom, TTo> MapMany<TFrom, TTo, TValue>(this IMapperDefinition<TFrom, TTo> definition, Action<TTo, TValue> setter)
            where TFrom : IEnumerable<TValue>
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            return definition.MapMany(from => from, setter);
        }

        /// <summary>
        /// The given setter function will be called for each item in the source.
        /// </summary>
        /// <typeparam name="TFrom">The type of the source object.</typeparam>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <typeparam name="TValue">The type of the values in the source.</typeparam>
        /// <param name="definition">The mapping definition to extend.</param>
        /// <param name="setter">
        /// A function that maps the source values to the destination objects. A zero-based count for how many times the function is called will be passed.
        /// </param>
        /// <returns>The current mapper.</returns>
        /// <exception cref="System.ArgumentNullException">The definition is null.</exception>
        public static IMapperDefinition<TFrom, TTo> MapMany<TFrom, TTo, TValue>(this IMapperDefinition<TFrom, TTo> definition, Action<TTo, int, TValue> setter)
            where TFrom : IEnumerable<TValue>
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            return definition.MapMany(from => from, setter);
        }

    }
}
