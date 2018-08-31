
using System.Collections.Generic;

namespace bimsyncFunction
{
    public class SharingCode
    {
        public string Viewer2dToken { get; set; }
        public string Viewer3dToken { get; set; }

        public List<SharedModel> SharedModels { get; set; }

    }

    public class SharedModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
