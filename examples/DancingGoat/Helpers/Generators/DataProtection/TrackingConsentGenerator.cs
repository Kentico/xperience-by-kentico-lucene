using CMS.DataEngine;
using CMS.DataProtection;

namespace DancingGoat.Helpers.Generator
{
    public class TrackingConsentGenerator
    {
        internal const string CONSENT_NAME = "DancingGoatTracking";
        private const string CONSENT_DISPLAY_NAME = "Dancing Goat - Tracking";

        private const string CONSENT_LONG_TEXT_EN = "<p>This is a sample consent declaration used for demonstration purposes only. "
                + "We strongly recommend forming a consent declaration suited for your website and consulting it with a lawyer.</p>";
        private const string CONSENT_SHORT_TEXT_EN = "<p>At Dancing Goat, we have exciting offers and news about our products and services "
                + "that we hope you'd like to hear about. To present you the offers that suit you the most, we need to know a few personal "
                + "details about you. We will gather some of your activities on our website (such as which pages you've visited, etc.) and "
                + "use them to personalize the website content and improve analytics about our visitors. In addition, we will store small "
                + "pieces of data in your browser cookies. We promise we will treat your data with respect, store it in a secured storage, "
                + "and won't release it to any third parties.</p>";

        private const string CONSENT_LONG_TEXT_ES = "<p>Esta es una declaración de consentimiento de muestra que se usa sólo para fines de demostración. "
                + "Recomendamos encarecidamente formar una declaración de consentimiento adecuada para su sitio web y consultarla con un abogado.</p>";
        private const string CONSENT_SHORT_TEXT_ES = "<p>En Dancing Goat, tenemos noticias y ofertas interesantes sobre nuestros productos y "
                + "servicios de los que esperamos que le gustaría escuchar. Para presentarle las ofertas que más le convengan, necesitamos "
                + "conocer algunos detalles personales sobre usted. Reuniremos algunas de sus actividades en nuestro sitio web (como las "
                + "páginas que visitó, etc.) y las usaremos para personalizar el contenido del sitio web y mejorar el análisis de nuestros "
                + "visitantes. Además, almacenaremos pequeñas cantidades de datos en las cookies del navegador. Nos comprometemos a tratar "
                + "sus datos con respeto, almacenarlo en un almacenamiento seguro, y no lo lanzará a terceros.</p>";

        private readonly IInfoProvider<ConsentInfo> consentInfoProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingConsentGenerator"/> class.
        /// </summary>
        /// <param name="consentInfoProvider">Consent info provider.</param>
        public TrackingConsentGenerator(IInfoProvider<ConsentInfo> consentInfoProvider)
        {
            this.consentInfoProvider = consentInfoProvider;
        }


        public void Generate()
        {
            CreateConsent();
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
    }
}
