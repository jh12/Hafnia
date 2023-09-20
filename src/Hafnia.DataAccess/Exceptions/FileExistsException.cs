namespace Hafnia.DataAccess.Exceptions;

public class FileExistsException : Exception
{
    public FileExistsException() : base("File already exists")
    {
    }
}
