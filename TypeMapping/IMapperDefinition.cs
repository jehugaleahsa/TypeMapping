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
        /// Performs the given action before mapping.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> BeforeMap(Action<TFrom> action);

        /// <summary>
        /// Performs the given action after mapping.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> AfterMap(Action<TFrom> action);

        /// <summary>
        /// Assigns the given value to the property returned by the given selector function.
        /// </summary>
        /// <typeparam name="TProp">The type of the property being set.</typeparam>
        /// <param name="toPropertySelector">An expression that returns the property to set.</param>
        /// <param name="value">The constant value to assign the property to.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> Map<TProp>(Expression<Func<TTo, TProp>> toPropertySelector, TProp value);

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
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">A function that is used to determine when to stop mapping.</param>
        /// <param name="setter">A function that maps the source object to the destination object.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> MapMany(Func<TFrom, bool> predicate, Action<TTo, TFrom> setter);

        /// <summary>
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">
        /// A function that is used to determine when to stop mapping. An zero-based count for how many times 
        /// the function is called will be passed.
        /// </param>
        /// <param name="setter">
        /// A function that maps the source object to the destination object. A zero-based count for how many times
        /// the function is called will be passed.
        /// </param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> MapMany(Func<TFrom, int, bool> predicate, Action<TTo, int, TFrom> setter);

        /// <summary>
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">A function that is used to determine when to stop mapping.</param>
        /// <param name="setter">A function that maps the source object to the destination object.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> MapMany(Func<TTo, bool> predicate, Action<TTo, TFrom> setter);

        /// <summary>
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">
        /// A function that is used to determine when to stop mapping. An zero-based count for how many times 
        /// the function is called will be passed.
        /// </param>
        /// <param name="setter">
        /// A function that maps the source object to the destination object. A zero-based count for how many times
        /// the function is called will be passed.
        /// </param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> MapMany(Func<TTo, int, bool> predicate, Action<TTo, int, TFrom> setter);

        /// <summary>
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">A function that is used to determine when to stop mapping from the source object.</param>
        /// <param name="setter">A function that maps the source object to the destination object.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> MapMany(Func<TFrom, TTo, bool> predicate, Action<TTo, TFrom> setter);

        /// <summary>
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">
        /// A function that is used to determine when to stop mapping frmo the source object. An zero-based count
        /// for how many times the function is called will be passed.
        /// </param>
        /// <param name="setter">
        /// A function that maps the source object to the destination object. A zero-based count for how many times
        /// the function is called will be passed.
        /// </param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> MapMany(Func<TFrom, TTo, int, bool> predicate, Action<TTo, int, TFrom> setter);

        /// <summary>
        /// The given setter function will be called for each item in the collection returned by the given selector.
        /// </summary>
        /// <typeparam name="TValue">The type of the value in the source collection.</typeparam>
        /// <param name="selector">A function that returns a collection within the source object.</param>
        /// <param name="setter">A function that maps the collection value to the destination object.</param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> MapMany<TValue>(Func<TFrom, IEnumerable<TValue>> selector, Action<TTo, TValue> setter);

        /// <summary>
        /// The given setter function will be called for each item in the collection returned by the given selector.
        /// </summary>
        /// <typeparam name="TValue">The type of the value in the source collection.</typeparam>
        /// <param name="selector">A function that returns a collection within the source object.</param>
        /// <param name="setter">
        /// A function that maps the collection value to the destination object. A zero-based count for how many times
        /// the function is called will be passed.
        /// </param>
        /// <returns>The current mapper.</returns>
        IMapperDefinition<TFrom, TTo> MapMany<TValue>(Func<TFrom, IEnumerable<TValue>> selector, Action<TTo, int, TValue> setter);

        /// <summary>
        /// Associates the source object to the other object using the current mapping's destination object as the source to
        /// the other mapping.
        /// </summary>
        /// <typeparam name="TOther">The type of the object to indirectly associate the source object to.</typeparam>
        /// <param name="definition">The mapping configuration associating this mapping's destination object to the other object.</param>
        /// <returns>A mapping configuration between the source type and the other type.</returns>
        /// <remarks>
        /// Associating TFrom to TOther is only possible if TTo is default constructible or Construct has been called.
        /// </remarks>
        IMapperDefinition<TFrom, TOther> Bridge<TOther>(IMapperDefinition<TTo, TOther> definition);

        /// <summary>
        /// Associates the source object to the other object using the current mapping's destination object as the source to
        /// the other mapping.
        /// </summary>
        /// <typeparam name="TOther">The type of the object to indirectly associate the source object to.</typeparam>
        /// <param name="definition">The mapping configuration associating this mapping's destination object to the other object.</param>
        /// <param name="identifier">The identifier to use to distinguish the new mapping definition from others with the same type parameters.</param>
        /// <returns>A mapping configuration between the source type and the other type.</returns>
        /// <remarks>
        /// Associating TFrom to TOther is only possible if TTo is default constructible or Construct has been called.
        /// </remarks>
        IMapperDefinition<TFrom, TOther> Bridge<TOther>(IMapperDefinition<TTo, TOther> definition, string identifier);
    }
}
