using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReleaseNotes.Generator.HTML.Helpers
{
    public static class Html
    {
        public static string EncodedMultiLineText(this IHtmlHelper helper, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var result = text.Replace("\n", "<br/>");
            return result;
        }
    }
}
