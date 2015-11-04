using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace TypeBuilder.Tests
{
    [TestClass]
    public class BuilderTests
    {
        private Converter _converter = new Converter();

        [TestMethod]
        public void BuildType_Success()
        {
            // Arrange
            var expectedPerson = PersonMother.GetPerson();
            var serialized = _converter.Serialize(expectedPerson);

            // Act
            var actualPerson = _converter.Deserialize<Person>(serialized);

            // Assert
            Assert.IsNotNull(actualPerson);
            Assert.AreEqual(expectedPerson.FirstName, actualPerson.FirstName);
            Assert.AreEqual(expectedPerson.LastName, actualPerson.LastName);
            Assert.AreEqual(expectedPerson.Age, actualPerson.Age);
            
            AssertAddress(expectedPerson.HomeAddress, actualPerson.HomeAddress);
        }

        private void AssertAddress(Address expectedAddress, Address actualAddress)
        {
            Assert.IsNotNull(actualAddress);
            Assert.AreEqual(expectedAddress.Street1, actualAddress.Street1);
            Assert.AreEqual(expectedAddress.Street2, actualAddress.Street2);
            Assert.AreEqual(expectedAddress.City, actualAddress.City);
            Assert.AreEqual(expectedAddress.State, actualAddress.State);
            Assert.AreEqual(expectedAddress.Zip, actualAddress.Zip);
        }
    }

    public static class PersonMother
    {
        public static Person GetPerson()
        {
            return new Person
            {
                FirstName = "Landon",
                LastName = "Poch",
                Age = 32,
                NetWorth = 450000.12M, // TODO: Test other types
                Male = true,
                HomeAddress = new Address
                {
                    Street1 = "678 Appletree Dr.",
                    City = "Sandy",
                    State = "UT",
                    Zip = "84070"
                },
                Quotes = new List<string>
                {
                    "This",
                    "is",
                    "a",
                    "test"
                }
            };
        }
    }

    public class Person
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Age { get; set; }

        public List<string> Quotes { get; set; }

        public Address HomeAddress { get; set; }

        public decimal NetWorth { get; set; }

        public bool Male { get; set; }
    }

    public class Address
    {
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
    }
}
