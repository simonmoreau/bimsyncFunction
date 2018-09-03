using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace bimsyncFunction
{
    public class Services
    {
        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        public static async Task<SharingCode> CreateSharingCode(SharedRevisions sharedRevisions, User user, List<bimsync.Model> SharedModels, List<string> spacesIds, string id)
        {
                bimsync.ViewerToken token2d = await bimsync.bimsyncServices.GetViewer2DToken(user.AccessToken, sharedRevisions.ProjectId, sharedRevisions.Revision2D);
                bimsync.ViewerToken token3d = await bimsync.bimsyncServices.GetViewer3DToken(user.AccessToken, sharedRevisions.ProjectId, sharedRevisions.Revisions3D);

                SharingCode sharingCode = new SharingCode
                {
                    id = id,
                    RefreshDate = System.DateTime.Now + new System.TimeSpan(0, 0, 3599),
                    UserId = user.id,
                    Viewer2dToken = token2d,
                    Viewer3dToken = token3d,
                    SharedRevisions = sharedRevisions,
                    SharedModels = SharedModels,
                    SpacesId = spacesIds
                };

                return sharingCode;
        }
    }
}