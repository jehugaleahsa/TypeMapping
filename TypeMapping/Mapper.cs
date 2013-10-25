using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using TypeMapping.Properties;

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

        private Action<TFrom> beforeMap;
        private Action<TFrom> afterMap;
        private Func<TFrom, TTo> constructor;
        private readonly List<Action<TTo>> assigners;
        private readonly List<Action<TFrom, TTo>> mappers;

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
            beforeMap = from => { };
            afterMap = from => { };
            constructor = createInstance();
            assigners = new List<Action<TTo>>();
            mappers = new List<Action<TFrom, TTo>>();
        }

        private static Func<TFrom, TTo> creator;

        private static Func<TFrom, TTo> createInstance()
        {
            if (creator == null)
            {
                ConstructorInfo ctorInfo = typeof(TTo).GetConstructor(Type.EmptyTypes);
                if (ctorInfo == null)
                {
                    creator = from => { throw new InvalidOperationException(Resources.ConstructorNotDefined); };
                }
                else
                {
                    DynamicMethod method = new DynamicMethod("create" + typeof(TTo).Name, typeof(TTo), new Type[] { typeof(TFrom) }, typeof(TTo));
                    ILGenerator generator = method.GetILGenerator();
                    generator.Emit(OpCodes.Newobj, ctorInfo);
                    generator.Emit(OpCodes.Ret);
                    creator = (Func<TFrom, TTo>)method.CreateDelegate(typeof(Func<TFrom, TTo>));
                }
            }
            return creator;
        }

        #region BeforeMap

        /// <summary>
        /// Performs the given action on the source object before mapping.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> BeforeMap(Action<TFrom> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            beforeMap = action;
            return this;
        }

        #endregion

        #region AfterMap

        /// <summary>
        /// Performs the given action after mapping.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> AfterMap(Action<TFrom> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            afterMap = action;
            return this;
        }

        #endregion

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

        #region Map

        /// <summary>
        /// Maps the given value to the property returned by the given selector function.
        /// </summary>
        /// <typeparam name="TProp">The type of the property being set.</typeparam>
        /// <param name="toPropertySelector">An expression that returns the property to set.</param>
        /// <param name="value">The constant value to assign the property to.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> Map<TProp>(Expression<Func<TTo, TProp>> toPropertySelector, TProp value)
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

        /// <summary>
        /// Maps the given value to the property returned by the given selector function.
        /// </summary>
        /// <typeparam name="TProp">The type of the property being set.</typeparam>
        /// <param name="toPropertySelector">An expression that returns the property to set.</param>
        /// <param name="generator">A function used to generate the value to assign to the property.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> Map<TProp>(Expression<Func<TTo, TProp>> toPropertySelector, Func<TProp> generator)
        {
            if (toPropertySelector == null)
            {
                throw new ArgumentNullException("toPropertySelector");
            }
            if (generator == null)
            {
                throw new ArgumentNullException("valueSelector");
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
                Action<TTo> assigner = (TTo to) => propertyInfo.SetValue(to, generator(), null);
                assigners.Add(assigner);
            }
            else if (memberExpression.Member.MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = (FieldInfo)memberExpression.Member;
                Action<TTo> assigner = (TTo to) => fieldInfo.SetValue(to, generator());
                assigners.Add(assigner);
            }
            else
            {
                throw new ArgumentException(Resources.InvalidPropertySelector, "toPropertySelector");
            }
            return this;
        }

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
                Action<TFrom, TTo> mapper = (from, to) => propertyInfo.SetValue(to, fromValueSelector(from), null);
                mappers.Add(mapper);
            }
            else if (memberExpression.Member.MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = (FieldInfo)memberExpression.Member;
                Action<TFrom, TTo> mapper = (from, to) => fieldInfo.SetValue(to, fromValueSelector(from));
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
            Action<TFrom, TTo> mapping = (from, to) => setter(to, fromValueSelector(from));
            mappers.Add(mapping);
            return this;
        }

        #endregion

        #region MapMany

        /// <summary>
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">A function that is used to determine when to stop mapping.</param>
        /// <param name="setter">A function that maps the source object to the destination object.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> MapMany(Func<TFrom, bool> predicate, Action<TTo, TFrom> setter)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            Action<TFrom, TTo> mapper = (from, to) =>
            {
                while (predicate(from))
                {
                    setter(to, from);
                }
            };
            mappers.Add(mapper);
            return this;
        }

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
        public IMapperDefinition<TFrom, TTo> MapMany(Func<TFrom, int, bool> predicate, Action<TTo, int, TFrom> setter)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            Action<TFrom, TTo> mapper = (from, to) =>
            {
                int count = 0;
                while (predicate(from, count))
                {
                    setter(to, count, from);
                    ++count;
                }
            };
            mappers.Add(mapper);
            return this;
        }

        /// <summary>
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">A function that is used to determine when to stop mapping.</param>
        /// <param name="setter">A function that maps the source object to the destination object.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> MapMany(Func<TTo, bool> predicate, Action<TTo, TFrom> setter)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            Action<TFrom, TTo> mapper = (from, to) =>
            {
                while (predicate(to))
                {
                    setter(to, from);
                }
            };
            mappers.Add(mapper);
            return this;
        }

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
        public IMapperDefinition<TFrom, TTo> MapMany(Func<TTo, int, bool> predicate, Action<TTo, int, TFrom> setter)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            Action<TFrom, TTo> mapper = (from, to) =>
            {
                int count = 0;
                while (predicate(to, count))
                {
                    setter(to, count, from);
                    ++count;
                }
            };
            mappers.Add(mapper);
            return this;
        }

        /// <summary>
        /// While the given predicate returns true, the source is used to map a value into the destination.
        /// </summary>
        /// <param name="predicate">A function that is used to determine when to stop mapping from the source object.</param>
        /// <param name="setter">A function that maps the source object to the destination object.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> MapMany(Func<TFrom, TTo, bool> predicate, Action<TTo, TFrom> setter)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            Action<TFrom, TTo> mapper = (from, to) =>
            {
                while (predicate(from, to))
                {
                    setter(to, from);
                }
            };
            mappers.Add(mapper);
            return this;
        }

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
        public IMapperDefinition<TFrom, TTo> MapMany(Func<TFrom, TTo, int, bool> predicate, Action<TTo, int, TFrom> setter)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            Action<TFrom, TTo> mapper = (from, to) =>
            {
                int count = 0;
                while (predicate(from, to, count))
                {
                    setter(to, count, from);
                    ++count;
                }
            };
            mappers.Add(mapper);
            return this;
        }

        /// <summary>
        /// The given setter function will be called for each item in the collection returned by the given selector.
        /// </summary>
        /// <typeparam name="TValue">The type of the value in the source collection.</typeparam>
        /// <param name="selector">A function that returns a collection within the source object.</param>
        /// <param name="setter">A function that maps the collection value to the destination object.</param>
        /// <returns>The current mapper.</returns>
        public IMapperDefinition<TFrom, TTo> MapMany<TValue>(Func<TFrom, IEnumerable<TValue>> selector, Action<TTo, TValue> setter)
        {
            if (selector == null)
            {
                throw new ArgumentNullException("selector");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            Action<TFrom, TTo> mapper = (from, to) =>
            {
                IEnumerable<TValue> collection = selector(from);
                foreach (TValue value in collection)
                {
                    setter(to, value);
                }
            };
            mappers.Add(mapper);
            return this;
        }

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
        public IMapperDefinition<TFrom, TTo> MapMany<TValue>(Func<TFrom, IEnumerable<TValue>> selector, Action<TTo, int, TValue> setter)
        {
            if (selector == null)
            {
                throw new ArgumentNullException("selector");
            }
            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            Action<TFrom, TTo> mapper = (from, to) =>
            {
                int count = 0;
                IEnumerable<TValue> collection = selector(from);
                foreach (TValue value in collection)
                {
                    setter(to, count, value);
                    ++count;
                }
            };
            mappers.Add(mapper);
            return this;
        }

        #endregion

        #region Bridge

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
        public IMapperDefinition<TFrom, TOther> Bridge<TOther>(IMapperDefinition<TTo, TOther> definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            Mapper<TTo, TOther> otherMapper = (Mapper<TTo, TOther>)definition;
            return DefineMap.From<TFrom>().To<TOther>().Map(from => Map(from), (other, to) => otherMapper.Map(to, other));
        }

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
        public IMapperDefinition<TFrom, TOther> Bridge<TOther>(IMapperDefinition<TTo, TOther> definition, string identifier)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            Mapper<TTo, TOther> otherMapper = (Mapper<TTo, TOther>)definition;
            return DefineMap.From<TFrom>().To<TOther>(identifier).Map(from => Map(from), (other, to) => otherMapper.Map(to, other));
        }

        #endregion

        /// <summary>
        /// Creates a new destination object and maps the source object to it using the specified configuration.
        /// </summary>
        /// <param name="from">The source object.</param>
        /// <returns>The destination object.</returns>
        public TTo Map(TFrom from)
        {
            try
            {
                beforeMap(from);
                TTo to = constructor(from);
                return map(from, to);
            }
            finally
            {
                afterMap(from);
            }
        }

        /// <summary>
        /// Maps the source object to the given destination object using the specified configuration.
        /// </summary>
        /// <param name="from">The source object.</param>
        /// <param name="to">The destination object.</param>
        /// <returns>The destination object.</returns>
        public TTo Map(TFrom from, TTo to)
        {
            try
            {
                beforeMap(from);
                return map(from, to);
            }
            finally
            {
                afterMap(from);
            }
        }

        private TTo map(TFrom from, TTo to)
        {
            foreach (Action<TTo> assigner in assigners)
            {
                assigner(to);
            }
            foreach (Action<TFrom, TTo> mapper in mappers)
            {
                mapper(from, to);
            }
            return to;
        }
    }
}
