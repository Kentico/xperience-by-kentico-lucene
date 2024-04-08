using System;
using System.Linq;

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
        public const string CONSENT_NAME = "DancingGoatCoffeeSampleListForm";
        internal const string CONSENT_DISPLAY_NAME = "Dancing Goat - Coffee sample list form";

        private const string CONSENT_LONG_TEXT_EN = "<p>This is a sample consent declaration used for demonstration purposes only. "
            + "We strongly recommend forming a consent declaration suited for your website and consulting it with a lawyer.</p>";
        private const string CONSENT_SHORT_TEXT_EN = "<p>I hereby accept that these provided information can be used for marketing purposes and targeted website content.</p>";

        private const string CONSENT_LONG_TEXT_ES = "<p>Esta es una declaración de consentimiento de muestra que se usa sólo para fines de demostración. "
            + "Recomendamos encarecidamente formar una declaración de consentimiento adecuada para su sitio web y consultarla con un abogado.</p>";
        private const string CONSENT_SHORT_TEXT_ES = "<p>Por lo presente acepto que esta información proporcionada puede ser utilizada con fines de marketing y contenido de sitios web dirigidos.</p>";

        private readonly IFormBuilderConfigurationSerializer formBuilderConfigurationSerializer;
        private readonly IInfoProvider<ConsentInfo> consentInfoProvider;
        private readonly IInfoProvider<BizFormInfo> bizFormInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="FormConsentGenerator"/> class.
        /// </summary>
        /// <param name="formBuilderConfigurationSerializer">Form builder configuration serializer.</param>
        /// <param name="consentInfoProvider">Consent info provider.</param>
        /// <param name="bizFormInfoProvider">BizForm info provide.</param>
        public FormConsentGenerator(
            IFormBuilderConfigurationSerializer formBuilderConfigurationSerializer,
            IInfoProvider<ConsentInfo> consentInfoProvider,
            IInfoProvider<BizFormInfo> bizFormInfoProvider)
        {
            this.formBuilderConfigurationSerializer = formBuilderConfigurationSerializer;
            this.consentInfoProvider = consentInfoProvider;
            this.bizFormInfoProvider = bizFormInfoProvider;
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
            if (consentInfoProvider.Get(CONSENT_NAME) != null)
            {
                return;
            }

            var consent = new ConsentInfo
            {
                ConsentName = CONSENT_NAME,
                ConsentDisplayName = CONSENT_DISPLAY_NAME,
            };

            consent.UpsertConsentText("en", CONSENT_SHORT_TEXT_EN, CONSENT_LONG_TEXT_EN);
            consent.UpsertConsentText("es", CONSENT_SHORT_TEXT_ES, CONSENT_LONG_TEXT_ES);

            consentInfoProvider.Set(consent);
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
            var contactUsForm = bizFormInfoProvider.Get(formName);
            var formBuilderConfiguration = formBuilderConfigurationSerializer.Deserialize(contactUsForm.FormBuilderLayout);
            formBuilderConfiguration
                .EditableAreas.LastOrDefault()
                .Sections.LastOrDefault()
                .Zones.LastOrDefault()
                .FormComponents.Add(new FormComponentConfiguration { Properties = new ConsentAgreementProperties() { Guid = field.Guid } });
            contactUsForm.FormBuilderLayout = formBuilderConfigurationSerializer.Serialize(formBuilderConfiguration, true);
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

            field.SetPropertyValue(FormFieldPropertyEnum.FieldCaption, string.Empty);
            field.SetPropertyValue(FormFieldPropertyEnum.DefaultValue, string.Empty);
            field.SetPropertyValue(FormFieldPropertyEnum.FieldDescription, string.Empty);

            return field;
        }
    }
}
