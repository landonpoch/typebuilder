using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeBuilder.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run(new Converter());
        }

        private void Run(Converter converter)
        {
            var expectedPerson = PersonMother.GetPerson();
            var serialized = converter.Serialize(expectedPerson);
            var actualPerson = converter.Deserialize<Person>(serialized);
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
