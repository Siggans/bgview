using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using BingGalleryViewer.Utility.Bing;
using System.Diagnostics;
namespace BingGalleryViewer.Utility.SQLite
{
	/// <summary>
	/// BGV's SQLite database access layer.  Validate, save, and retrieve data to and from sqlite database.
	/// </summary>
	internal partial class Datastore : IDisposable
	{

		/// <summary>
		/// Location to the sqlite database
		/// </summary>
		private static string FilePath { get { return Setting.GetCurrentSetting().DatastoreFilePath.LocalPath; } }

		/// <summary>
		/// Create a new sqlite database file if none exist before
		/// </summary>
		public static void PrepareDatabaseIfNotExist()
		{
			PrepareDatabaseIfNotExist(FilePath);
		}

		/// <summary>
		/// Create a new sqlite database file if none exist before
		/// </summary>
		/// <param name="filepath">file path to the file</param>
		public static void PrepareDatabaseIfNotExist(string filepath)
		{

			try
			{
				// check if file exists, if not, create the databasefile
				if (!File.Exists(filepath)) SQLiteConnection.CreateFile(FilePath);

				var builder = new SQLiteConnectionStringBuilder()
				{
					FailIfMissing = true,
					DataSource = filepath,
					Version = 3,
					UseUTF16Encoding = true,
				};

				// connect to the database file and initialize table.
				using (var conn = new SQLiteConnection(builder.ToString()))
				{
					conn.Open();
					using (var transaction = conn.BeginTransaction())
					using (var cmd = conn.CreateCommand())
					{
						try
						{
							// create table if none exists
							cmd.CommandText = TableBingImage.GetTableCreationString();
							cmd.ExecuteNonQuery();
							transaction.Commit();
						}
						catch (Exception e)
						{
							transaction.Rollback();
							throw new ApplicationException("Cannot create tables in database at " + FilePath, e);
						}
					}
					conn.Close();
				}
			}
			catch (ApplicationException e) { throw e; }
			catch (Exception e) { throw new ApplicationException("Cannot create database at " + FilePath, e); }


		}

		// helper metho for sql param name passing
		private static string param(string name) { return "@" + name; }

		/// <summary>
		/// database that this instance of datastore is associated with
		/// </summary>
		public readonly string StorageLocation;
		/// <summary>
		/// if this instance of the datastore allows writing/saving to database
		/// </summary>
		public readonly bool IsWriteable;

		// database connection
		private SQLiteConnection _connection = null;

		/// <summary>
		/// Check if the connection is opened with the database
		/// </summary>
		public bool IsConnected { get { return _connection != null; } }

		/// <summary>
		/// Construtor
		/// </summary>
		/// <param name="canWrite">set to true to write to database</param>
		public Datastore(bool canWrite = false) : this(FilePath, canWrite) { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filePath">database file location</param>
		/// <param name="canWrite">set to true to write to database</param>
		public Datastore(string filePath, bool canWrite = false)
		{
			if (!File.Exists(filePath)) throw new ArgumentException("File must exist");
			this.StorageLocation = filePath;
			this.IsWriteable = canWrite;
		}

		/// <summary>
		/// Initialize datastore to connect to database
		/// </summary>
		/// <returns>awaitable task</returns>
		private async Task InitializeAsync()
		{
			if (_connection == null)
			{
				// connect to datastore
				var builder = new SQLiteConnectionStringBuilder()
				{
					FailIfMissing = true,
					ReadOnly = !this.IsWriteable,
					DataSource = this.StorageLocation,
					Version = 3,
					UseUTF16Encoding = true,
				};
				_connection = new SQLiteConnection(builder.ToString());
				await _connection.OpenAsync();
			}
		}

		// sql cmd
		private static readonly string SaveDatesCmdTxt =
			string.Format("INSERT OR REPLACE INTO {0} VALUES ( {1}, {2}, {3}, {4}, {5});",
				TableBingImage.TableDescription,
				param(TableBingImage.IdName),
				param(TableBingImage.UrlName),
				param(TableBingImage.UrlBaseName),
				param(TableBingImage.CopyrightName),
				param(TableBingImage.CopyrightLinkName));

		/// <summary>
		/// Save a range of image infos to datastore
		/// </summary>
		/// <param name="infos"></param>
		/// <returns></returns>
		public async Task<bool> SaveDatesAsync(BingImageInfo[] infos)
		{
			if (infos == null || infos.Length == 0) return false;
			if (!IsWriteable) throw new InvalidOperationException("Datastore not open for write");
			await InitializeAsync(); // ensure connection is open

			try
			{
				using (var transaction = _connection.BeginTransaction())
				using (var cmd = _connection.CreateCommand())
				{
					try
					{
						cmd.CommandText = SaveDatesCmdTxt;
						cmd.Prepare();
						foreach (var info in infos)
						{
							cmd.Parameters.Clear();
							int id;
							if (!BingDataHelper.TryConvertStartdate(info.StartDate, out id)) throw new ArgumentException("Invalid date");
							cmd.Parameters.Add(new SQLiteParameter(param(TableBingImage.IdName), id));
							cmd.Parameters.Add(new SQLiteParameter(param(TableBingImage.UrlName), info.Url));
							cmd.Parameters.Add(new SQLiteParameter(param(TableBingImage.UrlBaseName), info.UrlBase));
							cmd.Parameters.Add(new SQLiteParameter(param(TableBingImage.CopyrightName), info.Copyright));
							cmd.Parameters.Add(new SQLiteParameter(param(TableBingImage.CopyrightLinkName), info.CopyrightLink));
							await cmd.ExecuteNonQueryAsync();
						}
						transaction.Commit();
						return true;
					}
					catch
					{
						transaction.Rollback();
					}
				}
			}
			catch (NullReferenceException e) { throw e; }
			catch { Trace.WriteLine("Exception occured at Datastore.SaveDatesAsync. " ); }
			return false;

		}

		private static readonly string ReadImageInfoCmdTxt =
			string.Format("SELECT * FROM {0} WHERE ROWID = @param1 ;",
				TableBingImage.TableName);
		public async Task<BingImageInfo> ReadImageInfoAsync(DateTime date)
		{
			int intDate;
			if (!BingDataHelper.TryConvertStartdate(date, out intDate)) return null;
			await InitializeAsync();
			BingImageInfo info = null;
			try
			{
				using (var cmd = _connection.CreateCommand())
				{
					cmd.CommandText = ReadImageInfoCmdTxt;
					cmd.Parameters.Add(new SQLiteParameter("@param1", intDate));
					var reader = await cmd.ExecuteReaderAsync();
					if (reader.Read())
					{
						info = new BingImageInfo()
						{
							StartDate = reader.GetInt32(0).ToString(),
							Url = reader.GetString(1),
							UrlBase = reader.GetString(2),
							Copyright = reader.GetString(3),
							CopyrightLink = reader.GetString(4),
						};
					}
				}
			}
			catch (NullReferenceException e) { throw e; }
			catch { Trace.WriteLine("Exception occured at Datastore.SaveDatesAsync. "); }
			return info;
		}

		private static readonly string ReadImageInfosCmdTxt =
			string.Format("SELECT * FROM {0} WHERE ROWID >= @param1 AND ROWID <=@param2 ORDER BY ROWID ASC ;",
		TableBingImage.TableName);
		public async Task<BingImageInfo[]> ReadImageInfosAsync(DateTime date1, DateTime date2)
		{
			int intDate1, intDate2;
			if (!BingDataHelper.TryConvertStartdate(date1, out intDate1) || !BingDataHelper.TryConvertStartdate(date2, out intDate2)) return new BingImageInfo[0];
			await InitializeAsync();
			var infos = new List<BingImageInfo>();
			try
			{
				if (intDate1 > intDate2) { var tmp = intDate1; intDate1 = intDate2; intDate2 = tmp; }
				using (var cmd = _connection.CreateCommand())
				{
					cmd.CommandText = ReadImageInfosCmdTxt;
					cmd.Parameters.Add(new SQLiteParameter("@param1", intDate1));
					cmd.Parameters.Add(new SQLiteParameter("@param2", intDate2));
					var reader = await cmd.ExecuteReaderAsync();
					while (await reader.ReadAsync())
					{
						infos.Add(new BingImageInfo()
						{
							StartDate = reader.GetInt32(0).ToString(),
							Url = reader.GetString(1),
							UrlBase = reader.GetString(2),
							Copyright = reader.GetString(3),
							CopyrightLink = reader.GetString(4),
						});
					}

				}
			}
			catch (NullReferenceException e) { throw e; }
			catch { Trace.WriteLine("Exception occured at Datastore.SaveDatesAsync. "); }
			return infos.ToArray();
		}

		private static readonly string ReadImageInfoLastStartdateCmdTxt =
			string.Format("SELECT ROWID FROM {0} ORDER BY ROWID DESC LIMIT 1 ;",
				TableBingImage.TableName);
		public async Task<int?> ReadImageInfoLastStartdate()
		{
			await InitializeAsync();
			try
			{
				using (var cmd = _connection.CreateCommand())
				{
					cmd.CommandText = ReadImageInfoLastStartdateCmdTxt;
					var reader = await cmd.ExecuteReaderAsync();
					if (reader.Read())
					{
						var result = reader.GetInt32(0);
						if (result > 10) return result;
					}
				}
			}
			catch (NullReferenceException e) { throw e; }
			catch { Trace.WriteLine("Exception occured at Datastore.SaveDatesAsync. "); }
			return null;

		}

		private static readonly string ReadImageInfoFirstStartdateCmdTxt =
			string.Format("SELECT ROWID FROM {0} ORDER BY ROWID ASC LIMIT 1 ;",
				TableBingImage.TableName);
		public async Task<int?> ReadImageInfoFirstStartdate()
		{
			await InitializeAsync();
			try
			{
				using (var cmd = _connection.CreateCommand())
				{
					cmd.CommandText = ReadImageInfoFirstStartdateCmdTxt;
					var reader = await cmd.ExecuteReaderAsync();
					if (reader.Read())
					{
						var result = reader.GetInt32(0);
						if (result > 10) return result;
					}
				}
			}
			catch (NullReferenceException e) { throw e; }
			catch { Trace.WriteLine("Exception occured at Datastore.SaveDatesAsync. "); }
			return null;

		}

		private bool _disposed = false;
		protected void Dispose(bool disposing)
		{
			if (_disposed || !disposing) return;
			if (_connection != null)
			{
				_connection.Close();
				_connection.Dispose();
			}
			_disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	/// Schema for database.
	/// </summary>
	internal partial class Datastore
	{
		/// <summary>
		/// table schema
		/// </summary>
		internal class TableBingImage
		{
			/// <summary>
			/// table name
			/// </summary>
			public const string TableName = "table_bgv";
			/// <summary>
			/// column name
			/// </summary>
			public const string IdName = "startdate";
			/// <summary>
			/// column name
			/// </summary>
			public const string UrlName = "url";
			/// <summary>
			/// column name
			/// </summary>
			public const string UrlBaseName = "urlbase";
			/// <summary>
			/// column name
			/// </summary>
			public const string CopyrightName = "copyright";
			/// <summary>
			/// column name
			/// </summary>
			public const string CopyrightLinkName = "copyrightLink";

			/// <summary>
			/// SQL table description
			/// </summary>
			public static readonly string TableDescription = string.Format("{0} ({1}, {2}, {3}, {4}, {5})", TableName, IdName, UrlName, UrlBaseName, CopyrightName, CopyrightLinkName);

			/// <summary>
			/// get sql string for creating the sql table
			/// </summary>
			/// <returns>sql command string</returns>
			public static string GetTableCreationString()
			{
				return "CREATE TABLE IF NOT EXISTS " + TableName + " ("
					+ IdName + " INTEGER PRIMARY KEY, "
					+ UrlName + " VARCHAR(1000) NOT NULL, "
					+ UrlBaseName + " VARCHAR(1000) NOT NULL, "
					+ CopyrightName + " VARCHAR(1000) NOT NULL, "
					+ CopyrightLinkName + " VARCHAR(1000) NOT NULL);";
			}
		}
	}
}
