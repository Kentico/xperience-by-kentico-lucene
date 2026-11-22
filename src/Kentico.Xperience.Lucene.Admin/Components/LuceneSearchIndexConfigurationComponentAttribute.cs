using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Represents an attribute used to mark a form component as a Lucene index configuration component.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal class LuceneSearchIndexConfigurationComponentAttribute : FormComponentAttribute
{
}
