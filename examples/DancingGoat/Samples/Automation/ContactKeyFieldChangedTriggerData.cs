using System.Collections.Generic;

using CMS.Automation;

namespace Samples.DancingGoat;

/// <summary>
/// Typed payload carried by <see cref="ContactKeyFieldChangedAutomationTrigger"/>.
/// Captures the contact fields that changed in the update that fired the trigger, so that
/// <see cref="ContactKeyFieldChangedAutomationTrigger.Evaluate"/> can decide whether the marketer-selected field was among them.
/// </summary>
internal sealed class ContactKeyFieldChangedTriggerData : IAutomationTriggerData
{
    /// <inheritdoc />
    public string Identifier => "DancingGoat.ContactKeyFieldChanged";


    /// <summary>
    /// Names of the contact fields (columns) whose values changed in the update.
    /// </summary>
    public IReadOnlyCollection<string> ChangedFields { get; set; } = [];
}
