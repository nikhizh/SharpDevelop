﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Threading;
using ICSharpCode.Core;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Parser;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Refactoring;

namespace CSharpBinding.Parser
{
	public class TParser : IParser
	{
		///<summary>IParser Interface</summary>
		string[] lexerTags;
		
		public string[] LexerTags {
			get {
				return lexerTags;
			}
			set {
				lexerTags = value;
			}
		}
		
		public bool CanParse(string fileName)
		{
			return Path.GetExtension(fileName).Equals(".CS", StringComparison.OrdinalIgnoreCase);
		}
		
		/*
		void RetrieveRegions(ICompilationUnit cu, ICSharpCode.NRefactory.Parser.SpecialTracker tracker)
		{
			for (int i = 0; i < tracker.CurrentSpecials.Count; ++i) {
				ICSharpCode.NRefactory.PreprocessingDirective directive = tracker.CurrentSpecials[i] as ICSharpCode.NRefactory.PreprocessingDirective;
				if (directive != null) {
					if (directive.Cmd == "#region") {
						int deep = 1;
						for (int j = i + 1; j < tracker.CurrentSpecials.Count; ++j) {
							ICSharpCode.NRefactory.PreprocessingDirective nextDirective = tracker.CurrentSpecials[j] as ICSharpCode.NRefactory.PreprocessingDirective;
							if (nextDirective != null) {
								switch (nextDirective.Cmd) {
									case "#region":
										++deep;
										break;
									case "#endregion":
										--deep;
										if (deep == 0) {
											cu.FoldingRegions.Add(new FoldingRegion(directive.Arg.Trim(), DomRegion.FromLocation(directive.StartPosition, nextDirective.EndPosition)));
											goto end;
										}
										break;
								}
							}
						}
						end: ;
					}
				}
			}
		}
		 */
		
		public ParseInformation Parse(IProjectContent projectContent, FileName fileName, ITextSource fileContent,
		                              bool fullParseInformationRequested)
		{
			CSharpParser parser = new CSharpParser();
			parser.GenerateTypeSystemMode = !fullParseInformationRequested;
			CompilationUnit cu;
			try {
				cu = parser.Parse(fileContent.CreateReader());
			} catch (Exception ex) {
				LoggingService.Error(ex);
				cu = new CompilationUnit();
			}
			
			TypeSystemConvertVisitor cv = new TypeSystemConvertVisitor(projectContent, fileName);
			CSharpParsedFile file = cv.Convert(cu);
			
			ParseInformation info = new ParseInformation(file, fullParseInformationRequested);
			if (fullParseInformationRequested)
				info.AddAnnotation(cu);
			return info;
		}
		
		/*void AddCommentTags(ICompilationUnit cu, System.Collections.Generic.List<ICSharpCode.NRefactory.Parser.TagComment> tagComments)
		{
			foreach (ICSharpCode.NRefactory.Parser.TagComment tagComment in tagComments) {
				DomRegion tagRegion = new DomRegion(tagComment.StartPosition.Y, tagComment.StartPosition.X);
				var tag = new ICSharpCode.SharpDevelop.Dom.TagComment(tagComment.Tag, tagRegion, tagComment.CommentText);
				cu.TagComments.Add(tag);
			}
		}
		 */
		
		public ResolveResult Resolve(ParseInformation parseInfo, TextLocation location, ITypeResolveContext context, CancellationToken cancellationToken)
		{
			CompilationUnit cu = parseInfo.Annotation<CompilationUnit>();
			if (cu == null)
				throw new ArgumentException("Parse info does not have CompilationUnit");
			CSharpParsedFile parsedFile = parseInfo.ParsedFile as CSharpParsedFile;
			if (parsedFile == null)
				throw new ArgumentException("Parse info does not have a C# ParsedFile");
			
			return ResolveAtLocation.Resolve(context, parsedFile, cu, location, cancellationToken);
		}
		
		public void FindLocalReferences(ParseInformation parseInfo, IVariable variable, ITypeResolveContext context, Action<Reference> callback)
		{
			CompilationUnit cu = parseInfo.Annotation<CompilationUnit>();
			if (cu == null)
				throw new ArgumentException("Parse info does not have CompilationUnit");
			
			CSharpParsedFile parsedFile = parseInfo.ParsedFile as CSharpParsedFile;
			if (parsedFile == null)
				throw new ArgumentException("Parse info does not have a C# ParsedFile");
			
			new FindReferences().FindLocalReferences(
				variable, parsedFile, cu, context,
				delegate (AstNode node, ResolveResult result) {
					var region = new DomRegion(parseInfo.FileName, node.StartLocation, node.EndLocation);
					callback(new Reference(region, result));
				});
		}
	}
}
