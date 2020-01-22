using LibGit2Sharp;
using System.Collections.Generic;

namespace ReleaseNotes.Generator.HTML.Models
{
    public class ReleaseNotesViewModel
    {
        public string ReleaseName { get; set; }
        public string ReleaseComment { get; set; }
        public List<CommitViewModel> Changes { get; set; }
    }
}
