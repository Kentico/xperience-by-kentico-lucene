using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Samples.DancingGoat;

/// <summary>
/// Marketer-configurable properties of <see cref="ContactKeyFieldChangedAutomationTrigger"/>.
/// </summary>
internal sealed class ContactKeyFieldChangedTriggerProperties : IAutomationTriggerProperties
{
    /// <summary>
    /// Name of the contact field to watch. The trigger starts the configured automation process only when
    /// this field's value changes in a contact update.
    /// </summary>
    [DropDownComponent(
        DataProviderType = typeof(ContactFieldsOptionsProvider),
        Label = "Contact field",
        ExplanationText = "Configured automation process starts only when the selected contact field changes.",
        Order = 10)]
    [RequiredValidationRule]
    public string WatchedField { get; set; }
}
