using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace GeneaGrab.Core.Models.Dates
{
    public interface IYear { int Value { get; } string Long { get; } string Medium { get; } string Short { get; } }
    public interface IMonth { int Value { get; } string Long { get; } string Medium { get; } string Short { get; } }
    public interface IDay { int Value { get; } string Long { get; } string Medium { get; } string Short { get; } }
    public interface IHour { int Value { get; } string Long { get; } string Medium { get; } string Short { get; } }
    public interface IMinute { int Value { get; } string Long { get; } string Medium { get; } string Short { get; } }
    public interface ISecond { int Value { get; } string Long { get; } string Medium { get; } string Short { get; } }

    [JsonObject(MemberSerialization.OptIn)]
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public abstract class Generic
    {
        protected Generic(uint value)
        {
            if (value < MinValue || value > MaxValue) throw new ArgumentOutOfRangeException(nameof(value), value, $"The value needs to be between {MinValue} and {MaxValue}");
            Value = (int)value;
        }
        protected Generic(int value) 
        {
            if (value < MinValue || value > MaxValue) throw new ArgumentOutOfRangeException(nameof(value), value, $"The value needs to be between {MinValue} and {MaxValue}");
            Value = value;
        }
        
        [JsonProperty] public int Value { get; set; }
        public virtual string Long => Medium;
        public virtual string Medium => Short;
        public virtual string Short => Value.ToString("00");

        internal abstract int MinValue { get; }
        internal abstract int MaxValue{ get; }

        public override string ToString() => Medium;
    }

    public abstract class GenericYear : Generic, IYear
    {
        public override string Short => Value.ToString("0000");
        protected GenericYear(uint value) : base(value) { }
        protected GenericYear(int value) : base(value) { }
    }
    public abstract class GenericMonth : Generic, IMonth
    {
        protected GenericMonth(uint value) : base(value) { }
        protected GenericMonth(int value) : base(value) { }
    }
    public abstract class GenericDay : Generic, IDay
    {
        protected GenericDay(uint value) : base(value) { }
        protected GenericDay(int value) : base(value) { }
    }
    public abstract class GenericHour : Generic, IHour
    {
        protected GenericHour(uint value) : base(value) { }
        protected GenericHour(int value) : base(value) { }
    }
    public abstract class GenericMinute : Generic, IMinute
    {
        protected GenericMinute(uint value) : base(value) { }
        protected GenericMinute(int value) : base(value) { }
    }
    public abstract class GenericSecond : Generic, ISecond
    {
        protected GenericSecond(uint value) : base(value) { }
        protected GenericSecond(int value) : base(value) { }
    }
}
