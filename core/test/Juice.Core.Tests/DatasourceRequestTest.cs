using Juice.AspNetCore.Models;
using Xunit;

namespace Juice.Core.Tests
{
    public class DatasourceRequestTest
    {
        [Fact]
        public void QueryShould()
        {
            var request = new DatasourceRequest
            {
                Query = "  a b  c  ",
                Page = 2,
                PageSize = 100,
                Sorts = new[]
                {
                    new SortDescriptor
                    {
                        Property = "a",
                        Direction = SortDirection.Asc
                    },
                    new SortDescriptor
                    {
                        Property = "b",
                        Direction = SortDirection.Desc
                    }
                }
            };
            request.Standardizing();
            Assert.Equal("%a%b%c%", request.FilterText);
            Assert.Equal(2, request.Page);
            Assert.Equal(50, request.PageSize);
            Assert.Equal(50, request.SkipCount);
        }
    }
}
