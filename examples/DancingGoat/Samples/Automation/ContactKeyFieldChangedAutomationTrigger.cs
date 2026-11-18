using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;

using Microsoft.Extensions.Logging;

using Samples.DancingGoat;

[assembly: RegisterAutomationTrigger<ContactKeyFieldChangedAutomationTrigger>(
    "DancingGoat.ContactKeyFieldChanged",
    "Contact key field changed",
    IconName = "xp-doc-user",
    Description = "Example of a custom trigger with a marketer-configurable contact field. Fires when a contact is updated and the selected field's value changed.")]

namespace Samples.DancingGoat;

/// <summary>
/// Demo custom automation trigger with a marketer-configurable contact field selector.
/// Fired whenever a <see cref="CMS.ContactManagement.ContactInfo"/> is updated; the typed
/// <see cref="ContactKeyFieldChangedTriggerData"/> payload carries the changed fields, and
/// <see cref="Evaluate"/> starts the configured automation process only when the marketer-selected field is among them.
/// </summary>
/// <remarks>
/// Fired by <see cref="ContactKeyFieldChangedTriggerHandler"/>, which is registered in
/// <see cref="DancingGoatSamplesModule.OnPreInit"/> for the typed contact update events.
/// </remarks>
internal sealed class ContactKeyFieldChangedAutomationTrigger(ILogger<ContactKeyFieldChangedAutomationTrigger> logger)
    : AutomationTrigger<ContactKeyFieldChangedTriggerData, ContactKeyFieldChangedTriggerProperties>
{
    private readonly ILogger<ContactKeyFieldChangedAutomationTrigger> logger = logger;


    /// <inheritdoc />
    public override Task<bool> Evaluate(
        AutomationTriggerContext context,
        ContactKeyFieldChangedTriggerProperties properties,
        ContactKeyFieldChangedTriggerData triggerData,
        CancellationToken cancellationToken)
    {
        var watchedField = properties.WatchedField;
        var fires = !string.IsNullOrEmpty(watchedField)
            && triggerData.ChangedFields.Contains(watchedField, StringComparer.InvariantCultureIgnoreCase);

        logger.LogDebug(
            "ContactKeyFieldChanged evaluated for watched field {WatchedField}: {Result}.",
            watchedField,
            fires ? "fires" : "skipped");

        return Task.FromResult(fires);
    }
}
