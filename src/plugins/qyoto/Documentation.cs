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
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Util;
using Mono.Data.Sqlite;
using zlib;

public unsafe class Documentation
{
	private readonly GeneratorData data;
	private readonly Translator translator;
	private readonly Dictionary<CodeTypeDeclaration, List<string>> memberDocumentation = new Dictionary<CodeTypeDeclaration, List<string>>();
	private readonly List<string> staticDocumentation = new List<string>();

	public Documentation(GeneratorData data, Translator translator)
	{
		this.data = data;
		this.translator = translator;
		this.GatherDocs();
	}

	public void DocumentEnumMember(Smoke* smoke, Smoke.Method* smokeMethod, CodeMemberField cmm, CodeTypeDeclaration type)
	{
		CodeTypeDeclaration parentType = this.memberDocumentation.Keys.FirstOrDefault(t => t.Name == (string) type.UserData["parent"]);
		if (parentType != null)
		{
			IList<string> docs = this.memberDocumentation[parentType];
			string typeName = Regex.Escape(parentType.Name) + "::" + Regex.Escape(type.Name);
			if (type.Comments.Count == 0)
			{
				for (int i = 0; i < docs.Count; i++)
				{
					const string enumDoc = @"enum {0}(\s*flags {1}::\w+\s+)?(?<docsStart>.*?)(\n){{3}}";
					Match matchEnum = Regex.Match(docs[i], string.Format(enumDoc, typeName, parentType.Name),
												  RegexOptions.Singleline | RegexOptions.ExplicitCapture);
					if (matchEnum.Success)
					{
						string doc = (matchEnum.Groups["docsStart"].Value + matchEnum.Groups["docsEnd1"].Value).Trim();
						doc = Regex.Replace(doc,
											@"(The \S+ type is a typedef for QFlags<\S+>\. It stores an OR combination of \S+ values\.)",
											string.Empty);
						doc = Regex.Replace(doc,
											@"ConstantValue(Description)?.*?(((\n){2})|$)",
											string.Empty, RegexOptions.Singleline | RegexOptions.ExplicitCapture).Trim();
						if (!string.IsNullOrEmpty(doc))
						{
							FormatComment(doc, type, i > 0);
							break;
						}
					}
				}
			}
			string memberName = Regex.Escape(parentType.Name) + "::" +
								Regex.Escape(ByteArrayManager.GetString(smoke->methodNames[smokeMethod->name]));
			const string memberDoc = @"enum {0}.*{1}\t[^\t\n]+\t(?<docs>.*?)(&\w+;)?(\n)";
			for (int i = 0; i < docs.Count; i++)
			{
				Match match = Regex.Match(docs[i], string.Format(memberDoc, typeName, memberName),
										  RegexOptions.Singleline | RegexOptions.ExplicitCapture);
				if (match.Success)
				{
					string doc = match.Groups["docs"].Value.Trim();
					if (!string.IsNullOrEmpty(doc))
					{
						FormatComment(Char.ToUpper(doc[0]) + doc.Substring(1), cmm, i > 0);
						break;
					}
				}
			}
		}
	}

	public void DocumentProperty(CodeTypeDeclaration type, string propertyName, string propertyType, CodeMemberProperty cmp)
	{
		if (this.memberDocumentation.ContainsKey(type))
		{
			IList<string> docs = this.memberDocumentation[type];
			for (int i = 0; i < docs.Count; i++)
			{
				Match match = Regex.Match(docs[i],
										  propertyName + " : (const )?" + propertyType +
										  @"\n(?<docs>This.*?)\nAccess functions:",
										  RegexOptions.Singleline | RegexOptions.ExplicitCapture);
				if (match.Success)
				{
					FormatComment(match.Groups["docs"].Value, cmp, i > 0);
					break;
				}
			}
		}
	}

	public void DocumentAttributeProperty(CodeTypeMember cmm, CodeTypeDeclaration type)
	{
		if (this.memberDocumentation.ContainsKey(type))
		{
			IList<string> docs = this.memberDocumentation[type];
			string typeName = Regex.Escape(type.Name);
			string originalName = Char.ToLowerInvariant(cmm.Name[0]) + cmm.Name.Substring(1);
			const string memberDoc = @"{0}::{1}\n\W*(?<docs>.*?)(\n\s*){{3}}";
			for (int i = 0; i < docs.Count; i++)
			{
				Match match = Regex.Match(docs[i], string.Format(memberDoc, typeName, originalName),
										  RegexOptions.Singleline | RegexOptions.ExplicitCapture);
				if (match.Success)
				{
					FormatComment(match.Groups["docs"].Value, cmm, i > 0);
					break;
				}
			}
		}
	}

	public void DocumentMember(Smoke* smoke, Smoke.Method* smokeMethod, CodeTypeMember cmm, CodeTypeDeclaration type)
	{
		if (type.Name == this.data.GlobalSpaceClassName || this.translator.NamespacesAsClasses.Contains(type.Name))
		{
			this.DocumentMember(smoke, smokeMethod, cmm, @"\w+", this.staticDocumentation, false);
		}
		else
		{
			if (this.memberDocumentation.ContainsKey(type))
			{
				this.DocumentMember(smoke, smokeMethod, cmm, type.Name, this.memberDocumentation[type]);
			}
		}
	}

	private void DocumentMember(Smoke* smoke, Smoke.Method* smokeMethod, CodeTypeMember cmm, string type, IEnumerable<string> docs, bool markObsolete = true)
	{
		string methodName = Regex.Escape(ByteArrayManager.GetString(smoke->methodNames[smokeMethod->name]));
		string[] argTypes = smoke->GetArgs(smokeMethod).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
		docs.Where((t, i) => this.TryMatch(type, methodName, cmm, t, i > 0 && markObsolete, argTypes)).Any();
	}

	private bool TryMatch(string type, string methodName, CodeTypeMember cmm, string docs, bool markObsolete, IEnumerable<string> argTypes)
	{
		const string memberDoc = @"(^|( --)|\n)\n([\w :*&<>,]+)?(({0}(\s*&)?::)| ){1}(const)?( \[(\w+\s*)+\])?\n\W*(?<docs>.*?)(\n\s*){{1,2}}((&?\S* --)|((\n\s*){{2}}))";
		const string separator = @",\s*";
		StringBuilder signatureRegex = new StringBuilder(methodName).Append(@"\s*\(\s*(");
		bool anyArgs = false;
		foreach (string argType in argTypes)
		{
			if (!anyArgs)
			{
				signatureRegex.Append("?<args>");
				anyArgs = true;
			}
			signatureRegex.Append(this.GetTypeRegex(argType)).Append(@"(\s+\w+(\s*=\s*[^,\r\n]+(\(\s*\))?)?)?");
			signatureRegex.Append(separator);
		}
		if (anyArgs)
		{
			signatureRegex.Insert(signatureRegex.Length - separator.Length, '(');
		}
		else
		{
			signatureRegex.Append('(');
		}
		signatureRegex.Append(@"[\w :*&<>]+\s*=\s*[^,\r\n]+(\(\s*\))?(,\s*)?)*)\s*\)\s*");
		Match match = Regex.Match(docs, string.Format(memberDoc, type, signatureRegex),
								  RegexOptions.Singleline | RegexOptions.ExplicitCapture);
		if (match.Success)
		{
			FormatComment(match.Groups["docs"].Value, cmm, markObsolete);
			FillMissingParameterNames(cmm, match.Groups["args"].Value);
			return true;
		}
		return false;
	}

	private string GetTypeRegex(string argType)
	{
		StringBuilder typeBuilder = new StringBuilder(argType);
		this.FormatType(typeBuilder);
		typeBuilder.Insert(0, "((");
		typeBuilder.Append(')');
		if (this.data.TypeDefsPerType.ContainsKey(argType))
		{
			foreach (StringBuilder typeDefBuilder in from typedef in this.data.TypeDefsPerType[argType]
													 select new StringBuilder(typedef))
			{
				this.FormatType(typeDefBuilder);
				typeBuilder.Append("|(").Append(typeDefBuilder).Append(')');
			}
		}
		typeBuilder.Append(')');
		return typeBuilder.ToString();
	}

	private void FormatType(StringBuilder typeBuilder)
	{
		int indexOfLt = Int32.MinValue;
		int indexOfGt = Int32.MinValue;
		int indexOfColon = Int32.MinValue;
		int firstColonIndex = Int32.MinValue;
		List<char> templateType = new List<char>(typeBuilder.Length);
		for (int i = typeBuilder.Length - 1; i >= 0; i--)
		{
			char @char = typeBuilder[i];
			switch (@char)
			{
				case '<':
					indexOfLt = i;
					break;
				case '>':
					indexOfGt = i;
					break;
				case ':':
					if (firstColonIndex < 0)
					{
						firstColonIndex = i;
					}
					else
					{
						if (i == firstColonIndex - 1)
						{
							indexOfColon = firstColonIndex;
							firstColonIndex = Int32.MinValue;
						}
					}
					break;
			}
			if (i > indexOfLt && i < indexOfGt)
			{
				typeBuilder.Remove(i, 1);
				templateType.Insert(0, @char);
			}
		}
		if (indexOfGt > indexOfLt)
		{
			typeBuilder.Replace("(", @"\(").Replace(")", @"\)");
			typeBuilder.Replace(@"*", @"\s*(\*|(\[\]))").Replace(@"&", @"\s*&").Replace(",", @",\s*");
			typeBuilder.Insert(indexOfLt + 1, this.GetTypeRegex(new string(templateType.ToArray())));
		}
		else
		{
			if (indexOfColon > 0)
			{
				int parentTypeStart = Math.Max(indexOfLt + 1, 0);
				typeBuilder.Remove(parentTypeStart, indexOfColon + 1 - parentTypeStart);
				typeBuilder.Insert(parentTypeStart, @"(const )?(\w+::)?");
				typeBuilder.Replace(@"*", @"\s*(\*|(\[\]))").Replace(@"&", @"\s*&").Replace(",", @",\s*");
			}
			else
			{
				typeBuilder.Replace("(", @"\(").Replace(")", @"\)");
				typeBuilder.Replace(@"*", @"\s*(\*|(\[\]))").Replace(@"&", @"\s*&").Replace(",", @",\s*");
			}
		}
	}

	private static void FillMissingParameterNames(CodeTypeMember cmm, string signature)
	{
		CodeMemberMethod method = cmm as CodeMemberMethod;
		if (method == null)
		{
			return;
		}
		List<string> args = new List<string>(signature.Split(','));
		if (args.Count < method.Parameters.Count)
		{
			// operator
			args.Insert(0, "one");
		}
		const string regex = @"^(.+?\s+)(?<name>\w+)(\s*=\s*[^\(,\s]+(\(\s*\))?)?\s*$";
		MethodsGenerator.RenameParameters(method, (from arg in args
												   select Regex.Match(arg, regex).Groups["name"].Value).ToList());
	}

	private void GatherDocs()
	{
		IDictionary<string, string> documentation = Get(this.data.Docs);
		foreach (CodeTypeDeclaration type in from smokeType in this.data.SmokeTypeMap
											 where string.IsNullOrEmpty((string) smokeType.Value.UserData["parent"])
											 select smokeType.Value)
		{
			foreach (CodeTypeDeclaration nestedType in type.Members.OfType<CodeTypeDeclaration>().Where(t => !t.IsEnum))
			{
				this.GetClassDocs(nestedType, string.Format("{0}::{1}", type.Name, nestedType.Name),
								  string.Format("{0}-{1}", type.Name, nestedType.Name), documentation);
			}
			this.GetClassDocs(type, type.Name, type.Name, documentation);
		}
		this.staticDocumentation.AddRange(from k in this.translator.TypeStringMap.Keys.SelectMany(k => new[] { k + ".html", k + "-obsolete.html", k + "-qt3.html" })
										  let key = k.ToLowerInvariant()
										  where documentation.ContainsKey(key)
										  select StripTags(documentation[key]));
		this.staticDocumentation.AddRange(from pair in documentation
										  where pair.Key.StartsWith("q", StringComparison.Ordinal) &&
												pair.Key.EndsWith("-h.html", StringComparison.Ordinal)
										  select StripTags(pair.Value));
	}

	private void GetClassDocs(CodeTypeDeclaration type, string typeName, string fileName, IDictionary<string, string> documentation)
	{
		List<string> docs = new List<string>();
		foreach (string docFile in new[] { fileName + ".html", fileName + "-obsolete.html", fileName + "-qt3.html" })
		{
			if (documentation.ContainsKey(docFile.ToLowerInvariant()))
			{
				string classDocs = StripTags(documentation[docFile.ToLowerInvariant()]);
				Match match = Regex.Match(classDocs, string.Format(@"(?<class>((The {0})|(This class)).+?)More\.\.\..*?\n" +
															  @"Detailed Description\s+(?<detailed>.*?)(\n){{3,}}" +
															  @"((\w+ )*\w+ Documentation\n(?<members>.+))", typeName),
										  RegexOptions.Singleline | RegexOptions.ExplicitCapture);
				if (match.Success)
				{
					type.Comments.Add(new CodeCommentStatement("<summary>", true));
					string summary = match.Groups["class"].Value.Trim();
					type.Comments.Add(new CodeCommentStatement(HtmlEncoder.HtmlEncode(summary), true));
					type.Comments.Add(new CodeCommentStatement("</summary>", true));
					string detailed = match.Groups["detailed"].Value.Replace(summary, string.Empty);
					FormatComment(detailed.Replace("\n/", "\n /"), type, false, "remarks");
					string members = match.Groups["members"].Value;
					docs.Add(members);
					Match matchStatic = Regex.Match(members, "Related Non-Members(?<static>.+)",
													RegexOptions.Singleline | RegexOptions.ExplicitCapture);
					if (matchStatic.Success)
					{
						this.staticDocumentation.Add(matchStatic.Groups["static"].Value);
					}
				}
				else
				{
					docs.Add(classDocs);
				}
			}
		}
		this.memberDocumentation[type] = docs;
	}

	private static string StripTags(string source)
	{
		char[] array = new char[source.Length];
		List<char> tagArray = new List<char>();
		int arrayIndex = 0;
		bool inside = false;

		foreach (char @let in source)
		{
			if (@let == '<')
			{
				inside = true;
				continue;
			}
			if (@let == '>')
			{
				inside = false;
				continue;
			}
			if (inside)
			{
				tagArray.Add(@let);
			}
			else
			{
				string tag = new string(tagArray.ToArray());
				if (tag.Contains("/tdtd"))
				{
					array[arrayIndex++] = '\t';
				}
				tagArray.Clear();
				array[arrayIndex++] = @let;
			}
		}
		return HtmlEncoder.HtmlDecode(new string(array, 0, arrayIndex));
	}

	private static IDictionary<string, string> Get(string docsPath)
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
															   f => new StringBuilder(File.ReadAllText(f)).Replace("\r", string.Empty).Replace(@"\", @"\\").ToString());
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

	private static void FormatComment(string docs, CodeTypeMember cmp, bool obsolete = false, string tag = "summary")
	{
		StringBuilder obsoleteMessageBuilder = new StringBuilder();
		cmp.Comments.Add(new CodeCommentStatement(string.Format("<{0}>", tag), true));
		foreach (string line in HtmlEncoder.HtmlEncode(docs).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.None))
		{
			cmp.Comments.Add(new CodeCommentStatement(string.Format("<para>{0}</para>", line), true));
			if (obsolete && (line.Contains("instead") || line.Contains("deprecated")))
			{
				obsoleteMessageBuilder.Append(HtmlEncoder.HtmlDecode(line));
				obsoleteMessageBuilder.Append(' ');
			}
		}
		cmp.Comments.Add(new CodeCommentStatement(string.Format("</{0}>", tag), true));
		if (obsolete)
		{
			if (obsoleteMessageBuilder.Length > 0)
			{
				obsoleteMessageBuilder.Remove(obsoleteMessageBuilder.Length - 1, 1);
			}
			CodeTypeReference obsoleteAttribute = new CodeTypeReference(typeof(ObsoleteAttribute));
			CodePrimitiveExpression obsoleteMessage = new CodePrimitiveExpression(obsoleteMessageBuilder.ToString());
			cmp.CustomAttributes.Add(new CodeAttributeDeclaration(obsoleteAttribute, new CodeAttributeArgument(obsoleteMessage)));
		}
	}
}
