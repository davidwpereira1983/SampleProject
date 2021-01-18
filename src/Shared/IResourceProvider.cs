namespace Company.TestProject.Shared
{
    public interface IResourceProvider
    {
        string GetTextResourceById(string resourceId, string culture = "en-US");
    }
}
