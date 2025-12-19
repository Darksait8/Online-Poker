using NUnit.Framework;
using UnityEngine;
using System;

namespace Tests
{
    /// <summary>
    /// Тесты для FriendRequestData
    /// </summary>
    public class FriendRequestDataTests
    {
        [Test]
        public void FriendRequestData_CanBeCreated()
        {
            // Arrange & Act
            FriendRequestData request = new FriendRequestData
            {
                from = "User1",
                to = "User2",
                createdAtTicks = DateTime.UtcNow.Ticks
            };

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("User1", request.from);
            Assert.AreEqual("User2", request.to);
            Assert.Greater(request.createdAtTicks, 0);
        }

        [Test]
        public void FriendRequestData_CreatedAtTicks_IsSet()
        {
            // Arrange
            long expectedTicks = DateTime.UtcNow.Ticks;

            // Act
            FriendRequestData request = new FriendRequestData
            {
                from = "User1",
                to = "User2",
                createdAtTicks = expectedTicks
            };

            // Assert
            Assert.AreEqual(expectedTicks, request.createdAtTicks);
        }

        [Test]
        public void FriendRequestData_CanBeCreatedWithEmptyStrings()
        {
            // Arrange & Act
            FriendRequestData request = new FriendRequestData
            {
                from = "",
                to = "",
                createdAtTicks = 0
            };

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("", request.from);
            Assert.AreEqual("", request.to);
            Assert.AreEqual(0, request.createdAtTicks);
        }
    }
}

