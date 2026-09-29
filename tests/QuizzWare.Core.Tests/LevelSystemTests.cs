using QuizzWare.Core.Leveling;

namespace QuizzWare.Core.Tests;

public class LevelSystemTests
{
    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(99, 1, 99)]
    [InlineData(100, 2, 0)]
    [InlineData(249, 2, 149)]
    [InlineData(250, 3, 0)]
    public void GetLevelInfo_ComputesLevelAndRemainder(int xp, int expectedLevel, int expectedInto)
    {
        var info = LevelSystem.GetLevelInfo(xp);

        Assert.Equal(expectedLevel, info.Level);
        Assert.Equal(expectedInto, info.XpIntoLevel);
    }

    [Fact]
    public void GetLevelInfo_CapsAtMaxLevel()
    {
        var info = LevelSystem.GetLevelInfo(LevelSystem.TotalXpForLevel(LevelSystem.MaxLevel) + 10_000);

        Assert.Equal(LevelSystem.MaxLevel, info.Level);
        Assert.True(info.IsMaxLevel);
        Assert.Equal(1, info.Progress);
    }

    [Fact]
    public void GetLevelInfo_NegativeXp_IsLevelOne()
    {
        Assert.Equal(1, LevelSystem.GetLevelInfo(-50).Level);
    }

    [Fact]
    public void XpRequirement_GrowsWithLevel()
    {
        for (var level = 1; level < LevelSystem.MaxLevel - 1; level++)
            Assert.True(LevelSystem.XpToReachNext(level + 1) > LevelSystem.XpToReachNext(level));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(7, 3)]
    [InlineData(10, 3)]
    public void MaxQuestionDifficulty_IncreasesWithLevel(int level, int expected)
    {
        Assert.Equal(expected, LevelSystem.MaxQuestionDifficulty(level));
    }
}
