namespace RssReader.Core;

//distinguish network failures from storage failures
public class ArticleStorageException : Exception
{
	public ArticleStorageException(
			string message,
			Exception innerException)
			: base(message, innerException)
	{
	}
}