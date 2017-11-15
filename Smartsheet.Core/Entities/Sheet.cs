using ProfessionalServices.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Smartsheet.Core.Http;
using System.Threading.Tasks;
using ProfessionalServices.Core.Responses;

namespace Smartsheet.Core.Entities
{
	public class Sheet : SmartsheetObject
	{
		public Sheet()
		{

		}

		public Sheet(long? id)
		{
			this.Id = id;
		}

		public Sheet(string sheetName)
		{
			this.Name = sheetName;
		}

		public Sheet(long? id, string sheetName)
		{
			this.Id = id;
			this.Name = sheetName;
		}

		public Sheet(string sheetName, IList<Column> columns)
		{
			this.Name = sheetName;
			this.Columns = columns;
		}

		public Sheet(SmartsheetHttpClient client, string sheetName = "", IList<Column> columns = null) : base(client)
		{
			this.EffectiveAttachmentOptions = new List<string>();
			this.Columns = new List<Column>();
			this.Rows = new List<Row>();
		}

		public long? Id { get; set; }
		public long? Version { get; set; }
		public long? OwnerId { get; set; }
		public long? FromId { get; set; }

		public int? TotalRowCount { get; set; }

		public string Name { get; set; }
		public string AccessLevel { get; set; }
		public string Permalink { get; set; }
		public string Owner { get; set; }

		public DateTime? CreatedAt { get; set; }
		public DateTime? ModifiedAt { get; set; }

		public bool? ReadOnly { get; set; }
		public bool? GantEnabled { get; set; }
		public bool? DependenciesEnabled { get; set; }
		public bool? ResourceManagementEnabled { get; set; }
		public bool? CellImageUploadEnabled { get; set; }
		public bool? Favorite { get; set; }
		public bool? ShowParentRowsForFilters { get; set; }

		public UserSettings UserSettings { get; set; }
		public Workspace Workspace { get; set; }

		public IList<string> EffectiveAttachmentOptions { get; set; }
		public IList<Column> Columns { get; set; }
		public IList<Row> Rows { get { return this.MapCellsToColumns(); } set { this.UnformattedRows = value; } }
		private IList<Row> UnformattedRows { get; set; }

		//
		//  Extension Methods
		#region Extensions
		private IList<Row> MapCellsToColumns()
		{
			if (this.UnformattedRows != null)
			{
				foreach (var row in this.UnformattedRows)
				{
					var parsedColumns = this.Columns.ToList();
					var parsedCells = row.Cells.ToList();

					for (var i = 0; i < parsedColumns.Count; i++)
					{
						var cell = parsedCells[i];

						cell.ColumnId = parsedColumns[i].Id;
						cell.Column = parsedColumns[i];
					}
				}
			}

			return this.UnformattedRows;
		}

		public Column GetColumnById(long columnId)
		{
			var column = this.Columns.Where(c => c.Id == columnId).FirstOrDefault();

			return column;
		}

		public Column GetColumnByTitle(string columnTitle, bool caseSensitive = false)
		{
			var column = new Column();

			if (caseSensitive)
			{
				column = this.Columns.FirstOrDefault(c => c.Title.Equals(columnTitle));
			}
			else
			{
				column = this.Columns.FirstOrDefault(c => c.Title.ToLower().Equals(columnTitle.ToLower()));
			}

			return column;
		}

		#endregion

		//
		//  Client Methods
		#region SmartsheetHttpClient
		public async Task<IEnumerable<Row>> CreateRows(IList<Row> rows, bool? toTop = null, bool? toBottom = null, long? parentId = null, long? siblingId = null)
		{
			if (rows.Count() > 0)
			{
				for (var i = 0; i < rows.Count(); i++)
				{
					foreach (var cell in rows[i].Cells)
					{
						cell.Build();
					}

					rows[i].ToTop = toTop;
					rows[i].ToBottom = toBottom;
					rows[i].ParentId = parentId;
					rows[i].SiblingId = siblingId;
				}
			}

			var response = await this._Client.ExecuteRequest<ResultResponse<IEnumerable<Row>>, IEnumerable<Row>>(HttpVerb.POST, string.Format("sheets/{0}/rows", this.Id), rows);

			return response.Result;
		}

		public async Task<IEnumerable<Row>> UpdateRows(IList<Row> rows, bool? strict = false, bool? toTop = null, bool? toBottom = null, bool? above = null, long? parentId = null, long? siblingId = null)
		{
			if (rows.Count() > 0)
			{
				var systemColumns = this.Columns.Where(c => c.SystemColumnType != null).Select(c => c.Id).ToList();

				for (var i = 0; i < rows.Count(); i++)
				{
					var removeCells = new List<Cell>();

					for (var x = 0; x < rows[i].Cells.Count(); x++)
					{
						rows[i].Cells[x].Build(strict);

						if (rows[i].Cells[x].Value == null || systemColumns.Contains(rows[i].Cells[x].ColumnId))
						{
							removeCells.Add(rows[i].Cells[x]);
						}
					}

					rows[i].Cells = rows[i].Cells.Except(removeCells).ToList();

					rows[i].Build(
						preserveId: true,
						strict: strict,
						toTop: toTop,
						toBottom: toBottom,
						above: above,
						parentId: parentId,
						siblingId: siblingId);
				}
			}

			var response = await this._Client.ExecuteRequest<ResultResponse<IEnumerable<Row>>, IEnumerable<Row>>(HttpVerb.PUT, string.Format("sheets/{0}/rows", this.Id), rows);

			return response.Result;
		}

		public async Task<IEnumerable<long>> RemoveRows(IList<Row> rows)
		{
			var rowList = rows.ToList();

			var response = new ResultResponse<IEnumerable<long>>();

			while (rowList.Count > 0)
			{
				var rowIdList = string.Join(",", rowList.Take(300).Select(r => Convert.ToString(r.Id)));

				var url = string.Format("sheets/{0}/rows?ids={1}&ignoreRowsNotFound=true", this.Id, rowIdList);

				response = await this._Client.ExecuteRequest<ResultResponse<IEnumerable<long>>, IEnumerable<Row>>(HttpVerb.DELETE, string.Format("sheets/{0}/rows?ids={1}&ignoreRowsNotFound=true", this.Id, rowIdList), null);

				if (response.Message.Equals("SUCCESS"))
				{
					rowList.RemoveAll(r => rowIdList.Contains(Convert.ToString(r.Id)));
				}
			}

			return response.Result;
		}
		#endregion
	}
}
