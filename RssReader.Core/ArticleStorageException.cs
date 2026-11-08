// Copyright (C) 2026 Greg Homan
// SPDX-License-Identifier: AGPL-3.0-or-later
// Additional terms: See ADDITIONAL-TERMS.md in the repository root.
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