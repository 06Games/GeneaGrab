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
    public abstract class Generic
    {
        [JsonProperty] public int Value { get; set; }
        public virtual string Long => Medium;
        public virtual string Medium => Short;
        public virtual string Short => Value.ToString("00");
    }

    public abstract class GenericYear : Generic, IYear
    {
        public override string Short => Value.ToString("0000");
    }
    public abstract class GenericMonth : Generic, IMonth { }
    public abstract class GenericDay : Generic, IDay { }
    public abstract class GenericHour : Generic, IHour { }
    public abstract class GenericMinute : Generic, IMinute { }
    public abstract class GenericSecond : Generic, ISecond { }
}
