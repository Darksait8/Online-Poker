using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Тесты для TableInfo
    /// </summary>
    public class TableInfoTests
    {
        [Test]
        public void TableInfo_CanBeCreated()
        {
            // Arrange & Act
            TableInfo tableInfo = new TableInfo("Test Table", 10, 6);

            // Assert
            Assert.IsNotNull(tableInfo);
            Assert.AreEqual("Test Table", tableInfo.tableName);
            Assert.AreEqual(10, tableInfo.smallBlind);
            Assert.AreEqual(20, tableInfo.bigBlind);
            Assert.AreEqual(6, tableInfo.maxSeats);
        }

        [Test]
        public void TableInfo_BigBlind_IsDoubleSmallBlind()
        {
            // Arrange
            TableInfo tableInfo = new TableInfo("Test Table", 25, 6);

            // Assert
            Assert.AreEqual(tableInfo.smallBlind * 2, tableInfo.bigBlind);
        }

        [Test]
        public void TableInfo_MaxSeats_IsWithinValidRange()
        {
            // Arrange & Act
            TableInfo tableInfo = new TableInfo("Test Table", 10, 6);

            // Assert
            Assert.GreaterOrEqual(tableInfo.maxSeats, 2);
            Assert.LessOrEqual(tableInfo.maxSeats, 9);
        }
    }
}

