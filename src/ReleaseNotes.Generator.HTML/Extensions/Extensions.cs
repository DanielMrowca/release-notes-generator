using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReleaseNotes.Generator.HTML.Extensions
{
    public static class Extensions
    {
        public static List<Tag> AsserTagExists(this List<Tag> tags, string tagName)
        {
            if (!tags.Any(x => x.FriendlyName == tagName || x.CanonicalName == tagName))
                throw new NotFoundException($"Specified tag '{tagName}' does not exists!");

            return tags;

        }

        public static Tag GetTag(this List<Tag> tags, string tagName)
        {
            return tags.SingleOrDefault(x => x.FriendlyName == tagName || x.CanonicalName == tagName);
        }
    }
}
