using Xunit;
using LM = ServiceHub.Person.Library.Models;

namespace ServiceHub.Person.Testing.Library
{
    public class TestSuite
    {
        [Fact]
        public void PersonTest()
        {
            var expected = typeof(LM.Person);

            var actual = new LM.Person();
      
            Assert.True(expected == actual.GetType());
        }
    
        [Fact]
        public void AddressTest()
        {
            var expected = typeof(LM.Address);

            var actual = new LM.Address();

            Assert.True(expected == actual.GetType());
        }

        [Fact]
        public void SettingsTest()
        {
            var expected = typeof(LM.Settings);

            var actual = new LM.Settings();
            
            Assert.True(expected == actual.GetType());
        }
    }
}
