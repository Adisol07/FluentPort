namespace FluentPort.SDK;

public class Log
{
    public LogLevels Level { get; set; }
    public DateTime Created { get; set; }
    public string? Source { get; set; }
    public string? Message { get; set; }

    public Log()
    { }
    public Log(string message)
    {
        Created = DateTime.Now;
        Level = LogLevels.Information;
        Source = "";
        Message = message;
    }
    public Log(string source, string message)
    {
        Created = DateTime.Now;
        Level = LogLevels.Information;
        Source = source;
        Message = message;
    }
    public Log(LogLevels level, string source, string message)
    {
        Created = DateTime.Now;
        Level = level;
        Source = source;
        Message = message;
    }
    public Log(DateTime created, LogLevels level, string source, string message)
    {
        Created = created;
        Level = level;
        Source = source;
        Message = message;
    }

    public override string ToString()
    {
        return "[" + Created.ToString() + " : " + Level.ToString() + "](" + Source + "): " + Message;
    }
}
public enum LogLevels
{
    Information = 0,
    Warning = 1,
    Error = 2,
}
