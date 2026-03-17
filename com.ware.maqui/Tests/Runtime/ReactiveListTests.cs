using System.Collections.Generic;
using NUnit.Framework;
using Maqui.Core.Logic;
using R3;

namespace Maqui.Tests
{
    public class ReactiveListTests
    {
        private ReactiveList<string> _list;

        [SetUp]
        public void SetUp()
        {
            _list = new ReactiveList<string>();
        }

        [TearDown]
        public void TearDown()
        {
            _list.Dispose();
        }

        // ── Construction ────────────────────────────────────────────────────

        [Test]
        public void Constructor_Empty_HasZeroCount()
        {
            Assert.AreEqual(0, _list.Count);
        }

        [Test]
        public void Constructor_WithInitial_PopulatesList()
        {
            using var list = new ReactiveList<int>(new[] { 10, 20, 30 });
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(20, list[1]);
        }

        // ── Add ─────────────────────────────────────────────────────────────

        [Test]
        public void Add_IncreasesCount()
        {
            _list.Add("alpha");
            Assert.AreEqual(1, _list.Count);
            Assert.AreEqual("alpha", _list[0]);
        }

        [Test]
        public void Add_EmitsAddEvent()
        {
            ListAddEvent<string> received = default;
            _list.ObserveAdd().Subscribe(e => received = e);

            _list.Add("beta");

            Assert.AreEqual(0, received.Index);
            Assert.AreEqual("beta", received.Item);
        }

        [Test]
        public void Add_UpdatesCountObservable()
        {
            var counts = new List<int>();
            _list.ObserveCountChanged().Subscribe(c => counts.Add(c));

            _list.Add("a");
            _list.Add("b");

            // Initial emit (0) + two adds (1, 2)
            Assert.Contains(1, counts);
            Assert.Contains(2, counts);
        }

        // ── Insert ──────────────────────────────────────────────────────────

        [Test]
        public void Insert_AtIndex_ShiftsElements()
        {
            _list.Add("a");
            _list.Add("c");
            _list.Insert(1, "b");

            Assert.AreEqual(3, _list.Count);
            Assert.AreEqual("a", _list[0]);
            Assert.AreEqual("b", _list[1]);
            Assert.AreEqual("c", _list[2]);
        }

        [Test]
        public void Insert_EmitsAddEventWithCorrectIndex()
        {
            _list.Add("a");
            _list.Add("c");

            ListAddEvent<string> received = default;
            _list.ObserveAdd().Subscribe(e => received = e);

            _list.Insert(1, "b");

            Assert.AreEqual(1, received.Index);
            Assert.AreEqual("b", received.Item);
        }

        // ── Remove ──────────────────────────────────────────────────────────

        [Test]
        public void Remove_ExistingItem_ReturnsTrue()
        {
            _list.Add("a");
            _list.Add("b");

            Assert.IsTrue(_list.Remove("a"));
            Assert.AreEqual(1, _list.Count);
            Assert.AreEqual("b", _list[0]);
        }

        [Test]
        public void Remove_NonExistentItem_ReturnsFalse()
        {
            _list.Add("a");
            Assert.IsFalse(_list.Remove("z"));
            Assert.AreEqual(1, _list.Count);
        }

        [Test]
        public void Remove_EmitsRemoveEvent()
        {
            _list.Add("x");
            _list.Add("y");

            ListRemoveEvent<string> received = default;
            _list.ObserveRemove().Subscribe(e => received = e);

            _list.Remove("x");

            Assert.AreEqual(0, received.Index);
            Assert.AreEqual("x", received.Item);
        }

        // ── RemoveAt ────────────────────────────────────────────────────────

        [Test]
        public void RemoveAt_RemovesCorrectElement()
        {
            _list.Add("a");
            _list.Add("b");
            _list.Add("c");

            _list.RemoveAt(1);

            Assert.AreEqual(2, _list.Count);
            Assert.AreEqual("a", _list[0]);
            Assert.AreEqual("c", _list[1]);
        }

        // ── Replace (indexer set) ───────────────────────────────────────────

        [Test]
        public void Indexer_Set_EmitsReplaceEvent()
        {
            _list.Add("old");

            ListReplaceEvent<string> received = default;
            _list.ObserveReplace().Subscribe(e => received = e);

            _list[0] = "new";

            Assert.AreEqual(0, received.Index);
            Assert.AreEqual("old", received.OldItem);
            Assert.AreEqual("new", received.NewItem);
            Assert.AreEqual("new", _list[0]);
        }

        // ── Clear ───────────────────────────────────────────────────────────

        [Test]
        public void Clear_EmptyList_EmitsResetEvent()
        {
            _list.Add("a");
            _list.Add("b");

            bool resetReceived = false;
            _list.ObserveReset().Subscribe(_ => resetReceived = true);

            _list.Clear();

            Assert.IsTrue(resetReceived);
            Assert.AreEqual(0, _list.Count);
        }

        // ── AddRange ────────────────────────────────────────────────────────

        [Test]
        public void AddRange_EmitsMultipleAddEvents()
        {
            var received = new List<ListAddEvent<string>>();
            _list.ObserveAdd().Subscribe(e => received.Add(e));

            _list.AddRange(new[] { "a", "b", "c" });

            Assert.AreEqual(3, received.Count);
            Assert.AreEqual(0, received[0].Index);
            Assert.AreEqual(1, received[1].Index);
            Assert.AreEqual(2, received[2].Index);
        }

        // ── Move ────────────────────────────────────────────────────────────

        [Test]
        public void Move_ReordersElements()
        {
            _list.Add("a");
            _list.Add("b");
            _list.Add("c");

            _list.Move(0, 2);

            Assert.AreEqual("b", _list[0]);
            Assert.AreEqual("c", _list[1]);
            Assert.AreEqual("a", _list[2]);
        }

        // ── Query ───────────────────────────────────────────────────────────

        [Test]
        public void Contains_ReturnsCorrectly()
        {
            _list.Add("hello");
            Assert.IsTrue(_list.Contains("hello"));
            Assert.IsFalse(_list.Contains("world"));
        }

        [Test]
        public void IndexOf_ReturnsCorrectIndex()
        {
            _list.Add("a");
            _list.Add("b");
            Assert.AreEqual(1, _list.IndexOf("b"));
            Assert.AreEqual(-1, _list.IndexOf("z"));
        }

        // ── Enumeration ─────────────────────────────────────────────────────

        [Test]
        public void Enumeration_IteratesAllItems()
        {
            _list.Add("a");
            _list.Add("b");
            _list.Add("c");

            var result = new List<string>();
            foreach (var item in _list)
                result.Add(item);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("a", result[0]);
            Assert.AreEqual("b", result[1]);
            Assert.AreEqual("c", result[2]);
        }

        // ── Dispose ─────────────────────────────────────────────────────────

        [Test]
        public void Dispose_CompletesAllSubjects()
        {
            bool addCompleted = false;
            bool removeCompleted = false;
            bool resetCompleted = false;

            _list.ObserveAdd().Subscribe(_ => { }, _ => addCompleted = true);
            _list.ObserveRemove().Subscribe(_ => { }, _ => removeCompleted = true);
            _list.ObserveReset().Subscribe(_ => { }, _ => resetCompleted = true);

            _list.Dispose();

            Assert.IsTrue(addCompleted);
            Assert.IsTrue(removeCompleted);
            Assert.IsTrue(resetCompleted);

            // Prevent double dispose in TearDown
            _list = new ReactiveList<string>();
        }
    }
}
