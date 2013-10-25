using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TypeMapping.Tests
{
    /// <summary>
    /// Tests different mapping configurations.
    /// </summary>
    [TestClass]
    public class MapperTester
    {
        /// <summary>
        /// Map from empty object to empty object.
        /// </summary>
        [TestMethod]
        public void ShouldCreateDestinationWhenNotProvided()
        {
            DefineMap.From<EmptyFromClass>().To<EmptyToClass>();

            EmptyFromClass source = new EmptyFromClass();
            EmptyToClass destination = Map.From<EmptyFromClass>(source).To<EmptyToClass>();
            Assert.IsNotNull(destination, "A destination was not created.");
        }

        /// <summary>
        /// If we pass the object to be mapped to, the constructor
        /// should not be called.
        /// </summary>
        [TestMethod]
        public void ShouldNotCallConstructorWhenDestinationPassed()
        {
            DefineMap.From<EmptyFromClass>().To<EmptyToClass>()
                     .Construct(from => { Assert.Fail("The constructor should not have been called."); return null; });

            EmptyFromClass source = new EmptyFromClass();
            EmptyToClass destination = new EmptyToClass();
            EmptyToClass result = Map.From<EmptyFromClass>(source).To<EmptyToClass>(destination);

            Assert.AreSame(destination, result, "The given destination was not returned.");
        }

        public class EmptyFromClass
        {
        }

        public class EmptyToClass
        {
        }

        /// <summary>
        /// We should be able to define a conversion from one type to the other.
        /// </summary>
        [TestMethod]
        public void ShouldBeAbleToConvertStringToInt()
        {
            DefineMap.From<string>().To<int>()
                .Construct(s => Int32.Parse(s));
            int result = Map.From<string>("123").To<int>();
            Assert.AreEqual(123, result, "The string was not parsed correctly.");
        }

        /// <summary>
        /// One of the primary uses for this library is to map properties from one object
        /// to another.
        /// </summary>
        [TestMethod]
        public void ShouldMapProperties()
        {
            DefineMap.From<FlatFrom>().To<FlatTo>()
                .Map(from => from.Value, to => to.Value);

            FlatFrom source = new FlatFrom() { Value = 123 };
            FlatTo destination = Map.From<FlatFrom>(source).To<FlatTo>();
            Assert.AreEqual(source.Value, destination.Value, "The value was not mapped.");
        }

        /// <summary>
        /// Let's make sure we can give a mapping a specific name.
        /// </summary>
        [TestMethod]
        public void ShouldMapPropertiesUsingNamedMapping()
        {
            const string identifier = "Multiply Ints";
            DefineMap.From<FlatFrom>().To<FlatTo>(identifier)
                .Map(from => from.Value, (to, value) => to.Value = value * 2);

            FlatFrom source = new FlatFrom() { Value = 123 };
            FlatTo destination = Map.From<FlatFrom>(source).To<FlatTo>(identifier);
            Assert.AreEqual(source.Value * 2, destination.Value, "The value was not mapped.");
        }

        public class FlatFrom
        {
            public int Value { get; set; }
        }

        public class FlatTo
        {
            public int Value { get; set; }
        }
    }
}
