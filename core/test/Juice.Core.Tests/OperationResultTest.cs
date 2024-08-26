using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Juice.Core.Tests
{
    public class OperationResultTest
    {
        private ITestOutputHelper _output;

        public OperationResultTest(ITestOutputHelper testOutput)
        {
            _output = testOutput;
        }

        [Fact]
        public void OR_should_be_success()
        {
            var rs = OR.Success;
            rs.Succeeded.Should().BeTrue();
            _output.WriteLine(rs.ToString());
        }

        [Fact]
        public void OR_should_be_success_with_data()
        {
            var rs = OR.Result("data");
            rs.Succeeded.Should().BeTrue();
            rs.Data.Should().Be("data");
            _output.WriteLine(rs.ToString());
        }

        [Fact]
        public void OR_should_be_success_without_data()
        {
            var rs = OR.Empty<string>();
            rs.Succeeded.Should().BeTrue();
            rs.Data.Should().BeNull();
            _output.WriteLine(rs.ToString());
        }

        [Fact]
        public void OR_should_be_failed()
        {
            try {
                var rs = Action();
                rs.ThrowIfNotSucceeded();
            }catch(Exception ex)
            {
                ex.Message.Should().Be("Inner message");
                _output.WriteLine(ex.StackTrace);
                ex.StackTrace.Should().Contain("OperationResultTest.Action()");
            }
           
        }

        private IOperationResult Action() {
            try
            {
                throw new Exception("Inner message");
            }
            catch (Exception ex)
            {
               return OR.Failed(ex);
            }
        }

    }

    internal class OR: OperationResult
    {
       
    }
}
