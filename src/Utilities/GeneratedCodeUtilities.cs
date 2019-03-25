﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using CSharp = Microsoft.CodeAnalysis.CSharp;
using VisualBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace Roslyn.Utilities
{
    internal static class GeneratedCodeUtilities
    {
        private static readonly string[] s_autoGeneratedStrings = new[] { "<autogenerated", "<auto-generated" };

        internal static async Task<bool> IsGeneratedCodeAsync(Document document, CancellationToken cancellationToken)
        {
            var isCommentTrivia = document.Project.Language == LanguageNames.CSharp
                ? IsCSharpCommentTrivia
                : IsVisualBasicCommentTrivia;

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return GeneratedCodeUtilities.IsGeneratedCode(syntaxTree, isCommentTrivia, cancellationToken);
        }

        internal static bool IsGeneratedCode(
            SyntaxTree tree, Func<SyntaxTrivia, bool> isComment, CancellationToken cancellationToken)
        {
            return IsGeneratedCodeFile(tree.FilePath) ||
                   BeginsWithAutoGeneratedComment(tree, isComment, cancellationToken);
        }

        private static bool IsGeneratedCodeFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var extension = Path.GetExtension(fileName);
                if (!string.IsNullOrEmpty(extension))
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                    if (fileNameWithoutExtension.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".generated", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".g", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".g.i", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool BeginsWithAutoGeneratedComment(
            SyntaxTree tree, Func<SyntaxTrivia, bool> isComment, CancellationToken cancellationToken)
        {
            var root = tree.GetRoot(cancellationToken);
            if (root.HasLeadingTrivia)
            {
                var leadingTrivia = root.GetLeadingTrivia();

                foreach (var trivia in leadingTrivia)
                {
                    if (!isComment(trivia))
                    {
                        continue;
                    }

                    var text = trivia.ToString();

                    // Check to see if the text of the comment contains an auto generated comment.
                    foreach (var autoGenerated in s_autoGeneratedStrings)
                    {
                        if (text.Contains(autoGenerated))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static readonly Func<SyntaxTrivia, bool> IsCSharpCommentTrivia =
            (syntaxTrivia) => syntaxTrivia.IsKind(CSharp.SyntaxKind.SingleLineCommentTrivia)
                || syntaxTrivia.IsKind(CSharp.SyntaxKind.MultiLineCommentTrivia)
                || syntaxTrivia.IsKind(CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia)
                || syntaxTrivia.IsKind(CSharp.SyntaxKind.MultiLineDocumentationCommentTrivia);

        private static readonly Func<SyntaxTrivia, bool> IsVisualBasicCommentTrivia =
            (syntaxTrivia) => syntaxTrivia.IsKind(VisualBasic.SyntaxKind.CommentTrivia)
                || syntaxTrivia.IsKind(VisualBasic.SyntaxKind.DocumentationCommentTrivia);
    }
}
