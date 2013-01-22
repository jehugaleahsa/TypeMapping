using System;

namespace TypeMapping
{
    /// <summary>
    /// Retrieves a mapping from a source type to a destination type.
    /// </summary>
    public static class Map
    {
        /// <summary>
        /// Specifies the source object that the mapping will use.
        /// </summary>
        /// <typeparam name="TFrom">The type of the source object.</typeparam>
        /// <param name="source">The source object to map from.</param>
        /// <returns>An object that will allow the destination type to be specified.</returns>
        public static IFromMapping<TFrom> From<TFrom>(TFrom source)
        {
            return new FromMapping<TFrom>(source);
        }
    }
}
