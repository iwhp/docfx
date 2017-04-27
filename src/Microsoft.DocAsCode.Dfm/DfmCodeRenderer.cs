﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmCodeRenderer
    {
        private readonly DfmCodeExtractor _dfmCodeExtractor = new DfmCodeExtractor();

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmFencesToken token, IMarkdownContext context)
        {
            if (!PathUtility.IsRelativePath(token.Path))
            {
                string errorMessage = $"Code absolute path: {token.Path} is not supported in file {context.GetFilePathStack().Peek()}";
                Logger.LogError(errorMessage);
                return RenderFencesCode(token, renderer.Options, errorMessage);
            }

            try
            {
                // Always report original dependency
                context.ReportDependency(token.Path);
                var filePath = FindFile(token, context);
                var code = ExtractCode(token, filePath);
                return RenderFencesCode(token, renderer.Options, code.ErrorMessage, code.CodeLines);
            }
            catch (DirectoryNotFoundException)
            {
                return RenderReferenceNotFoundErrorMessage(renderer, token);
            }
            catch (FileNotFoundException)
            {
                return RenderReferenceNotFoundErrorMessage(renderer, token);
            }
        }

        public virtual string FindFile(DfmFencesToken token, IMarkdownContext context)
        {
            return DfmFallbackHelper.GetFilePathWithFallback(token.Path, context).Item1;
        }

        public virtual DfmExtractCodeResult ExtractCode(DfmFencesToken token, string filePath)
        {
            return _dfmCodeExtractor.ExtractFencesCode(token, filePath);
        }

        public virtual StringBuffer RenderFencesCode(DfmFencesToken token, Options options, string errorMessage, string[] codeLines = null)
        {
            StringBuffer result;
            string renderedErrorMessage = string.Empty;
            string renderedCodeLines = string.Empty;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                result = RenderCodeErrorString(errorMessage);
            }
            else
            {
                result = StringBuffer.Empty;
            }

            if (codeLines != null)
            {
                result = RenderOpenPreTag(result, token, options);
                result = RenderOpenCodeTag(result, token, options);
                foreach (var line in codeLines)
                {
                    result += StringHelper.HtmlEncode(line);
                    result += "\n";
                }
                result = RenderCloseCodeTag(result, token, options);
                result = RenderClosePreTag(result, token, options);
            }

            return result;
        }

        public virtual StringBuffer RenderOpenPreTag(StringBuffer result, DfmFencesToken token, Options options)
        {
            return result + "<pre>";
        }

        public virtual StringBuffer RenderClosePreTag(StringBuffer result, DfmFencesToken token, Options options)
        {
            return result + "</pre>";
        }

        public virtual StringBuffer RenderOpenCodeTag(StringBuffer result, DfmFencesToken token, Options options)
        {
            result += "<code";
            if (!string.IsNullOrEmpty(token.Lang))
            {
                result = result + " class=\"" + options.LangPrefix + token.Lang + "\"";
            }
            if (!string.IsNullOrEmpty(token.Name))
            {
                result = result + " name=\"" + StringHelper.HtmlEncode(token.Name) + "\"";
            }
            if (!string.IsNullOrEmpty(token.Title))
            {
                result = result + " title=\"" + StringHelper.HtmlEncode(token.Title) + "\"";
            }
            if (!string.IsNullOrEmpty(token.PathQueryOption?.HighlightLines))
            {
                result = result + " highlight-lines=\"" + StringHelper.HtmlEncode(token.PathQueryOption.HighlightLines) + "\"";
            }
            result += ">";
            return result;
        }

        public virtual StringBuffer RenderCloseCodeTag(StringBuffer result, DfmFencesToken token, Options options)
        {
            return result + "</code>";
        }

        public virtual StringBuffer RenderReferenceNotFoundErrorMessage(IMarkdownRenderer renderer, DfmFencesToken token)
        {
            var errorMessageInMarkdown = $"Can not find reference {token.Path}";
            var errorMessage = $"Unable to resolve {token.SourceInfo.Markdown}. {errorMessageInMarkdown}.";
            Logger.LogError(errorMessage, line: token.SourceInfo.LineNumber.ToString());
            return RenderCodeErrorString(errorMessageInMarkdown);
        }

        public virtual StringBuffer RenderCodeErrorString(string errorMessage)
        {
            return (StringBuffer)"<!-- " + StringHelper.HtmlEncode(errorMessage) + " -->\n";
        }

    }
}
