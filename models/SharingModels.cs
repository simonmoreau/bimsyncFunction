
using System.Collections.Generic;

namespace bimsyncFunction
{
    public class SharingCode
    {
        public string id { get; set; }

        public System.DateTime RefreshDate{get;set;}
        public string UserId { get; set; }
        public bimsync.ViewerToken Viewer2dToken { get; set; }
        public bimsync.ViewerToken Viewer3dToken { get; set; }
        public List<bimsync.Model> SharedModels { get; set; }
        public SharedRevisions SharedRevisions { get; set; }
        public List<string> SpacesId {get;set;}
    }

    public class SharedRevisions
    {
        public string ProjectId {get;set;}
        public string[] Revisions3D { get; set; }
        public string Revision2D { get; set; }
    }

    public class SharedRevisions3D
    {
        public string[] revisions { get; set; }
    }

        public class SharedRevisions2D
    {
        public string revision { get; set; }
    }
}
