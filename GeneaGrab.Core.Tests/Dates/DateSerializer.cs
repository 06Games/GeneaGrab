using GeneaGrab.Core.Models.Dates;
using GeneaGrab.Core.Tests.Dates;
using Newtonsoft.Json;
using Xunit.Sdk;

[assembly: RegisterXunitSerializer(typeof(DateSerializer), typeof(Date))]

namespace GeneaGrab.Core.Tests.Dates;

public class DateSerializer : IXunitSerializer
{
    public bool IsSerializable(Type type, object? value, [NotNullWhen(false)] out string? failureReason)
    {
        failureReason = null;
        if (value is Date)
            return true;

        failureReason = $"Type {type.Name} is not a Date type.";
        return false;
    }

    public object Deserialize(Type type, string serializedValue)
    {
        if (type != typeof(Date))
            throw new ArgumentException($"Cannot deserialize type {type.Name} to Date.", nameof(type));
        return JsonConvert.DeserializeObject<Date>(serializedValue) ?? throw new InvalidOperationException();
    }

    public string Serialize(object value)
    {
        if (value is not Date date)
            throw new ArgumentException($"Cannot serialize type {value.GetType().Name} as Date.", nameof(value));
        return JsonConvert.SerializeObject(date);
    }
}