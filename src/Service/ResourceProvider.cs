using Company.TestProject.Shared;

namespace Company.TestProject.Service
{
    public class ResourceProvider : IResourceProvider
    {
        public string GetTextResourceById(string resourceId, string culture = "en-US")
        {
            return resourceId;
        }
    }
}
