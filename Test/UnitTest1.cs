using Xunit;
using FluentAssertions;
using CustomCollections.BinaryList;

public class BinaryListTests
{
    [Fact]
    public void Add_ShouldKeepElementsSorted()
    {
        // Arrange
        var list = new BinaryList<int>();
        var inputs = new[] { 10, 5, 20, 1, 15 };
        var expected = new[] { 1, 5, 10, 15, 20 };

        // Act
        foreach (var i in inputs) list.Add(i);

        // Assert
        list.Should().Equal(expected);
    }

    [Theory]
    [InlineData(FindIndexOptions.First, 0)]
    [InlineData(FindIndexOptions.Last, 2)]
    public void FindIndex_ShouldHandleDuplicatesCorrectly(FindIndexOptions option, int expectedIndex)
    {
        // Arrange
        var list = new BinaryList<int> { 10, 10, 10, 20, 30 };

        // Act
        int result = list.FindIndex(10, option);

        // Assert
        result.Should().Be(expectedIndex);
    }
}