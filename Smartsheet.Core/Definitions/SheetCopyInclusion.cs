using System;
namespace Smartsheet.NET.Core.Definitions
{
	public enum SheetCopyInclusion
	{
		/// <summary>
		/// Includes the data.
		/// </summary>
		Data,

		/// <summary>
		/// Includes the attachments.
		/// </summary>
		Attachments,

		/// <summary>
		/// Includes the discussions.
		/// </summary>
		Discussions,

		/// <summary>
		/// Includes cell links.
		/// </summary>
		CellLinks,

		/// <summary>
		/// Includes the forms.
		/// </summary>
		Forms,

		/// <summary>
		/// includeS everything (data, attachments, discussions, cellLinks, and forms).
		/// </summary>
		All,
	}
}
