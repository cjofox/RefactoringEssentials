using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace RefactoringEssentials
{
    public static class GeneratedCodeRecognition
    {

		static WeakReference<ImmutableDictionary<SyntaxTree, bool>> cache = new WeakReference<ImmutableDictionary<SyntaxTree, bool>> (ImmutableDictionary<SyntaxTree, bool>.Empty);

        public static bool IsFromGeneratedCode(this SemanticModel semanticModel, CancellationToken cancellationToken)
        {
			ImmutableDictionary<SyntaxTree, bool> table;
			var tree = semanticModel.SyntaxTree;

			if (cache.TryGetTarget(out table))
			{
				if (table.ContainsKey(tree))
					return table[tree];
			}
			else 
			{
				table = ImmutableDictionary<SyntaxTree, bool>.Empty;
			}

			var result = IsFileNameForGeneratedCode(tree.FilePath) || ContainsAutogeneratedComment(tree, cancellationToken);
			cache.SetTarget(table.Add(tree, result));

			return result;
        }

        public static bool IsFromGeneratedCode(this SyntaxNodeAnalysisContext context)
        {
            return IsFromGeneratedCode(context.SemanticModel, context.CancellationToken);
        }

        public static bool IsFromGeneratedCode(this SemanticModelAnalysisContext context)
        {
            return IsFromGeneratedCode(context.SemanticModel, context.CancellationToken);
        }

        static readonly string[] generatedCodeSuffixes = {
            "AssemblyInfo",
            ".designer",
            ".generated",
            ".g",
            ".g.i",
            ".AssemblyAttributes"
        };

        static readonly string generatedCodePrefix = "TemporaryGeneratedFile_";
        const int generatedCodePrefixLength = 23;

        public unsafe static bool IsFileNameForGeneratedCode(string fileName)
        {
            char* curPtr, endPtr;
            fixed (char* beginPtr = fileName)
            {
                // Check prefix
                if (generatedCodePrefixLength < fileName.Length)
                {
                    curPtr = beginPtr;
                    endPtr = beginPtr + generatedCodePrefixLength;

                    fixed (char* patternPtr = generatedCodePrefix)
                    {
                        char* curPatternPtr = patternPtr;
                        while (curPtr != endPtr)
                        {
                            if (char.ToUpperInvariant (*curPtr) != char.ToUpperInvariant (*curPatternPtr))
                            {
                                break;
                            }
                            curPtr++;
                            curPatternPtr++;
                        }
                        if (curPtr == endPtr)
                        {
                            return true;
                        }
                    }
                }

                // Search last index of '.'
                curPtr = beginPtr + fileName.Length - 1;
                while (curPtr >= beginPtr && *curPtr != '.')
                {
                    curPtr--;
                }
                if (curPtr < beginPtr)
                {
                    return false;
                }
                endPtr = curPtr;

                // Check suffix
                for (int i = 0; i < generatedCodeSuffixes.Length; i++)
                {
                    string str = generatedCodeSuffixes[i];
                    curPtr = endPtr - str.Length;
                    if (curPtr < beginPtr)
                        continue;
                    fixed (char* patternPtr = str)
                    {
                        char* curPatternPtr = patternPtr;
                        while (curPtr != endPtr)
                        {
                            if (char.ToUpperInvariant (*curPtr) != char.ToUpperInvariant (*curPatternPtr))
                            {
                                break;
                            }
                            curPtr++;
                            curPatternPtr++;
                        }
                        if (curPtr == endPtr)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static bool ContainsAutogeneratedComment(SyntaxTree tree, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = tree.GetRoot(cancellationToken);
            if (root == null)
                return false;
            var firstToken = root.GetFirstToken();
            if (!firstToken.HasLeadingTrivia)
            {
                return false;
            }

            foreach (var trivia in firstToken.LeadingTrivia.Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)).Take(2))
            {
                var str = trivia.ToString();
                if (str == "// This file has been generated by the GUI designer. Do not modify." ||
                    str == "// <auto-generated>" || str == "// <autogenerated>")
                {
                    return true;
                }
            }
            return false;
        }
    }
}