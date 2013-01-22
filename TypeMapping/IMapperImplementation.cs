using System;

namespace TypeMapping
{
    /// <summary>
    /// Builds an object by mapping values from another object.
    /// </summary>
    /// <typeparam name="TFrom">The type of the source object.</typeparam>
    /// <typeparam name="TTo">The type of the destination object.</typeparam>
    internal interface IMapperImplementation<TFrom, TTo>
    {
        /// <summary>
        /// Creates a new destination object and maps the source object to it using the specified configuration.
        /// </summary>
        /// <param name="from">The source object.</param>
        /// <returns>The destination object.</returns>
        TTo Map(TFrom from);

        /// <summary>
        /// Maps the source object to the given destination object using the specified configuration.
        /// </summary>
        /// <param name="from">The source object.</param>
        /// <param name="to">The destination object.</param>
        /// <returns>The destination object.</returns>
        TTo Map(TFrom from, TTo to);
    }
}
