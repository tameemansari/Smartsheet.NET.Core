using Newtonsoft.Json;
using Smartsheet.NET.Core.Definitions;
using Smartsheet.NET.Core.Entities;
using ProfessionalServices.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using ProfessionalServices.Core.Responses;
using System.Threading.Tasks;
using System.Threading;
using Smartsheet.NET.Core.Interfaces;
using Smartsheet.NET.Core.Responses;
using System.Collections;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Smartsheet.NET.Core.Configuration;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Smartsheet.NET.Core.Http
{
	public class SmartsheetHttpClient : ISmartsheetHttpClient
	{
		private HttpClient _HttpClient = new HttpClient();
        public string _AccessToken { get; private set; }
        public string _ChangeAgent { get; private set; }
		private static int _AttemptLimit = 10;
		private int _WaitTime = 0;
		private int _RetryCount = 0;
		private bool _RetryRequest = true;

		public SmartsheetHttpClient(IOptions<ApplicationSettings> options)
		{
			this._AccessToken = options.Value.SmartsheetCredentials.AccessToken;
			this._ChangeAgent = options.Value.SmartsheetCredentials.ChangeAgent;
			this.InitializeHttpClient();
		}

		public SmartsheetHttpClient(string accessToken, string changeAgent)
		{
			this._AccessToken = accessToken;
			this._ChangeAgent = changeAgent;
			this.InitializeHttpClient();
		}

		/// <summary>
		/// Set the base address, and default request headers
		/// for the Http client prior to sending a request.
		/// </summary>
		private void InitializeHttpClient()
		{
			this._HttpClient.BaseAddress = new Uri("https://api.smartsheet.com/2.0/");
			this._HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
		}

		/// <summary>
		/// Sets the authorization header.
		/// </summary>
		/// <param name="accessToken">Access token.</param>
		public void SetAuthorizationHeader(string accessToken)
		{
			if (accessToken != null)
			{
				this._HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
			}
			else
			{
				this._HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + this._AccessToken);
			}
		}

		/// <summary>
		/// Executes any, and all requests against the Smartsheet API. Additionally,
		/// handles all retry logic and serialization / deserialization of 
		/// requests / responses.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="verb">Verb.</param>
		/// <param name="url">URL.</param>
		/// <param name="data">Data.</param>
		/// <param name="secure">If set to <c>true</c> secure.</param>
		/// <typeparam name="TResult">The 1st type parameter.</typeparam>
		/// <typeparam name="T">The 2nd type parameter.</typeparam>
		public async Task<TResult> ExecuteRequest<TResult, T>(HttpVerb verb, string url, T data, string accessToken = null)
		{
			this.ValidateRequestInjectedResult(typeof(TResult));

			//this.ValidateRequestInjectedType(typeof(T));

			this._HttpClient.DefaultRequestHeaders.Remove("Authorization");

			if (accessToken != null)
			{
				this._HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
			}
			else
			{
				this._HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + this._AccessToken);
			}

			if (this._ChangeAgent != null)
			{
				this._HttpClient.DefaultRequestHeaders.Add("Smartsheet-Change-Agent", this._ChangeAgent);
			}

			this.ValidateClientParameters();

			this.InitiazeNewRequest();

			while (_RetryRequest && (_RetryCount < _AttemptLimit))
			{
				try
				{
					if (_WaitTime > 0)
					{
						Thread.Sleep(_WaitTime);
					}

					HttpResponseMessage response;

					var serializerSettings = new JsonSerializerSettings()
					{
						NullValueHandling = NullValueHandling.Ignore,
						ContractResolver = new CamelCasePropertyNamesContractResolver()
					};

					var serializedData = JsonConvert.SerializeObject(data, Formatting.None, serializerSettings);

					switch (verb)
					{
						default:
						case HttpVerb.GET:
							response = await this._HttpClient.GetAsync(url);
							break;
						case HttpVerb.PUT:
							response = await this._HttpClient.PutAsync(url, new StringContent(serializedData, System.Text.Encoding.UTF8, "application/json"));
							break;
						case HttpVerb.POST:
							response = await this._HttpClient.PostAsync(url, new StringContent(serializedData, System.Text.Encoding.UTF8, "application/json"));
							break;
						case HttpVerb.DELETE:
							response = await this._HttpClient.DeleteAsync(url);
							break;
					}

					var statusCode = response.StatusCode;

					if (statusCode == HttpStatusCode.OK)
					{
						try
						{
							var responseBody = await response.Content.ReadAsStringAsync();

							var jsonReponseBody = JsonConvert.DeserializeObject(responseBody).ToString();

							var resultResponse = JsonConvert.DeserializeObject<TResult>(jsonReponseBody);

							return resultResponse;
						}
						catch (Exception e)
						{
							throw e;
						}
					}

					if (statusCode.Equals(HttpStatusCode.InternalServerError) || statusCode.Equals(HttpStatusCode.ServiceUnavailable) || statusCode.Equals((HttpStatusCode)429)) // .NET doesn't have a const for this
					{
						var responseJson = await response.Content.ReadAsStringAsync();

						dynamic result = JsonConvert.DeserializeObject(responseJson);

						// did we hit an error that we should retry?
						int code = result["errorCode"];

						if (code == 4001)
						{
							// service may be down temporarily
							_WaitTime = Backoff(_WaitTime, 60 * 1000);
						}
						else if (code == 4002 || code == 4004)
						{
							// internal error or simultaneous update.
							_WaitTime = Backoff(_WaitTime, 1 * 1000);
						}
						else if (code == 4003)
						{
							// rate limit
							_WaitTime = Backoff(_WaitTime, 2 * 1000);
						}
					}
					else
					{
						_RetryRequest = false;
						dynamic result;
						try
						{
							var responseJson = await response.Content.ReadAsStringAsync();

							result = JsonConvert.DeserializeObject(responseJson);
						}
						catch (Exception)
						{
							throw new Exception(string.Format("HTTP Error {0}: url:[{1}]", statusCode, url));
						}

						var message = string.Format("Smartsheet error code {0}: {1} url:[{2}]", result["errorCode"], result["message"], url);

						throw new Exception(message);
					}
				}
				catch (Exception e)
				{
					if (!_RetryRequest)
					{
						throw e;
					}
				}

				_RetryCount += 1;
			}

			throw new Exception(string.Format("Retries exceeded.  url:[{0}]", url));
		}

		/// <summary>
		/// Backoff the specified current and minimumWait.
		/// </summary>
		/// <returns>The backoff.</returns>
		/// <param name="current">Current.</param>
		/// <param name="minimumWait">Minimum wait.</param>
		private static int Backoff(int current, int minimumWait)
		{
			if (current > 0)
			{
				return current * 2;
			}

			return minimumWait;
		}

		/// <summary>
		/// Validates the request injected result.
		/// </summary>
		/// <param name="type">Type.</param>
		private void ValidateRequestInjectedResult(Type type)
		{
			if (!type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ISmartsheetResult)))
			{
				throw new Exception("Injected type must implement interface ISmartsheetResult");
			}
		}

		/// <summary>
		/// Validates the type of the request injected.
		/// </summary>
		/// <param name="type">Type.</param>
		private void ValidateRequestInjectedType(Type type)
		{
			if (typeof(IEnumerable).IsAssignableFrom(type))
			{
				if (type.GetGenericArguments()[0] != typeof(ISmartsheetObject))
				{
					throw new Exception("Injected type must implement interface ISmartsheetObject");
				}
			}
			else
			{
				if (!type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ISmartsheetObject)))
				{
					throw new Exception("Injected type must implement interface ISmartsheetObject");
				}
			}
		}

		/// <summary>
		/// Validates the client parameters.
		/// </summary>
		private void ValidateClientParameters()
		{
			if (this._AccessToken == null || string.IsNullOrWhiteSpace(this._AccessToken))
			{
				throw new ArgumentException("Access Token must be provided");
			}
		}

		/// <summary>
		/// Initiazes the new request.
		/// </summary>
		private void InitiazeNewRequest()
		{
			this._WaitTime = 0;
			this._RetryCount = 0;
			this._RetryRequest = true;
		}

		//
		//  Authorization
		#region Authorization
		public async Task<HttpResponseMessage> RequestAuthorizationFromEndUser(string url, string clientId, string scopes, string state = "")
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new Exception("Provided Smartsheet Api URL cannot be null");
			}

			var paramaters = new Dictionary<string, string>()
			{
				{ "response_type", "code" },
				{ "client_id", clientId },
				{ "state", state },
				{ "scope", scopes }
			};

			var uri = QueryHelpers.AddQueryString(url, paramaters);

			var response = await this._HttpClient.GetAsync(uri);

			return response;
		}

		public async Task<HttpResponseMessage> ObtainAccessToken(string url, string code, string clientId, string clientSecret, string redirectUri = "")
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				throw new Exception("Provided Smartsheet Code cannot be null");
			}

			var hash = SHA.GenerateSHA256String(clientSecret + "|" + code);

			var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("grant_type", "authorization_code"),
				new KeyValuePair<string, string>("client_id", clientId),
				new KeyValuePair<string, string>("code", code),
				new KeyValuePair<string, string>("hash", hash)
			});

			content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

			var response = await this._HttpClient.PostAsync(url, content);

			return response;
		}

		public async Task<HttpResponseMessage> RefreshAccessToken(string url, string refreshToken, string clientId, string clientSecret, string redirectUri = "")
		{
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				throw new Exception("Provided Smartsheet Refresh Token cannot be null");
			}

			var hash = SHA.GenerateSHA256String(clientSecret + "|" + refreshToken);

			var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("grant_type", "refresh_token"),
				new KeyValuePair<string, string>("client_id", clientId),
				new KeyValuePair<string, string>("refresh_token", refreshToken),
				new KeyValuePair<string, string>("hash", hash)
			});

			content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

			var response = await this._HttpClient.PostAsync(url, content);

			return response;
		}

		public async Task<HttpResponseMessage> GetCurrentUserInformation(string url, string accessToken)
		{
			if (string.IsNullOrWhiteSpace(accessToken))
			{
				throw new Exception("Provided Smartsheet Access Token cannot be null");
			}

			this._HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

			var response = await this._HttpClient.GetAsync(url);

			return response;
		}
		#endregion

		//
		//  Workspaces
		#region Workspaces
		public async Task<ISmartsheetObject> CreateWorkspace(string workspaceName, string accessToken = null)
		{
			if (string.IsNullOrWhiteSpace(workspaceName))
			{
				throw new Exception("Workspace Name cannot be null or blank");
			}

			var workspace = new Workspace(workspaceName);

			var response = await this.ExecuteRequest<ResultResponse<Workspace>, Workspace>(HttpVerb.POST, string.Format("workspaces"), workspace, accessToken: accessToken);

			return response.Result;
		}

		public async Task<ISmartsheetObject> GetWorkspaceById(long? workspaceId, string accessToken = null)
		{
			if (workspaceId == null)
			{
				throw new Exception("Workspace ID cannot be null");
			}

			var response = await this.ExecuteRequest<Workspace, Workspace>(HttpVerb.GET, string.Format("workspaces/{0}", workspaceId), null, accessToken: accessToken);

			return response;
		}

		#endregion

		//
		//  Sheets
		#region Sheets
		public async Task<Sheet> CreateSheet(string sheetName, IEnumerable<Column> columns, string folderId = null, string workspaceId = null, string accessToken = null)
		{
			if (string.IsNullOrWhiteSpace(sheetName))
			{
				throw new Exception("Sheet Name cannot be null or blank");
			}

			var sheet = new Sheet(sheetName, columns.ToList());

			var response = await this.ExecuteRequest<ResultResponse<Sheet>, Sheet>(HttpVerb.POST, string.Format("sheets"), sheet, accessToken: accessToken);

			response.Result._Client = this;

			return response.Result;
		}

		public async Task<Sheet> UpdateSheet(long? sheetId, Sheet sheet, string accessToken = null)
		{
			if (sheet == null)
			{
				throw new Exception("Sheet cannot be null or blank");
			}

			var response = await this.ExecuteRequest<ResultResponse<Sheet>, Sheet>(HttpVerb.PUT, string.Format("sheets/{0}", sheetId), sheet, accessToken: accessToken);

			response.Result._Client = this;

			return response.Result;
		}

		public async Task<Sheet> CreateSheetFromTemplate(string sheetName, long? templateId, long? folderId = null, long? workspaceId = null, string accessToken = null)
		{
			if (string.IsNullOrWhiteSpace(sheetName))
			{
				throw new Exception("Sheet Name cannot be null or blank");
			}

			if (templateId == null)
			{
				throw new Exception("Template ID cannot be null or blank");
			}

			var sheet = new Sheet(sheetName, null);
			sheet.FromId = templateId;

			var response = new ResultResponse<Sheet>();

			if (folderId == null && workspaceId == null)
			{
				response = await this.ExecuteRequest<ResultResponse<Sheet>, Sheet>(HttpVerb.POST, string.Format("sheets"), sheet, accessToken: accessToken);
			}
			else if (folderId != null && workspaceId == null) // Folders
			{
				response = await this.ExecuteRequest<ResultResponse<Sheet>, Sheet>(HttpVerb.POST, string.Format("folders/{0}/sheets?include=data", folderId), sheet, accessToken: accessToken);
			}
			else if (folderId == null && workspaceId != null) // Folders
			{
				response = await this.ExecuteRequest<ResultResponse<Sheet>, Sheet>(HttpVerb.POST, string.Format("workspaces/{0}/sheets", workspaceId), sheet, accessToken: accessToken);
			}

			response.Result._Client = this;

			return response.Result;
		}

		public async Task<Sheet> CopySheet(string newName, long? sourceSheetId, long? destinationId, DestinationType destinationType, IEnumerable<SheetCopyInclusion> includes, string accessToken = null)
		{
			if (string.IsNullOrWhiteSpace(newName))
			{
				throw new Exception("New sheet name cannot be null or blank");
			}

			if (sourceSheetId == null)
			{
				throw new Exception("Source sheet ID cannot be null or blank");
			}

			if (destinationId == null)
			{
				throw new Exception("Destination ID cannot be null or blank");
			}

			var response = new ResultResponse<Sheet>();

			ContainerDestination container = new ContainerDestination()
			{
				DestinationId = destinationId.Value,
				DestinationType = destinationType.ToString(),
				NewName = newName
			};

			string includeString = "";

			if (includes != null && includes.Count() > 0)
			{
				includeString += string.Format("?include={0}", string.Join(",", includes.Select(i => i.ToString())));
			}

			response = await this.ExecuteRequest<ResultResponse<Sheet>, ContainerDestination>(HttpVerb.POST, string.Format("sheets/{0}/copy{1}", sourceSheetId, includeString), container, accessToken: accessToken);

			response.Result._Client = this;

			return response.Result;
		}

		public async Task<Sheet> GetSheetById(long? sheetId, string accessToken = null)
		{
			if (sheetId == null)
			{
				throw new Exception("Sheet ID cannot be null");
			}

			var response = await this.ExecuteRequest<Sheet, Sheet>(HttpVerb.GET, string.Format("sheets/{0}", sheetId), null, accessToken: accessToken);

			response._Client = this;

			return response;
		}

		public async Task<IEnumerable<Sheet>> GetSheetsForWorkspace(long? workspaceId, string accessToken = null)
		{
			if (workspaceId == null)
			{
				throw new Exception("Workspace ID cannot be null");
			}

			var response = await this.ExecuteRequest<Workspace, Workspace>(HttpVerb.GET, string.Format("workspaces/{0}", workspaceId), null, accessToken: accessToken);

			response.Sheets.FirstOrDefault()._Client = this;

			return response.Sheets;
		}
		#endregion

		//
		//  Rows
		#region Rows
		public async Task<IEnumerable<Row>> CreateRows(long? sheetId, IEnumerable<Row> rows, bool? toTop = null, bool? toBottom = null, long? parentId = null, long? siblingId = null, string accessToken = null)
		{
			if (sheetId == null)
			{
				throw new Exception("Sheet ID cannot be null");
			}

			if (rows.Count() > 1)
			{
				foreach (var row in rows)
				{
					row.ToTop = toTop;
					row.ToBottom = toBottom;
					row.ParentId = parentId;
					row.SiblingId = siblingId;

					foreach (var cell in row.Cells)
					{
						cell.Build();
					}
				}
			}

			var response = await this.ExecuteRequest<ResultResponse<IEnumerable<Row>>, IEnumerable<Row>>(HttpVerb.POST, string.Format("sheets/{0}/rows", sheetId), rows, accessToken: accessToken);

			return response.Result;
		}

		public async Task<CopyOrMoveRowResult> MoveRows(long? sourceSheetId, long? destinationSheetId, IEnumerable<long> rowIds, string accessToken = null)
		{
			if (sourceSheetId == null)
			{
				throw new Exception("Source Sheet ID cannot be null");
			}

			if (destinationSheetId == null)
			{
				throw new Exception("Destination Sheet ID cannot be null");
			}

			var copyOrMoveRowDirective = new CopyOrMoveRowDirective()
			{
				To = new CopyOrMoveRowDestination()
				{
					SheetId = destinationSheetId
				},
				RowIds = rowIds.ToList()
			};

			var response = await this.ExecuteRequest<CopyOrMoveRowResult, CopyOrMoveRowDirective>(HttpVerb.POST, string.Format("sheets/{0}/rows/move?include=attachments,discussions", sourceSheetId), copyOrMoveRowDirective, accessToken: accessToken);

			return response;
		}

		public async Task<CopyOrMoveRowResult> CopyRows(long? sourceSheetId, long? destinationSheetId, IEnumerable<long> rowIds, string accessToken = null)
		{
			if (sourceSheetId == null)
			{
				throw new Exception("Source Sheet ID cannot be null");
			}

			if (destinationSheetId == null)
			{
				throw new Exception("Destination Sheet ID cannot be null");
			}

			var copyOrMoveRowDirective = new CopyOrMoveRowDirective()
			{
				To = new CopyOrMoveRowDestination()
				{
					SheetId = destinationSheetId
				},
				RowIds = rowIds.ToList()
			};

			var response = await this.ExecuteRequest<CopyOrMoveRowResult, CopyOrMoveRowDirective>(HttpVerb.POST, string.Format("sheets/{0}/rows/copy?include=attachments,discussions", sourceSheetId), copyOrMoveRowDirective, accessToken: accessToken);

			return response;
		}
		#endregion

		//
		//  Folders
		#region Folders

		public async Task<IEnumerable<ISmartsheetObject>> GetFoldersForWorkspace(long? workspaceId, string accessToken = null)
		{
			if (workspaceId == null)
			{
				throw new Exception("Workspace ID cannot be null");
			}

			var response = await this.ExecuteRequest<Workspace, Workspace>(HttpVerb.GET, string.Format("workspaces/{0}", workspaceId), null, accessToken: accessToken);

			return response.Folders;
		}

		public async Task<Folder> GetFolderById(long? folderId, string accessToken = null)
		{
			if (folderId == null)
			{
				throw new Exception("Folder ID cannot be null");
			}

			var response = await this.ExecuteRequest<Folder, Folder>(HttpVerb.GET, string.Format("folders/{0}", folderId), null, accessToken: accessToken);

			return response;
		}

		public async Task<Folder> CopyFolder(long? folderId, long? destinationId, string newName, string accessToken = null)
		{
			if (folderId == null)
			{
				throw new Exception("Folder ID cannot be null");
			}

			var containerDestinationObject = new ContainerDestinationObject()
			{
				DestinationId = destinationId.Value,
				DestinationType = "folder",
				NewName = newName
			};

			var response = await this.ExecuteRequest<ResultResponse<Folder>, ContainerDestinationObject>(HttpVerb.POST, string.Format("folders/{0}/copy?include=data", folderId), containerDestinationObject, accessToken: accessToken);

			return response.Result;
		}
		#endregion

		//
		//  Reports
		#region Reports
		public async Task<IEnumerable<ISmartsheetObject>> GetReportsForWorkspace(long? workspaceId, string accessToken = null)
		{
			if (workspaceId == null)
			{
				throw new Exception("Workspace ID cannot be null");
			}

			var response = await this.ExecuteRequest<Workspace, Workspace>(HttpVerb.GET, string.Format("workspaces/{0}", workspaceId), null, accessToken: accessToken);

			return response.Reports;
		}
		#endregion

		//
		//  Templates
		#region Templates
		public async Task<IEnumerable<ISmartsheetObject>> GetTemplatesForWorkspace(long? workspaceId, string accessToken = null)
		{
			if (workspaceId == null)
			{
				throw new Exception("Workspace ID cannot be null");
			}

			var response = await this.ExecuteRequest<Workspace, Workspace>(HttpVerb.GET, string.Format("workspaces/{0}", workspaceId), null, accessToken: accessToken);

			return response.Templates;
		}
		#endregion

		//
		//  Update Requests
		#region Update Requests
		public async Task<UpdateRequest> CreateUpdateRequest(long? sheetId, IEnumerable<long> rowIds, IEnumerable<Recipient> sendTo, IEnumerable<long> columnIds, string subject = null, string message = null, bool ccMe = false, bool includeDiscussions = true, bool includeAttachments = true, string accessToken = null)
		{
			if (sheetId == null)
			{
				throw new Exception("Sheet ID cannot be null");
			}

			if (rowIds.Count() == 0)
			{
				throw new Exception("Must specifiy 1 or more rows to update");
			}

			if (sendTo.Count() == 0)
			{
				throw new Exception("Must specifiy 1 or more recipients");
			}

			var request = new UpdateRequest()
			{
				SendTo = sendTo.ToList(),
				Subject = subject,
				Message = message,
				CcMe = ccMe,
				RowIds = rowIds.ToList(),
				ColumnIds = columnIds.ToList(),
				IncludeAttachments = includeAttachments,
				IncludeDiscussions = includeDiscussions
			};

			var result = await this.ExecuteRequest<ResultResponse<UpdateRequest>, UpdateRequest>(HttpVerb.POST, string.Format("sheets/{0}/updaterequests", sheetId), request, accessToken: accessToken);

			return result.Result;
		}
		#endregion


		//
		//	Webhooks
		#region
		public async Task<IEnumerable<Webhook>> GetWebhooksForUser(string accessToken = null)
		{
			var result = await this.ExecuteRequest<IndexResultResponse<Webhook>, Webhook>(HttpVerb.GET, "webhooks", null, accessToken: accessToken);

			return result.Data;
		}

		public async Task<Webhook> GetWebhook(long? webhookId, string accessToken = null)
		{
			if (webhookId == null)
			{
				throw new Exception("Webhook ID cannot be null");
			}

			var result = await this.ExecuteRequest<Webhook, Webhook>(HttpVerb.GET, string.Format("webhooks/{0}", webhookId), null, accessToken: accessToken);

			return result;
		}

		public async Task<Webhook> CreateWebhook(Webhook model, string accessToken = null)
		{
			var result = await this.ExecuteRequest<ResultResponse<Webhook>, Webhook>(HttpVerb.POST, "webhooks", model, accessToken: accessToken);

			return result.Result;
		}

		public async Task<Webhook> UpdateWebhook(long? webhookId, Webhook model, string accessToken = null)
		{
			var result = await this.ExecuteRequest<ResultResponse<Webhook>, Webhook>(HttpVerb.PUT, string.Format("webhooks/{0}", webhookId), model, accessToken: accessToken);

			return result.Result;
		}
		#endregion

		//
		//	Columns
		#region Columns
		public async Task<Column> EditColumn(long? sheetId, long? columnId, Column model, string accessToken = null)
		{
			if (columnId == null)
			{
				throw new Exception("Column ID cannot be null");
			}

			var result = await this.ExecuteRequest<ResultResponse<Column>, Column>(HttpVerb.PUT, string.Format("sheets/{0}/columns/{1}", sheetId, columnId), model, accessToken: accessToken);

			return result.Result;
		}
		#endregion

		#region Attachments
		public async Task<Attachment> UploadAttachmentToRow(long? sheetId, long? rowId, string fileName, long length, Stream stream, string contentType = null, string accessToken = null)
		{
			this._HttpClient.DefaultRequestHeaders.Remove("Authorization");
			this.SetAuthorizationHeader(accessToken);

			var url = string.Format("https://api.smartsheet.com/2.0/sheets/{0}/rows/{1}/attachments", sheetId, rowId);

			byte[] data;
			using (var br = new BinaryReader(stream))
			{
				data = br.ReadBytes((int)stream.Length);
			}

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Content = new ByteArrayContent(data);
			request.Content.Headers.ContentLength = length;
			request.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
			{
				FileName = fileName
			};
			if (contentType != null)
			{
				request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
			}

			var response = await this._HttpClient.SendAsync(request);
			var responseBody = await response.Content.ReadAsStringAsync();
			var jsonResponseBody = JsonConvert.DeserializeObject(responseBody).ToString();
			var resultResponse = JsonConvert.DeserializeObject<Attachment>(jsonResponseBody);
			return resultResponse;
		}

		public async Task<Attachment> UploadAttachmentToRow(long? sheetId, long? rowId, IFormFile formFile, string accessToken = null)
		{
			using (var stream = formFile.OpenReadStream())
			{
				var response = await this.UploadAttachmentToRow(sheetId, rowId, formFile.FileName, formFile.Length, stream, formFile.ContentType, accessToken);
				return response;
			}

		}
		#endregion
	}
}
