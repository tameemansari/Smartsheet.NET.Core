using Smartsheet.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartsheet.Core.Entities;
using System.Net.Http;
using ProfessionalServices.Core.Interfaces;
using Smartsheet.Core.Definitions;
using Smartsheet.Core.Responses;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Smartsheet.Core.Interfaces
{
	public interface ISmartsheetHttpClient
	{
		//	Root Request Exection
		Task<TResult> ExecuteRequest<TResult, T>(HttpVerb verb, string url, T data, string accessToken = null);

		//	Authorization
		Task<HttpResponseMessage> RequestAuthorizationFromEndUser(string url, string clientId, string scopes, string state = "");
		Task<HttpResponseMessage> ObtainAccessToken(string url, string code, string clientId, string clientSecret, string redirectUri = "");
		Task<HttpResponseMessage> RefreshAccessToken(string url, string refreshToken, string clientId, string clientSecret, string redirectUri = "");
		Task<HttpResponseMessage> GetCurrentUserInformation(string url, string accessToken);

		//	Workspaces
		Task<ISmartsheetObject> CreateWorkspace(string workspaceName, string accessToken = null);
		Task<ISmartsheetObject> GetWorkspaceById(long? workspaceId, string accessToken = null);

		//	Sheets
		Task<Sheet> GetSheetById(long? sheetId, string accessToken = null);
		Task<Sheet> CreateSheet(string sheetName, IEnumerable<Column> columns, string folderId = null, string workspaceId = null, string accessToken = null);
		Task<Sheet> CreateSheetFromTemplate(string sheetName, long? templateId, long? folderId = null, long? workspaceId = null, string accessToken = null);
		Task<Sheet> CopySheet(string newName, long? sourceSheetId, long? destinationId, DestinationType destinationType, IEnumerable<SheetCopyInclusion> includes, string accessToken = null);
		Task<IEnumerable<Sheet>> GetSheetsForWorkspace(long? workspaceId, string accessToken = null);

		//	Rows
		Task<IEnumerable<Row>> CreateRows(long? sheetId, IEnumerable<Row> rows, bool? toTop = null, bool? toBottom = null, long? parentId = null, long? siblingId = null, string accessToken = null);
		Task<CopyOrMoveRowResult> MoveRows(long? sourceSheetId, long? destinationSheetId, IEnumerable<long> rowIds, string accessToken = null);
		Task<CopyOrMoveRowResult> CopyRows(long? sourceSheetId, long? destinationSheetId, IEnumerable<long> rowIds, string accessToken = null);
		Task<IEnumerable<ISmartsheetObject>> GetFoldersForWorkspace(long? workspaceId, string accessToken = null);
		Task<Folder> GetFolderById(long? folderId, string accessToken = null);

		//	Reports
		Task<IEnumerable<ISmartsheetObject>> GetReportsForWorkspace(long? workspaceId, string accessToken = null);

		//	Templates
		Task<IEnumerable<ISmartsheetObject>> GetTemplatesForWorkspace(long? workspaceId, string accessToken = null);

		//	Update Requests
		Task<UpdateRequest> CreateUpdateRequest(long? sheetId, IEnumerable<long> rowIds, IEnumerable<Recipient> sendTo, IEnumerable<long> columnIds, string subject = null, string message = null, bool ccMe = false, bool includeDiscussions = true, bool includeAttachments = true, string accessToken = null);

		//	Webhooks
		Task<IEnumerable<Webhook>> GetWebhooksForUser(string accessToken = null);
		Task<Webhook> GetWebhook(long? webhookId, string accessToken = null);
		Task<Webhook> CreateWebhook(Webhook model, string accessToken = null);
		Task<Webhook> UpdateWebhook(long? webhookId, Webhook model, string accessToken = null);

		//	Columns
		Task<Column> EditColumn(long? sheetId, long? columnId, Column model, string accessToken = null);

		//  Attachments
		Task<Attachment> UploadAttachmentToRow(long? sheetId, long? rowId, string fileName, long length, Stream data, string contentType = null, string accessToken = null);
		Task<Attachment> UploadAttachmentToRow(long? sheetId, long? rowId, IFormFile formFile, string accessToken = null);
	}
}
