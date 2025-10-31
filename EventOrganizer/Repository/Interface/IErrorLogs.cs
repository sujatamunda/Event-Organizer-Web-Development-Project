namespace EventOrganizer.Repository.Interface
{
    public interface IErrorLogs
    {
        void ErrorLog(string LogLevel, string Message, string StackTrace);
    }
}
