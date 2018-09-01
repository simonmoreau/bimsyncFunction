
using System.Collections.Generic;

namespace bimsyncFunction
{
    public class SharingCode
    {
        public string id { get; set; }
        public bimsync.ViewerToken Viewer2dToken { get; set; }
        public bimsync.ViewerToken Viewer3dToken { get; set; }
        public List<SharedModel> SharedModels { get; set; }
        public List<string> spacesId {get;set;}
    }

    public class SharedModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class SharedRevisions
    {
        public string projectId {get;set;}
        public string[] revisions3D { get; set; }
        public string revision2D { get; set; }
    }

    public class SharedRevisions3D
    {
        public string[] revisions { get; set; }
    }
}
