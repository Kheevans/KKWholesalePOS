
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests;

public class UserSessionTests
{
    [Fact]
    public void StartSession_ShouldSetCurrentUser()
    {
        // Arrange
        var session = new UserSessionService();

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true
        };

        // Act
        session.StartSession(user);

        // Assert
        Assert.Equal(user, session.CurrentUser);
    }

    [Fact]
    public void StartSession_ShouldSetAuthenticatedState()
    {
        // Arrange
        var session = new UserSessionService();

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true
        };

        // Act
        session.StartSession(user);

        // Assert
        Assert.True(session.IsAuthenticated);
    }

    [Fact]
    public void StartSession_ShouldRejectInactiveUser()
    {
        // Arrange
        var session = new UserSessionService();

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = false
        };

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() =>
            session.StartSession(user));
    }

    [Fact]
    public void StartSession_ShouldRejectNullUser()
    {
        // Arrange
        var session = new UserSessionService();

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() =>
            session.StartSession(null!));
    }

    [Fact]
    public void EndSession_ShouldClearCurrentUser()
    {
        // Arrange
        var session = new UserSessionService();

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true
        };

        session.StartSession(user);

        // Act
        session.EndSession();

        // Assert
        Assert.Null(session.CurrentUser);
        Assert.False(session.IsAuthenticated);
    }
}
