using BottleGame.Application.Common;
using FluentAssertions;

namespace BottleGame.UnitTests.Common;

/// <summary>
/// Tests for PageRequest
/// </summary>
public class PageRequestTests
{
    /// <summary>
    /// Without page and pageSize no paging is asked for, so the whole list is returned
    /// </summary>
    [Fact]
    public void Create_NoParameters_ReturnsNull()
    {
        PageRequest.Create(null, null).Should().BeNull();
    }

    /// <summary>
    /// A missing page size gets the default one, a missing page is the first page
    /// </summary>
    [Fact]
    public void Create_OnlyOneParameter_UsesDefaults()
    {
        PageRequest.Create(3, null).Should().Be(new PageRequest(3, PageRequest.DefaultPageSize));
        PageRequest.Create(null, 50).Should().Be(new PageRequest(1, 50));
    }

    /// <summary>
    /// Values out of range are brought into range, so a client cannot ask for a huge page or a negative offset
    /// </summary>
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-5, -5, 1, 1)]
    [InlineData(2, 100000, 2, PageRequest.MaxPageSize)]
    public void Create_OutOfRange_IsClamped(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        PageRequest.Create(page, pageSize).Should().Be(new PageRequest(expectedPage, expectedPageSize));
    }

    /// <summary>
    /// Skip is the number of items before the page
    /// </summary>
    [Fact]
    public void Skip_IsItemsBeforeThePage()
    {
        new PageRequest(3, 20).Skip.Should().Be(40);
    }
}
