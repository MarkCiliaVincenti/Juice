﻿using FluentAssertions;
using Juice.Modular;
using Xunit;

namespace Juice.Core.Tests
{
    public class FeatureTest
    {
        [Fact]
        public void FeatureAttr_should_be_received()
        {
            var type = typeof(AFeature);
            var feature = type.GetFeature();
            feature.Should().NotBeNull();
            feature!.Name.Should().Be("A");
        }
    }

    internal class FeatureAttr : Feature { }

    [FeatureAttr(Name = "A")]
    internal class AFeature { }
}
