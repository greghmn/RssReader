namespace RssReader.Core;

public enum ArticleStorageOperation
{
	Read,
	Write
}

public class ArticleStorageException : Exception
{
	public ArticleStorageOperation Operation { get; }

	public ArticleStorageException(
			string message,
			ArticleStorageOperation operation,
			Exception innerException)
			: base(message, innerException)
	{
		Operation = operation;
	}
}