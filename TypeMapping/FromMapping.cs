using System;

namespace TypeMapping
{
    /// <summary>
    /// Indicates that a mapping will be performed using the specified type as the source.
    /// </summary>
    /// <typeparam name="TFrom">The type of the source object.</typeparam>
    internal sealed class FromMapping<TFrom> : IFromMapping<TFrom>
    {
        private readonly TFrom source;

        /// <summary>
        /// Initializes a new instance of a FromMapping.
        /// </summary>
        /// <param name="source">The source object to map from.</param>
        public FromMapping(TFrom source)
        {
            this.source = source;
        }

        /// <summary>
        /// Creates a new destination object and uses the source and configured mapper to populate it.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <returns>The mapped destination object.</returns>
        public TTo To<TTo>()
        {
            IMapperImplementation<TFrom, TTo> mapper = Mapper<TFrom, TTo>.Get(String.Empty);
            return mapper.Map(source);
        }

        /// <summary>
        /// Creates a new destination object and uses the source and configured mapper to populate it.
        /// The mapping configuration associated with the given identifier will be used.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <param name="identifier">The identifier of the mapping configuration to use.</param>
        /// <returns>The mapped destination object.</returns>
        public TTo To<TTo>(string identifier)
        {
            IMapperImplementation<TFrom, TTo> mapper = Mapper<TFrom, TTo>.Get(identifier);
            return mapper.Map(source);
        }

        /// <summary>
        /// Uses the source and configured mapper to populate the given destination object.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <param name="destination">The destination object to store the results of the mapping.</param>
        /// <returns>The mapped destination object.</returns>
        public TTo To<TTo>(TTo destination)
        {
            IMapperImplementation<TFrom, TTo> mapper = Mapper<TFrom, TTo>.Get(String.Empty);
            return mapper.Map(source, destination);
        }

        /// <summary>
        /// Uses the source and configured mapper to populate the given destination object.
        /// The mappign configuration associated with the given identifier will be used.
        /// </summary>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <param name="destination">The destination object to store the result of the mapping.</param>
        /// <param name="identifier">The identifier of the mapping configuration to use.</param>
        /// <returns>The mapped destination object.</returns>
        public TTo To<TTo>(TTo destination, string identifier)
        {
            IMapperImplementation<TFrom, TTo> mapper = Mapper<TFrom, TTo>.Get(identifier);
            return mapper.Map(source, destination);
        }
    }
}
