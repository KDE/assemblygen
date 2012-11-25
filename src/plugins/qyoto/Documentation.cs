/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009, 2012 Arno Rehn <arno@arnorehn.de>, Dimitar Dobrev <dpldobrev@yahoo.com>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;
using zlib;

public class Documentation
{
	public static IDictionary<string, string> Get(string docsPath)
	{
		if (!Directory.Exists(docsPath))
		{
			return new Dictionary<string, string>();
		}
		try
		{
			IDictionary<string, string> documentation = GetFromHtml(docsPath);
			return documentation.Count > 0 ? documentation : GetFromQch(docsPath);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("Documentation generation failed: {0}", ex.Message);
			return new Dictionary<string, string>();
		}
	}

	private static IDictionary<string, string> GetFromHtml(string docsPath)
	{
		string docs = Path.Combine(docsPath, "html");
		if (!Directory.Exists(docs))
		{
			return new Dictionary<string, string>();
		}
		return Directory.GetFiles(docs, "*.html").ToDictionary(Path.GetFileName,
															   f => File.ReadAllText(f).Replace("\r", string.Empty).Replace(@"\", @"\\"));
	}

	private static IDictionary<string, string> GetFromQch(string docsPath)
	{
		string dataSource = Path.Combine(docsPath, "qch", "qt.qch");
		if (!File.Exists(dataSource))
		{
			return new Dictionary<string, string>();
		}
		SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder();
		sqliteConnectionStringBuilder.DataSource = dataSource;
		using (SqliteConnection sqliteConnection = new SqliteConnection(sqliteConnectionStringBuilder.ConnectionString))
		{
			sqliteConnection.Open();
			using (SqliteCommand sqliteCommand = new SqliteCommand(
				"SELECT Name, Data FROM FileNameTable INNER JOIN FileDataTable ON FileNameTable.FileId = FileDataTable.Id " +
				"WHERE Name LIKE '%.html' " +
				"ORDER BY Name", sqliteConnection))
			{
				using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
				{
					Dictionary<string, string> documentation = new Dictionary<string, string>();
					while (sqliteDataReader.Read())
					{
						byte[] blob = new byte[ushort.MaxValue];
						int length = (int) sqliteDataReader.GetBytes(1, 0, blob, 0, blob.Length);
						using (MemoryStream output = new MemoryStream(length - 4))
						{
							using (ZOutputStream zOutputStream = new ZOutputStream(output))
							{
								zOutputStream.Write(blob, 4, length - 4);
								zOutputStream.Flush();
								documentation.Add(sqliteDataReader.GetString(0), Encoding.UTF8.GetString(output.ToArray()).Replace(@"\", @"\\"));
							}
						}
					}
					return documentation;
				}
			}
		}
	}
}
