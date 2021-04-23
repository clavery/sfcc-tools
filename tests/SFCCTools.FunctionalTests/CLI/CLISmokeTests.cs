using System;
using Xunit;

using SFCCTools.CLI;
using System.Text;
using System.IO;

namespace SFCCTools.FunctionalTests
{
    public class CLISmokeTests
    {
        [Fact]
        public void DummyTestAssertsTrueIfCompiles()
        {
            Assert.True(true);
        }
    }
}
