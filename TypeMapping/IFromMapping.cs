using System;

namespace TypeMapping
{
    /// <summary>
    /// Allows a mapping to be performed between the source object and the destination type.
    /// </summary>
    /// <typeparam name="TFrom">The type of the source object.</typeparam>
    public interface IFromMapping<TFrom>
    {
        /// <summary>
        /// Creates a new destination object and uses the source and configured mapper to populate it.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <returns>The mapped destination object.</returns>
        TTo To<TTo>();

        /// <summary>
        /// Creates a new destination object and uses the source and configured mapper to populate it.
        /// The mapping configuration associated with the given identifier will be used.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <param name="identifier">The identifier of the mapping configuration to use.</param>
        /// <returns>The mapped destination object.</returns>
        TTo To<TTo>(string identifier);

        /// <summary>
        /// Uses the source and configured mapper to populate the given destination object.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <param name="destination">The destination object to store the results of the mapping.</param>
        /// <returns>The mapped destination object.</returns>
        TTo To<TTo>(TTo destination);

        /// <summary>
        /// Uses the source and configured mapper to populate the given destination object.
        /// The mappign configuration associated with the given identifier will be used.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <param name="destination">The destination object to store the result of the mapping.</param>
        /// <param name="identifier">The identifier of the mapping configuration to use.</param>
        /// <returns>The mapped destination object.</returns>
        TTo To<TTo>(TTo destination, string identifier);
    }
}
