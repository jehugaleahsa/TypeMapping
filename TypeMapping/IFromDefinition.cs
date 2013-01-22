using System;

namespace TypeMapping
{
    /// <summary>
    /// Indicates that a mapping will be defined from the type.
    /// </summary>
    /// <typeparam name="TFrom">The type of the source object.</typeparam>
    public interface IFromDefinition<TFrom>
    {
        /// <summary>
        /// Defines a mapping from the source type to the destination type.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <returns>The mapping definition object.</returns>
        IMapperDefinition<TFrom, TTo> To<TTo>();

        /// <summary>
        /// Defines a mapping from the source type to the destination type, identified
        /// with the given identifier.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <param name="identifier">The identifier to use to distinguish this mapping definition from others with the same type parameters.</param>
        /// <returns>The mapping definition.</returns>
        IMapperDefinition<TFrom, TTo> To<TTo>(string identifier);
    }
}
