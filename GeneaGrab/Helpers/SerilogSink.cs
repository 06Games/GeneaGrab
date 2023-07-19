using System.Diagnostics.CodeAnalysis;
using Avalonia.Logging;

namespace GeneaGrab.Helpers;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
public class SerilogSink: ILogSink
{
    public bool IsEnabled(LogEventLevel level, string area) => true;
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Serilog.Log
            .ForContext("Area", area)
            .ForContext("Source", source)
            .Write((Serilog.Events.LogEventLevel)level, messageTemplate);
    }
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        Serilog.Log
            .ForContext("Area", area)
            .ForContext("Source", source)
            .Write((Serilog.Events.LogEventLevel)level, messageTemplate, propertyValues);
    }
}
