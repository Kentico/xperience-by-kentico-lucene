using System;
using System.Linq;

using CMS.Base;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.FormEngine;
using CMS.OnlineForms;

using Kentico.Forms.Web.Mvc;
using Kentico.Forms.Web.Mvc.Internal;

namespace DancingGoat.Helpers.Generator
{
    /// <summary>
    /// Contains methods for generating sample data for the Campaign module.
    /// </summary>
    public class FormConsentGenerator
    {
        private readonly ISiteInfo mSite;
        private readonly IFormBuilderConfigurationSerializer mFormBuilderConfigurationSerializer;

        public const string CONSENT_NAME = "DancingGoatCoffeeSampleListForm";
        internal const string CONSENT_DISPLAY_NAME = "Dancing Goat - Coffee sample list form";
        private const string CONSENT_SHORT_TEXT_EN = "I hereby accept that these provided information can be used for marketing purposes and targeted website content.";
        private const string CONSENT_SHORT_TEXT_ES = "Por lo presente acepto que esta información proporcionada puede ser utilizada con fines de marketing y contenido de sitios web dirigidos.";
        private const string CONSENT_LONG_TEXT_EN = @"This is a sample consent declaration used for demonstration purposes only. 
                We strongly recommend forming a consent declaration suited for your website and consulting it with a lawyer.";
        private const string CONSENT_LONG_TEXT_ES = @"Esta es una declaración de consentimiento de muestra que se usa sólo para fines de demostración.
                Recomendamos encarecidamente formar una declaración de consentimiento adecuada para su sitio web y consultarla con un abogado.";


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="site">Site the form consent will be generated for</param>
        public FormConsentGenerator(ISiteInfo site, IFormBuilderConfigurationSerializer formBuilderConfigurationSerializer)
        {
            mSite = site;
            mFormBuilderConfigurationSerializer = formBuilderConfigurationSerializer;
        }


        /// <summary>
        /// Generates sample form consent data. Suitable only for Dancing Goat demo site.
        /// </summary>
        public void Generate(string formName, string formFieldName)
        {
            CreateConsent();
            UpdateForm(formName, formFieldName);
        }


        private void CreateConsent()
        {
            if (ConsentInfo.Provider.Get(CONSENT_NAME) != null)
            {
                return;
            }

            var consent = new ConsentInfo
            {
                ConsentName = CONSENT_NAME,
                ConsentDisplayName = CONSENT_DISPLAY_NAME,
            };

            consent.UpsertConsentText("en-US", CONSENT_SHORT_TEXT_EN, CONSENT_LONG_TEXT_EN);
            consent.UpsertConsentText("es-ES", CONSENT_SHORT_TEXT_ES, CONSENT_LONG_TEXT_ES);

            ConsentInfo.Provider.Set(consent);
        }


        private void UpdateForm(string formName, string formFieldName)
        {
            var formClassInfo = DataClassInfoProvider.GetDataClassInfo($"BizForm.{formName}");
            if (formClassInfo == null)
            {
                return;
            }

            var formInfo = FormHelper.GetFormInfo(formClassInfo.ClassName, true);
            if (formInfo.FieldExists(formFieldName))
            {
                return;
            }

            // Update ClassFormDefinition
            var field = CreateFormField(formFieldName);
            formInfo.AddFormItem(field);
            formClassInfo.ClassFormDefinition = formInfo.GetXmlDefinition();
            formClassInfo.Update();

            // Update Form builder JSON
            var contactUsForm = BizFormInfo.Provider.Get(formName, mSite.SiteID);
            var formBuilderConfiguration = mFormBuilderConfigurationSerializer.Deserialize(contactUsForm.FormBuilderLayout);
            formBuilderConfiguration.EditableAreas.LastOrDefault()
                                    .Sections.LastOrDefault()
                                    .Zones.LastOrDefault()
                                    .FormComponents
                                        .Add(new FormComponentConfiguration { Properties = new ConsentAgreementProperties() { Guid = field.Guid } });
            contactUsForm.FormBuilderLayout = mFormBuilderConfigurationSerializer.Serialize(formBuilderConfiguration, true);
            contactUsForm.Update();
        }


        private static FormFieldInfo CreateFormField(string formFieldName)
        {
            var field = new FormFieldInfo
            {
                Name = formFieldName,
                DataType = FieldDataType.Guid,
                System = false,
                Visible = true,
                AllowEmpty = true,
                Guid = Guid.NewGuid()
            };

            field.Settings["componentidentifier"] = ConsentAgreementComponent.IDENTIFIER;
            field.Settings[nameof(ConsentAgreementProperties.ConsentCodeName)] = CONSENT_NAME;

            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, String.Empty);
            field.SetPropertyValue(FormFieldPropertyEnum.DefaultValue, String.Empty);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, String.Empty);

            return field;
        }
    }
}