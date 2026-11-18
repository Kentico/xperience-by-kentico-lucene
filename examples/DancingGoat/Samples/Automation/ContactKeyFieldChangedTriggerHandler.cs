using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;

namespace Samples.DancingGoat;

/// <summary>
/// Fires <see cref="ContactKeyFieldChangedAutomationTrigger"/> after a <see cref="ContactInfo"/> is updated.
/// Uses the typed info object update events, which provide a dedicated asynchronous execution path
/// (<see cref="HandleAsync(InfoObjectAfterUpdateEvent{ContactInfo}, CancellationToken)"/>), instead of the legacy
/// <c>ContactInfo.TYPEINFO.Events.Update</c> delegate.
/// </summary>
/// <remarks>
/// The before-update stage captures the changed fields (available only before the save) into the shared event
/// <see cref="InfoObjectEvent.State"/>; the after-update stage reads them and fires the trigger once the update succeeded.
/// </remarks>
internal sealed class ContactKeyFieldChangedTriggerHandler :
    IInfoObjectEventHandler<InfoObjectBeforeUpdateEvent<ContactInfo>>,
    IInfoObjectEventHandler<InfoObjectAfterUpdateEvent<ContactInfo>>
{
    private const string CHANGED_FIELDS_STATE_KEY = "DancingGoat.ContactKeyFieldChanged.ChangedFields";


    private readonly IAutomationTriggerDispatcher triggerDispatcher;


    /// <summary>
    /// Initializes a new instance of the <see cref="ContactKeyFieldChangedTriggerHandler"/> class.
    /// </summary>
    public ContactKeyFieldChangedTriggerHandler(IAutomationTriggerDispatcher triggerDispatcher)
    {
        this.triggerDispatcher = triggerDispatcher;
    }


    /// <inheritdoc />
    public void Handle(InfoObjectBeforeUpdateEvent<ContactInfo> infoObjectEvent)
    {
        CaptureChangedFields(infoObjectEvent);
    }


    /// <inheritdoc />
    public Task HandleAsync(InfoObjectBeforeUpdateEvent<ContactInfo> infoObjectEvent, CancellationToken cancellationToken)
    {
        CaptureChangedFields(infoObjectEvent);

        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public void Handle(InfoObjectAfterUpdateEvent<ContactInfo> infoObjectEvent)
    {
        var dispatch = BuildDispatch(infoObjectEvent);
        if (dispatch is null)
        {
            return;
        }

        triggerDispatcher.FireTrigger<ContactKeyFieldChangedAutomationTrigger>(dispatch).GetAwaiter().GetResult();
    }


    /// <inheritdoc />
    public async Task HandleAsync(InfoObjectAfterUpdateEvent<ContactInfo> infoObjectEvent, CancellationToken cancellationToken)
    {
        var dispatch = BuildDispatch(infoObjectEvent);
        if (dispatch is null)
        {
            return;
        }

        await triggerDispatcher.FireTrigger<ContactKeyFieldChangedAutomationTrigger>(dispatch, cancellationToken);
    }


    private static void CaptureChangedFields(InfoObjectBeforeUpdateEvent<ContactInfo> infoObjectEvent)
    {
        var changedFields = infoObjectEvent.InfoObject
            .ChangedColumns()
            .Where(column => !string.Equals(column, nameof(ContactInfo.ContactLastModified), StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        infoObjectEvent.State.SetValue(CHANGED_FIELDS_STATE_KEY, changedFields);
    }


    private static AutomationTriggerDispatch BuildDispatch(InfoObjectAfterUpdateEvent<ContactInfo> infoObjectEvent)
    {
        if (!infoObjectEvent.State.TryGetValue<List<string>>(CHANGED_FIELDS_STATE_KEY, out var changedFields) || changedFields.Count == 0)
        {
            return null;
        }

        var triggerData = new ContactKeyFieldChangedTriggerData
        {
            ChangedFields = changedFields
        };

        return new AutomationTriggerDispatch(infoObjectEvent.InfoObject, triggerData);
    }
}
