﻿#nullable enable

using System.Xml;
using System.Reflection;

namespace Bang.Lingo.Extensions;

public static class LingoExtensions
{
	public static void AddXml(this Lingo interpreterProvider, String path, Boolean throwIfNotExists = true)
	{
		interpreterProvider.LoadTranslations += interpreterProvider =>
		{
			var physicalPath = path;

			if(Directory.Exists(physicalPath))
			{
				foreach(var file in Directory.GetFiles(physicalPath, "*.xml", SearchOption.AllDirectories))
				{
					using(var fileStream = File.OpenRead(file))

					LoadXml(interpreterProvider, fileStream);
				}
			}
			else if(File.Exists(physicalPath))
			{
					using(var fileStream = File.OpenRead(path))

					LoadXml(interpreterProvider, fileStream);
			}
			else if(throwIfNotExists)
			{
				throw new ArgumentException($"Could not find path '{path}'");
			}
		};
	}

	public static void AddXml(this Lingo interpreterProvider, Assembly assembly, String path, Boolean throwIfNotExists = true)
	{
		interpreterProvider.LoadTranslations += interpreterProvider =>
		{
			//path = path.UnSuffix('/');

			var resCount = 0;
			var resPattern = path.Replace("/", ".");
			var strComp = StringComparison.OrdinalIgnoreCase;

			if(resPattern.StartsWith('.'))
			{
				resPattern = $"{assembly.GetName().Name}{resPattern}";
			}

			foreach(var resId in assembly.GetManifestResourceNames()
				.Where(r => r.Equals(resPattern, strComp) || (r.StartsWith(resPattern + ".", strComp) && r.EndsWith(".xml", strComp))))
			{
				var stream = assembly.GetManifestResourceStream(resId);

				if(stream != null)
				{
					resCount++;
					LoadXml(interpreterProvider, stream);
				}
			}

			if(resCount == 0 && throwIfNotExists)
			{
				throw new ArgumentException($"Could not find resource '{resPattern}'");
			}
		};
	}



	#region Protected Area
	
	private static void LoadXml(Lingo lingo, Stream stream)
	{
		var doc = new XmlDocument();

		doc.Load(stream);

		static void ReadXml(TranslationDictionary dictionary, XmlNodeList nodes, String? key = null)
		{
			if(nodes.Count > 0)
			{
				foreach(XmlNode node in nodes)
				{
					switch(node.NodeType)
					{
						case XmlNodeType.Text:
							var text = node.Value;

							var markup = node.ParentNode?.Attributes?["Markup"]?.Value;
							var textFormat = markup != null ? (markup.Equals("Paragraphed", StringComparison.OrdinalIgnoreCase) ? TextFormat.ParagraphedMarkup : TextFormat.Markup) : TextFormat.Plain;

							if(key != null)
							{
								dictionary.Add(key, text ?? String.Empty, textFormat, replaceIfExists: true);
							}
							break;

						case XmlNodeType.Element:
							ReadXml(dictionary, node.ChildNodes, String.Format("{0}{1}{2}", key, key != null ? "." : null, node.Name));
							break;
					}
				}
			}
			else if(key != null)
			{
				dictionary.Add(key, String.Empty);
			}
		}


		foreach(XmlNode node in doc.GetElementsByTagName("Translations"))
		{
			if(node != null)
			{
				var lang = node.Attributes?.GetNamedItem("Language")?.Value;

				if(lang != null)
				{
					ReadXml(lingo.GetDictionary(lang), node.ChildNodes);
				}
			}
		}
	}

	#endregion

}
