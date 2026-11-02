using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.ContactManagement;
using CMS.Core;
using CMS.FormEngine;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Samples.DancingGoat;

/// <summary>
/// Supplies the selectable contact fields for the <see cref="ContactKeyFieldChangedTriggerProperties.WatchedField"/>
/// drop-down. Only a curated set of marketer-relevant profile fields is offered, so the trigger watches
/// meaningful contact changes.
/// </summary>
internal sealed class ContactFieldsOptionsProvider : IDropDownOptionsProvider
{
    private const string CONTACT_EDIT_FORM_NAME = $"{ContactInfo.OBJECT_TYPE}.ContactEdit";


    private static readonly HashSet<string> watchableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(ContactInfo.ContactFirstName),
        nameof(ContactInfo.ContactLastName),
        nameof(ContactInfo.ContactEmail),
        nameof(ContactInfo.ContactCompanyName),
        nameof(ContactInfo.ContactJobTitle),
        nameof(ContactInfo.ContactBirthday),
        nameof(ContactInfo.ContactMobilePhone),
        nameof(ContactInfo.ContactBusinessPhone),
        nameof(ContactInfo.ContactAddress1),
        nameof(ContactInfo.ContactCity),
        nameof(ContactInfo.ContactZIP),
        nameof(ContactInfo.ContactCountryID)
    };


    private readonly ILocalizationService localizationService;


    /// <summary>
    /// Initializes a new instance of the <see cref="ContactFieldsOptionsProvider"/> class.
    /// </summary>
    public ContactFieldsOptionsProvider(ILocalizationService localizationService)
    {
        this.localizationService = localizationService;
    }


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var contactForm = FormHelper.GetFormInfo(CONTACT_EDIT_FORM_NAME, false);
        if (contactForm is null)
        {
            return Task.FromResult(Enumerable.Empty<DropDownOptionItem>());
        }

        var optionItems = contactForm
            .GetFields<FormFieldInfo>()
            .Where(field => watchableFields.Contains(field.Name))
            .Select(field => new DropDownOptionItem
            {
                Value = field.Name,
                Text = GetFieldCaption(field)
            })
            .OrderBy(item => item.Text)
            .AsEnumerable();

        return Task.FromResult(optionItems);
    }


    private string GetFieldCaption(FormFieldInfo field)
    {
        if (string.IsNullOrEmpty(field.Caption))
        {
            return field.Name;
        }

        return localizationService.LocalizeString(field.Caption);
    }
}
