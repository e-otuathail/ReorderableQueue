using NUnit;
using Moq;
using NUnit.Framework.Internal;
using Queue.Manager.Interfaces;

namespace Queue.Manager.Test.UnitTests
{
    public class QueueManagerTests
    {
        [Test]
        public void GetDirectionOfMove_WhenCurrentAndRequestedPositionAreTheSame_ThenReturnZero()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            int currentPosition = 1;
            int requestedPosition = 1;
            
            // Act
            var result = sut.GetDirectionOfMove(currentPosition, requestedPosition);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Dequeue_WhenNoItemsInTheQueue_ThenThrowInvalidOperationException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => sut.Dequeue());

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Queue is empty"));
        }


        [Test]
        public void Dequeue_WhenItemsInTheQueue_ThenReduceTheNumberOfItemsByOne()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            sut.Enqueue(new CustomObject { Name = "Item A", Region = "Dub" });
            sut.Enqueue(new CustomObject { Name = "Item B", Region = "Lux" });
            sut.Enqueue(new CustomObject { Name = "Item C", Region = "HK" });
            int itemCountBefore = sut.Count;

            // Act
            var itemDequeued = sut.Dequeue();

            // Assert
            Assert.That(itemCountBefore -1, Is.EqualTo(sut.Count));
        }

        [Test]
        public void Enqueue_WhenItemAddedToTheQueue_ThenTheQueuePositionIsSetToTheQueueCountPlusOne()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());

            // Act
            sut.Enqueue(new CustomObject { Name = "Item A", Region = "Dub" });
            sut.Enqueue(new CustomObject { Name = "Item B", Region = "Lux" });
            sut.Enqueue(new CustomObject { Name = "Item C", Region = "HK" });

            // Assert
            var items = sut.Sort();
            Assert.Multiple(() =>
            {
                Assert.That(items[0].QueuePosition, Is.EqualTo(1));
                Assert.That(items[1].QueuePosition, Is.EqualTo(2));
                Assert.That(items[2].QueuePosition, Is.EqualTo(3));
            });
            
        }

        [Test]
        public void RemoveItemAt_WhenItemRemovedFromSpecifiedPosition_ThenNumberOfItemsInTheQueueIsReducedByOne()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            sut.Enqueue(new CustomObject { Name = "Item A", Region = "Dub" });
            sut.Enqueue(new CustomObject { Name = "Item B", Region = "Lux" });
            sut.Enqueue(new CustomObject { Name = "Item C", Region = "HK" });

            // Act
            var itemsRemaining = sut.RemoveItemAt(3);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(itemsRemaining, Has.Length.EqualTo(2));
                Assert.That(sut.Count, Is.EqualTo(2));
            });
        }

        [Test]
        public void RemoveItemAt_WhenItemRemovedFromSpecifiedPosition_ThenQueuePositionForRemainingItemsIsUpdated()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            sut.Enqueue(new CustomObject { Name = "Item A", Region = "Dub" });
            sut.Enqueue(new CustomObject { Name = "Item B", Region = "Lux" });
            sut.Enqueue(new CustomObject { Name = "Item C", Region = "HK" });
            sut.Enqueue(new CustomObject { Name = "Item D", Region = "HK" });

            // Act
            var itemsRemaining = sut.RemoveItemAt(2);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(itemsRemaining[0].QueuePosition, Is.EqualTo(1));
                Assert.That(itemsRemaining[1].QueuePosition, Is.EqualTo(2));
                Assert.That(itemsRemaining[2].QueuePosition, Is.EqualTo(3));
            });
        }

        [Test]
        public void Demote_WhenCurrentPositionIsHigherThanRequestedPosition_ThenThrowInvalidOperationException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject itemX = new CustomObject { Name = "Item X", Region = "Dub" };
            CustomObject itemY = new CustomObject { Name = "Item Y", Region = "Dub" };
            CustomObject itemToMove = new CustomObject { Name = "Item A", Region = "Dub" };
            sut.Enqueue(itemX);
            sut.Enqueue(itemY);
            sut.Enqueue(itemToMove);

            int requestedPosition = 2;

            string expectedMessage = $"Current queue position <{itemToMove.QueuePosition}> " +
                $"cannot be lower in the queue than the requested position <{requestedPosition}>";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => sut.Demote(itemToMove, requestedPosition));

            // Assert
            Assert.That(ex.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public void Demote_WhenitemsAreBetweenCurrentAndRequestedPosition_ThenDecrementQueuePositionForAllByOne()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            var itemABeingMoved = new CustomObject { Name = "Item A", Region = "Dub" };
            var ItemBInQueue = new CustomObject { Name = "Item B", Region = "Lux" };
            var ItemCInQueue = new CustomObject { Name = "Item C", Region = "HK" };
            sut.Enqueue(itemABeingMoved);   // QueuePosition = 1
            sut.Enqueue(ItemBInQueue);      // QueuePosition = 2
            sut.Enqueue(ItemCInQueue);      // QueuePosition = 3

            int requestedPosition = 3;

            // Act
            sut.Demote(itemABeingMoved, requestedPosition);

            // Assert
            var actual = (from c in sut.Sort()
                            where c.Name == ItemCInQueue.Name
                            select c.QueuePosition)
                            .Single();

            Assert.That(actual, Is.EqualTo(2));
        }

        [Test]
        public void Demote_WhenTheItemParameterIsNull_ThenThrowArgumentNullException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject? customObject = null;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => sut.Demote(customObject, 2));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Value cannot be null. (Parameter 'item')"));
        }


        [Test]
        public void Demote_WhenNoItemsInTheQueue_ThenThrowInvalidOperationException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject customObject = new CustomObject { Name = "Item A", Region = "Dub" };

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => sut.Demote(customObject, 1));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Queue is empty"));
        }


        [Test]
        public void Demote_WhenRequestedPositionIsLessThanOne_ThenThrowArgumentException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject customObject = new CustomObject { Name = "Item A", Region = "Dub" };
            sut.Enqueue(customObject);
            int requestedPosition = 0;

            // Act
            var ex = Assert.Throws<ArgumentException>(() => sut.Demote(customObject, requestedPosition));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("The Requested Position is Invalid"));
        }


        [Test]
        public void Demote_WhenRequestedPositionIsGreaterThanTheNumberOfItemsInTheQueue_ThenThrowArgumentException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject customObject = new CustomObject { Name = "Item A", Region = "Dub" };
            sut.Enqueue(customObject);
            int requestedPosition = 2;

            // Act
            var ex = Assert.Throws<ArgumentException>(() => sut.Demote(customObject, requestedPosition));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("The Requested Position is Invalid"));
        }

        [Test]
        public void Promote_WhenitemsAreBetweenCurrentAndRequestedPosition_ThenIncrementQueuePositionForAllByOne()
        {
            // Arrange
            QueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            var itemInQueue = new CustomObject { Name = "Item A", Region = "Dub" };
            var item = new CustomObject { Name = "Item B", Region = "Lux" };
            sut.Enqueue(itemInQueue);
            sut.Enqueue(item);

            int requestedPosition = 1;

            // Act
            sut.Promote(item, requestedPosition);

            // Assert
            var actual = (from a in sut.Sort()
                          where a.Name == itemInQueue.Name
                          select a.QueuePosition)
                              .Single();

            Assert.That(actual, Is.EqualTo(2));
        }

        [Test]
        public void Promote_WhenCurrentPositionIsLessThanRequestedPosition_ThenThrowInvalidOperationException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject itemToMove = new CustomObject { Name = "Item A", Region = "Dub" };
            CustomObject itemB = new CustomObject { Name = "Item B", Region = "Dub" };
            CustomObject itemC = new CustomObject { Name = "Item C", Region = "Dub" };
            sut.Enqueue(itemToMove);
            sut.Enqueue(itemB);
            sut.Enqueue(itemC);

            int requestedPosition = 2;

            string expectedMessage = $"Current queue position <{itemToMove.QueuePosition}> " +
                $"cannot be higher in the queue than the requested position <{requestedPosition}>";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => sut.Promote(itemToMove, requestedPosition));

            // Assert
            Assert.That(ex.Message, Is.EqualTo(expectedMessage));
        }
        
        [Test]
        public void Promote_WhenTheItemParameterIsNull_ThenThrowArgumentNullException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject? customObject = null;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => sut.Promote(customObject, 2));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Value cannot be null. (Parameter 'item')"));
        }


        [Test]
        public void Promote_WhenNoItemsInTheQueue_ThenThrowInvalidOperationException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject customObject = new CustomObject { Name = "Item A", Region = "Dub" };

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => sut.Promote(customObject, 1));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Queue is empty"));
        }

        [Test]
        public void Promote_WhenRequestedPositionIsLessThanOne_ThenThrowArgumentException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject customObject = new CustomObject { Name = "Item A", Region = "Dub" };
            sut.Enqueue(customObject);
            int requestedPosition = 0;

            // Act
            var ex = Assert.Throws<ArgumentException>(() => sut.Promote(customObject, requestedPosition));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("The Requested Position is Invalid"));
        }


        [Test]
        public void Promote_WhenRequestedPositionIsGreaterThanTheNumberOfItemsInTheQueue_ThenThrowArgumentException()
        {
            // Arrange
            IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
            CustomObject customObject = new CustomObject { Name = "Item A", Region = "Dub" };
            sut.Enqueue(customObject);
            int requestedPosition = 2;

            // Act
            var ex = Assert.Throws<ArgumentException>(() => sut.Promote(customObject, requestedPosition));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("The Requested Position is Invalid"));
        }


        //[Test]
        //public void ReOrder_WhenItemNotInQueue_ThenThrowInvalidOperationException()
        //{
        //    // Arrange
        //    IQueueManager<CustomObject> sut = new QueueManager<CustomObject>(new QueuePositionComparer());
        //    CustomObject itemNotFound = new CustomObject { Name = "Missing", Region = "Dub" };
        //    CustomObject itemA = new CustomObject { Name = "Item A", Region = "Dub" };
        //    CustomObject itemB = new CustomObject { Name = "Item B", Region = "Dub" };
        //    sut.Enqueue(itemA);
        //    sut.Enqueue(itemB);

        //    // Act
        //    var ex = Assert.Throws<InvalidOperationException>(() => sut.Demote(itemNotFound, 1));

        //    // Assert   Item not found in the Queue
        //    Assert.That(ex.Message, Is.EqualTo("Queue is empty"));
        //}

    }
}
