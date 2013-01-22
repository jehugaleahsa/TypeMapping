using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TypeMapping.Properties;
using System.Reflection.Emit;

namespace TypeMapping
{
    /// <summary>
    /// Builds an object by mapping values from another object.
    /// </summary>
    /// <typeparam name="TFrom">The type of the source object.</typeparam>
    /// <typeparam name="TTo">The type of the destination object.</typeparam>
    internal sealed class Mapper<TFrom, TTo> : IMapperDefinition<TFrom, TTo>, IMapperImplementation<TFrom, TTo>
    {
        private readonly static Dictionary<string, Mapper<TFrom, TTo>> mapperLookup = new Dictionary<string, Mapper<TFrom, TTo>>();

        private Func<TFrom, TTo> constructor;
        private readonly List<Action<TTo>> assigners;
        private Func<TFrom, Func<TFrom, TTo, bool>> predicateFactory;
        private readonly List<Action<TFrom, TTo, int>> mappers;

        /// <summary>
        /// Defines the mapper with the given identifier, creating it if it doesn't exists.
        /// </summary>
        /// <param name="identifier">The identifier specifying which mapper to get.</param>
        /// <returns>The mapper.</returns>
        public static IMapperDefinition<TFrom, TTo> Define(string identifier)
        {
            Mapper<TFrom, TTo> mapper;
            if (!mapperLookup.TryGetValue(identifier, out mapper))
            {
                mapper = new Mapper<TFrom, TTo>(identifier);
                mapperLookup.Add(identifier, mapper);
            }
            return mapper;
        }

        /// <summary>
        /// Gets the mapper with the given identifier.
        /// </summary>
        /// <param name="identifier">The identifier specifying which mapper to get.</param>
        /// <returns>The mapper.</returns>
        public static IMapperImplementation<TFrom, TTo> Get(string identifier)
        {
            if (!mapperLookup.ContainsKey(identifier))
            {
                throw new InvalidOperationException(Resources.MappingNotConfigured);
            }
            Mapper<TFrom, TTo> mapper = mapperLookup[identifier];
            return mapper;
        }

        /// <summary>
        /// Initializes a new instance of a Mapper with the given identifier.
        /// </summary>
        /// <param name="identifier">The identifier to uniquely identify the from -> to mapping.</param>
        private Mapper(string identifier)
        {
            constructor = from => Activator.CreateInstance<TTo>();
            assigners = new List<Action<TTo>>();
            predicateFactory = (TFrom from) => getOneTimePredicate();
            mappers = new List<Action<TFrom, TTo, int>>();
        }

        private Func<TFrom, TTo, bool> getOneTimePredicate()
        {
            using (IEnumerator<int> enumerator = Enumerable.Repeat(0, 1).GetEnumerator())
            {
                return (TFrom from, TTo to) => enumerator.MoveNext();
            }
        }

        #region Construct

        /// <summary>
        /// Specifies a method that constructs the new destination object.
        /// </summary>
        /// <param name="constructor">A function that creates a new destination object.</param>
        /// <returns>The current mapper.</returns>
        /// <remarks>This method is ignored if the destination object is provided.</remarks>
        public IMapperDefinition<TFrom, TTo> Construct(Func<TFrom, TTo> constructor)
        {
            if (constructor == null)
            {
                throw new ArgumentNullException("constructor");
            }
            this.constructor = constructor;
            return this;
        }

        #endregion

        #region Assign

        /// <summary>
        /// Assigns the given value to the property returned by the given selector function.
        /// </summary>
        /// <typeparam name="TProp">The type of the property being set.</typeparam>
        /// <param name="toPropertySelector">An expression that returns the property to set.</param>
        /// <param name="value">The constant value to assign the property to.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> Assign<TProp>(Expression<Func<TTo, TProp>> toPropertySelector, TProp value)
        {
            if (toPropertySelector == null)
            {
                throw new ArgumentNullException("toPropertySelector");
            }
            LambdaExpression lambdaExpression = toPropertySelector as LambdaExpression;
            if (lambdaExpression == null)
            {
                throw new ArgumentException(Resources.InvalidPropertySelector, "toPropertySelector");
            }
            MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
            if (memberExpression == null)
            {
                throw new ArgumentException(Resources.InvalidPropertySelector, "toPropertySelector");
            }
            if (memberExpression.Member.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberExpression.Member;
                Action<TTo> assigner = (TTo to) => propertyInfo.SetValue(to, value, null);
                assigners.Add(assigner);
            }
            else if (memberExpression.Member.MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = (FieldInfo)memberExpression.Member;
                Action<TTo> assigner = (TTo to) => fieldInfo.SetValue(to, value);
                assigners.Add(assigner);
            }
            else
            {
                throw new ArgumentException(Resources.InvalidPropertySelector, "toPropertySelector");
            }
            return this;
        }

        #endregion

        #region ForEach

        /// <summary>
        /// Retrieves a collection from the source object and performs the mapping once for each item in the collection.
        /// </summary>
        /// <typeparam name="TItems">The type of the items in the collection.</typeparam>
        /// <param name="collectionSelector">A function that returns a collection within the source object.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> ForEach<TItems>(Func<TFrom, IEnumerable<TItems>> collectionSelector)
        {
            if (collectionSelector == null)
            {
                throw new ArgumentNullException("collectionSelector");
            }
            predicateFactory = (TFrom source) =>
            {
                IEnumerable<TItems> collection = collectionSelector(source);
                using (IEnumerator<TItems> enumerator = collection.GetEnumerator())
                {
                    return (TFrom from, TTo to) => enumerator.MoveNext();
                }
            };
            return this;
        }

        #endregion

        #region While

        /// <summary>
        /// Performs the mapping operations while the given predicate returns true.
        /// </summary>
        /// <param name="predicate">A function that determines whether to continue mapping values.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> While(Func<TFrom, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            predicateFactory = (TFrom source) => (TFrom from, TTo to) => predicate(from);
            return this;
        }

        /// <summary>
        /// Performs the mapping operations while the given predicate returns true.
        /// </summary>
        /// <param name="predicate">A function that determines whether to continue mapping values.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> While(Func<TTo, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            predicateFactory = (TFrom source) => (TFrom from, TTo to) => predicate(to);
            return this;
        }

        /// <summary>
        /// Performs the mapping operations while the given predicate returns true.
        /// </summary>
        /// <param name="predicate">A function that determines whether to continue mapping values.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> While(Func<TFrom, TTo, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            predicateFactory = (TFrom source) => predicate;
            return this;
        }

        #endregion

        #region Map

        /// <summary>
        /// Maps a value from the source object to a property in the destination object.
        /// </summary>
        /// <typeparam name="TProp">The type of the value being mapped.</typeparam>
        /// <param name="fromValueSelector">A function that retrieves the value to map from the source object.</param>
        /// <param name="toPropertySelector">A function that selects the property to assign the mapped value to.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> Map<TProp>(Func<TFrom, TProp> fromValueSelector, Expression<Func<TTo, TProp>> toPropertySelector)
        {
            if (fromValueSelector == null)
            {
                throw new ArgumentNullException("fromValueSelector");
            }
            if (toPropertySelector == null)
            {
                throw new ArgumentNullException("toPropertySelector");
            }
            LambdaExpression lambdaExpression = toPropertySelector as LambdaExpression;
            if (lambdaExpression == null)
            {
                throw new ArgumentException(Resources.InvalidPropertySelector, "toPropertySelector");
            }
            MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
            if (memberExpression == null)
            {
                throw new ArgumentException(Resources.InvalidPropertySelector, "toPropertySelector");
            }
            if (memberExpression.Member.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberExpression.Member;
                Action<TFrom, TTo, int> mapper = (from, to, index) => propertyInfo.SetValue(to, fromValueSelector(from), null);
                mappers.Add(mapper);
            }
            else if (memberExpression.Member.MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = (FieldInfo)memberExpression.Member;
                Action<TFrom, TTo, int> mapper = (from, to, index) => fieldInfo.SetValue(to, fromValueSelector(from));
                mappers.Add(mapper);
            }
            else
            {
                throw new ArgumentException(Resources.InvalidPropertySelector, "toPropertySelector");
            }
            return this;
        }

        /// <summary>
        /// Retrieves a value from the source object and passes it to the given mapping function.
        /// </summary>
        /// <typeparam name="TProp">The type of the value being mapped.</typeparam>
        /// <param name="fromValueSelector">A function that retrieves the value to map from the source object.</param>
        /// <param name="setter">A function that applies the extracted value to the destination object.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> Map<TProp>(Func<TFrom, TProp> fromValueSelector, Action<TTo, TProp> setter)
        {
            if (fromValueSelector == null)
            {
                throw new ArgumentNullException("fromValueSelector");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("mapper");
            }
            Action<TFrom, TTo, int> mapping = (TFrom from, TTo to, int index) => setter(to, fromValueSelector(from));
            mappers.Add(mapping);
            return this;
        }

        /// <summary>
        /// Retrieves a value from the source object and passes it to the given mapping function. An zero-based index
        /// representing how many times the mapper has been called will be provided.
        /// </summary>
        /// <typeparam name="TProp">The type of the value being mapped.</typeparam>
        /// <param name="fromValueSelector">A function that retrieves the value to map from the source object.</param>
        /// <param name="setter">A function that applies the extracted value to the destination object.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> Map<TProp>(Func<TFrom, int, TProp> fromValueSelector, Action<TTo, int, TProp> setter)
        {
            if (fromValueSelector == null)
            {
                throw new ArgumentNullException("fromValueSelector");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("mapper");
            }
            Action<TFrom, TTo, int> mapping = (TFrom from, TTo to, int index) => setter(to, index, fromValueSelector(from, index));
            mappers.Add(mapping);
            return this;
        }

        #endregion

        /// <summary>
        /// Creates a new destination object and maps the source object to it using the specified configuration.
        /// </summary>
        /// <param name="from">The source object.</param>
        /// <returns>The destination object.</returns>
        public TTo Map(TFrom from)
        {
            TTo to = constructor(from);
            return map(from, to);
        }

        /// <summary>
        /// Maps the source object to the given destination object using the specified configuration.
        /// </summary>
        /// <param name="from">The source object.</param>
        /// <param name="to">The destination object.</param>
        /// <returns>The destination object.</returns>
        public TTo Map(TFrom from, TTo to)
        {
            return map(from, to);
        }

        private TTo map(TFrom from, TTo to)
        {
            foreach (Action<TTo> assigner in assigners)
            {
                assigner(to);
            }
            Func<TFrom, TTo, bool> predicate = predicateFactory(from);
            for (int index = 0; predicate(from, to); ++index)
            {
                foreach (Action<TFrom, TTo, int> mapper in mappers)
                {
                    mapper(from, to, index);
                }
            }
            return to;
        }
    }
}
