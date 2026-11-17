using Inspection_Control_App.Model;
using Xunit;

namespace Inspection_Control_App.Tests
{
    public class LayoutDraftTests
    {
        private static MachineSlot M(int id, string name, int row, int col) =>
            new() { Id = id, MachineName = name, RowNo = row, ColNo = col };

        private static LayoutDraft TwoMachines() =>
            new(new[] { M(1, "F2Tab22010165", 1, 1), M(2, "SPC-F2JP005", 2, 1) });

        private static MachineSlot ByName(LayoutDraft d, string name) =>
            d.Slots.Single(s => s.MachineName == name);

        [Fact]
        public void Move_ToEmptyCell_Moves()
        {
            var d = TwoMachines();
            var a = ByName(d, "F2Tab22010165");

            Assert.True(d.Move(a, 6, 4));
            Assert.Same(a, d.At(6, 4));
            Assert.Null(d.At(1, 1));
            Assert.Equal(1, d.ChangeCount);
        }

        [Fact]
        public void Move_ToOccupiedCell_Swaps()
        {
            var d = TwoMachines();
            var a = ByName(d, "F2Tab22010165");
            var b = ByName(d, "SPC-F2JP005");

            Assert.True(d.Move(a, 2, 1));
            Assert.Same(a, d.At(2, 1));
            Assert.Same(b, d.At(1, 1));
            Assert.Equal(2, d.ChangeCount);
        }

        [Fact]
        public void Move_OutsideFrame_ReturnsFalse()
        {
            var d = TwoMachines();
            var a = ByName(d, "F2Tab22010165");

            Assert.False(d.Move(a, 7, 1));
            Assert.False(d.Move(a, 1, 5));
            Assert.False(d.Move(a, 0, 1));
            Assert.Same(a, d.At(1, 1));
            Assert.False(d.HasChanges);
        }

        [Fact]
        public void Move_SameCell_ReturnsFalse()
        {
            var d = TwoMachines();
            Assert.False(d.Move(ByName(d, "F2Tab22010165"), 1, 1));
        }

        [Fact]
        public void Move_BackToOriginal_NoChanges()
        {
            var d = TwoMachines();
            var a = ByName(d, "F2Tab22010165");

            d.Move(a, 3, 3);
            d.Move(a, 1, 1);

            Assert.False(d.HasChanges);
        }

        [Fact]
        public void Add_EmptyName_Rejected()
        {
            var d = TwoMachines();
            Assert.NotNull(d.Add("   ", 3, 3));
            Assert.Null(d.At(3, 3));
        }

        [Fact]
        public void Add_OccupiedCell_Rejected()
        {
            var d = TwoMachines();
            Assert.NotNull(d.Add("NEW01", 1, 1));
        }

        [Fact]
        public void Add_OutsideFrame_Rejected()
        {
            var d = TwoMachines();
            Assert.NotNull(d.Add("NEW01", 7, 1));
        }

        [Fact]
        public void Add_DuplicateIgnoringCase_Rejected()
        {
            var d = TwoMachines();
            Assert.NotNull(d.Add("f2tab22010165", 3, 3));
        }

        [Fact]
        public void Add_Valid_AddsTrimmedNewMachine()
        {
            var d = TwoMachines();

            Assert.Null(d.Add("  NEW01 ", 3, 2));
            var added = d.At(3, 2);
            Assert.NotNull(added);
            Assert.Equal("NEW01", added!.MachineName);
            Assert.Equal(0, added.Id);
            Assert.Equal(1, d.ChangeCount);
        }

        [Fact]
        public void Add_NameOfDeletedMachine_Allowed()
        {
            var d = TwoMachines();
            d.Remove(ByName(d, "F2Tab22010165"));

            Assert.Null(d.Add("F2Tab22010165", 4, 4));
        }

        [Fact]
        public void Remove_ExistingMachine_RecordsDeletedId()
        {
            var d = TwoMachines();
            d.Remove(ByName(d, "SPC-F2JP005"));

            Assert.Equal(new[] { 2 }, d.DeletedIds);
            Assert.Null(d.At(2, 1));
            Assert.Equal(1, d.ChangeCount);
        }

        [Fact]
        public void Remove_NewMachine_NoDeletedId()
        {
            var d = TwoMachines();
            d.Add("NEW01", 3, 3);
            d.Remove(d.At(3, 3)!);

            Assert.Empty(d.DeletedIds);
            Assert.False(d.HasChanges);
        }

        [Fact]
        public void Rename_DuplicateIgnoringCase_Rejected()
        {
            var d = TwoMachines();
            Assert.NotNull(d.Rename(ByName(d, "F2Tab22010165"), "spc-f2jp005"));
        }

        [Fact]
        public void Rename_SameNameDifferentCase_Allowed()
        {
            var d = TwoMachines();
            var a = ByName(d, "F2Tab22010165");

            Assert.Null(d.Rename(a, "F2TAB22010165"));
            Assert.Equal("F2TAB22010165", a.MachineName);
            Assert.Equal(1, d.ChangeCount);
        }

        [Fact]
        public void Rename_Empty_Rejected()
        {
            var d = TwoMachines();
            Assert.NotNull(d.Rename(ByName(d, "F2Tab22010165"), ""));
        }

        [Fact]
        public void OutOfFrame_KeptAndNotCounted()
        {
            var d = new LayoutDraft(new[] { M(1, "A", 1, 1), M(9, "FAR", 9, 1) });

            Assert.Contains(d.Slots, s => s.MachineName == "FAR");
            Assert.False(d.HasChanges);
            Assert.NotNull(d.Add("far", 2, 2)); // trùng mã máy ngoài khung -> bị chặn
        }

        [Fact]
        public void Draft_DoesNotMutateOriginalSlots()
        {
            var original = M(1, "A", 1, 1);
            var d = new LayoutDraft(new[] { original });

            d.Move(d.Slots[0], 2, 2);

            Assert.Equal(1, original.RowNo);
            Assert.Equal(1, original.ColNo);
        }
    }
}
