﻿using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FusionPlusPlus.Syntax
{
	internal class FusionLogSyntaxHighlightService : ISyntaxHighlightService
	{
		readonly Document document;
		readonly SyntaxHighlightProperties defaultSettings = new SyntaxHighlightProperties() { ForeColor = Color.White };
		readonly SyntaxHighlightProperties infoKeywordSettings = new SyntaxHighlightProperties() { ForeColor = Color.SkyBlue };
		readonly SyntaxHighlightProperties warningKeywordSettings = new SyntaxHighlightProperties() { ForeColor = Color.Orange };
		readonly SyntaxHighlightProperties errorKeywordSettings = new SyntaxHighlightProperties() { ForeColor = Color.Red };
		readonly SyntaxHighlightProperties commentSettings = new SyntaxHighlightProperties() { ForeColor = Color.LimeGreen };

		readonly string[] infoKeywords = new string[] { "LOG" };
		readonly string[] warningKeywords = new string[] { "WARNUNG", "WRN" };
		readonly string[] errorKeywords = new string[] { "FAIL", "FAILED", "ERROR", "ERR", "The operation failed", "Fehler bei diesem Vorgang" };

		public FusionLogSyntaxHighlightService(Document document)
		{
			this.document = document;
		}

		private List<SyntaxHighlightToken> ParseTokens()
		{
			var tokens = new List<SyntaxHighlightToken>();
			DocumentRange[] ranges;

			var enclosingAsterisksRegex = @"\*\*\*.*\*\*\*";
			var enclosingEqualSignsRegex = @"===.*===";
			var equalSignsRegex = @"===";
			var minusesRegex = @"---";

			var regex = new Regex($"({string.Join("|", enclosingAsterisksRegex,	enclosingEqualSignsRegex, equalSignsRegex, minusesRegex)})", RegexOptions.CultureInvariant);
			ranges = document.FindAll(regex);
			for (int i = 0; i < ranges.Length; i++)
				tokens.Add(new SyntaxHighlightToken(ranges[i].Start.ToInt(), ranges[i].End.ToInt() - ranges[i].Start.ToInt(), commentSettings));

			AddKeyWords(tokens, infoKeywords, infoKeywordSettings);
			AddKeyWords(tokens, warningKeywords, warningKeywordSettings);
			AddKeyWords(tokens, errorKeywords, errorKeywordSettings);

			// order tokens by their start position
			tokens.Sort(new SyntaxHighlightTokenComparer());

			// fill in gaps in document coverage
			AddPlainTextTokens(tokens);

			return tokens;
		}

		private void AddKeyWords(List<SyntaxHighlightToken> tokens, string[] keywords, SyntaxHighlightProperties keywordSettings)
		{
			for (int i = 0; i < keywords.Length; i++)
			{
				var ranges = document.FindAll(keywords[i], SearchOptions.WholeWord);

				for (int j = 0; j < ranges.Length; j++)
				{
					if (!IsRangeInTokens(ranges[j], tokens))
						tokens.Add(new SyntaxHighlightToken(ranges[j].Start.ToInt(), ranges[j].Length, keywordSettings));
				}
			}
		}

		private void AddPlainTextTokens(List<SyntaxHighlightToken> tokens)
		{
			int count = tokens.Count;

			if (count == 0)
			{
				tokens.Add(new SyntaxHighlightToken(0, document.Range.End.ToInt(), defaultSettings));
				return;
			}

			tokens.Insert(0, new SyntaxHighlightToken(0, tokens[0].Start, defaultSettings));

			for (int i = 1; i < count; i++)
				tokens.Insert(i * 2, new SyntaxHighlightToken(tokens[i * 2 - 1].End, tokens[i * 2].Start - tokens[i * 2 - 1].End, defaultSettings));

			tokens.Add(new SyntaxHighlightToken(tokens[count * 2 - 1].End, document.Range.End.ToInt() - tokens[count * 2 - 1].End, defaultSettings));
		}

		private bool IsRangeInTokens(DocumentRange range, List<SyntaxHighlightToken> tokens)
		{
			return tokens.Any(t => IsIntersect(range, t));
		}

		bool IsIntersect(DocumentRange range, SyntaxHighlightToken token)
		{
			int start = range.Start.ToInt();
			if (start >= token.Start && start < token.End)
				return true;

			int end = range.End.ToInt() - 1;
			if (end >= token.Start && end < token.End)
				return true;

			return false;
		}

		public void ForceExecute()
		{
			Execute();
		}

		public void Execute()
		{
			document.ApplySyntaxHighlight(ParseTokens());
		}
	}

	public class SyntaxHighlightTokenComparer : IComparer<SyntaxHighlightToken>
	{
		public int Compare(SyntaxHighlightToken x, SyntaxHighlightToken y) => x.Start - y.Start;
	}
}