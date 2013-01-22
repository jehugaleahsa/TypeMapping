using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TypeMapping
{
    /// <summary>
    /// Builds an object by mapping values from another object.
    /// </summary>
    /// <typeparam name="TFrom">The type of the source object.</typeparam>
    /// <typeparam name="TTo">The type of the destination object.</typeparam>
    public interface IMapperDefinition<TFrom, TTo>
    {
        /// <summary>
        /// Assigns the given value to the property returned by the given selector function.
        /// </summary>
        /// <typeparam name="TProp">The type of the property being set.</typeparam>
        /// <param name="toPropertySelector">An expression that returns the property to set.</param>
        /// <param name="value">The constant value to assign the property to.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> Assign<TProp>(Expression<Func<TTo, TProp>> toPropertySelector, TProp value);

        /// <summary>
        /// Specifies a method that constructs the new destination object.
        /// </summary>
        /// <param name="constructor">A function that creates a new destination object.</param>
        /// <returns>The current mapper.</returns>
        /// <remarks>This method is ignored if the destination object is provided.</remarks>
        IMapperDefinition<TFrom, TTo> Construct(Func<TFrom, TTo> constructor);

        /// <summary>
        /// Retrieves a value from the source object and passes it to the given mapping function.
        /// </summary>
        /// <typeparam name="TProp">The type of the value being mapped.</typeparam>
        /// <param name="fromValueSelector">A function that retrieves the value to map from the source object.</param>
        /// <param name="setter">A function that applies the extracted value to the destination object.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> Map<TProp>(Func<TFrom, TProp> fromValueSelector, Action<TTo, TProp> setter);

        /// <summary>
        /// Maps a value from the source object to a property in the destination object.
        /// </summary>
        /// <typeparam name="TProp">The type of the value being mapped.</typeparam>
        /// <param name="fromValueSelector">A function that retrieves the value to map from the source object.</param>
        /// <param name="toPropertySelector">A function that selects the property to assign the mapped value to.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> Map<TProp>(Func<TFrom, TProp> fromValueSelector, Expression<Func<TTo, TProp>> toPropertySelector);

        /// <summary>
        /// Retrieves a value from the source object and passes it to the given mapping function. An zero-based index
        /// representing how many times the mapper has been called will be provided.
        /// </summary>
        /// <typeparam name="TProp">The type of the value being mapped.</typeparam>
        /// <param name="fromValueSelector">A function that retrieves the value to map from the source object.</param>
        /// <param name="setter">A function that applies the extracted value to the destination object.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> Map<TProp>(Func<TFrom, int, TProp> fromValueSelector, Action<TTo, int, TProp> setter);

        /// <summary>
        /// Retrieves a collection from the source object and performs the mapping once for each item in the collection.
        /// </summary>
        /// <typeparam name="TItems">The type of the items in the collection.</typeparam>
        /// <param name="collectionSelector">A function that returns a collection within the source object.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> ForEach<TItems>(Func<TFrom, IEnumerable<TItems>> collectionSelector);

        /// <summary>
        /// Performs the mapping operations while the given predicate returns true.
        /// </summary>
        /// <param name="predicate">A function that determines whether to continue mapping values.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> While(Func<TFrom, bool> predicate);

        /// <summary>
        /// Performs the mapping operations while the given predicate returns true.
        /// </summary>
        /// <param name="predicate">A function that determines whether to continue mapping values.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> While(Func<TFrom, TTo, bool> predicate);

        /// <summary>
        /// Performs the mapping operations while the given predicate returns true.
        /// </summary>
        /// <param name="predicate">A function that determines whether to continue mapping values.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> While(Func<TTo, bool> predicate);
    }
}
