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

            var rs1 = OR.Succeeded("message");
            rs1.Succeeded.Should().BeTrue();
            rs1.Message.Should().Be("message");
            _output.WriteLine(rs1.ToString());
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
            var rs = OR.Succeeded<string>("message");
            rs.Succeeded.Should().BeTrue();
            rs.Data.Should().BeNull();
            rs.Message.Should().Be("message");
            _output.WriteLine(rs.ToString());
        }

        [Fact]
        public void OR_should_be_failed_with_data()
        {
            var rs = OR.Failed<string>(new Exception("Inner message"), "message", "string data");

            rs.Succeeded.Should().BeFalse();
            rs.Data.Should().Be("string data");
            rs.Message.Should().Be("message");
            rs.Exception.Should().NotBeNull();
            _output.WriteLine(rs.ToString());
        }

        [Fact]
        public void OR_should_be_throwed_with_full_stack_trace()
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

        [Fact]
        public void OR_should_be_changed_type()
        {
            var rs = OR.Failed(new Exception("Inner message")).Of<int>();
            rs.Succeeded.Should().BeFalse();
            rs.Exception.Should().NotBeNull();
            rs.Message.Should().Be("Inner message");
            rs.Data.Should().Be(0);
            _output.WriteLine(rs.ToString());
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
