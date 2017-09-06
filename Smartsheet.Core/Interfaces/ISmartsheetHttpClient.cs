using Smartsheet.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartsheet.Core.Entities;

namespace Smartsheet.Core.Interfaces
{
	public interface ISmartsheetHttpClient
	{
		Task<TResult> ExecuteRequest<TResult, T>(HttpVerb verb, string url, T data, string accessToken = null);

        Task<Sheet> GetSheetById(long? sheetId, string accessToken = null);
	}
}
