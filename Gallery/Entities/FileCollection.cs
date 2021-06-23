/// Represents a group of files filtered by their containing folder and/or a set of search parameters to match against their metadata.
/// FileCollection doesn't actually contain or fetch any files; it defines a set of criteria that can be used to define a collection of files.
namespace Gallery.Entities
{
    using System.Collections.Generic;

    using Gallery.Entities.SearchParameters;

    // Todo:
    // Should this class be mutable or not? Could it be a struct or record?
    // (Should other classes be able to set the collections, or just modify them directly?
    public class FileCollection
    {
        public FileCollection(IEnumerable<string>? sourceFolders = null, IEnumerable<ISearchParameter>? parameters = null)
        {
            SourceFolders = sourceFolders != null ? new List<string>(sourceFolders) : new List<string>();
            Parameters = parameters != null ? new List<ISearchParameter>(parameters) : new List<ISearchParameter>();
        }

        public bool IncludeUntracked { get; set; } = true;

        /// An empty SourceFolders list implies that the parameters are meant to be applied to any set of source folders
        /// (probably whichever folders are already selected when this FileCollection is loaded).
        //
        // Todo: List, Set, something else? List order could theoretically be useful
        // (e.g. display folders' contents in the order in which the folders were selected?)
        public IList<string> SourceFolders { get; set; }

        public IList<ISearchParameter> Parameters { get; set; }
    }
}
