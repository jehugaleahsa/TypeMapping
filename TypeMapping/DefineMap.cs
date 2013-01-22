using System;

namespace TypeMapping
{
    /// <summary>
    /// Defines a mapping between a source and destination type.
    /// </summary>
    public static class DefineMap
    {
        /// <summary>
        /// Specifies the type of the source object.
        /// </summary>
        /// <typeparam name="TFrom">The type of the source object.</typeparam>
        /// <returns>An object that allows the destination type to be specified.</returns>
        public static IFromDefinition<TFrom> From<TFrom>()
        {
            return new FromDefinition<TFrom>();
        }
    }
}
