using CMS.Base;
using CMS.DataEngine;
using CMS.DataProtection;

namespace DancingGoat.Helpers.Generator
{
    public class TrackingConsentGenerator
    {
        private readonly ISiteInfo mSite;

        internal const string CONSENT_NAME = "DancingGoatTracking";
        private const string CONSENT_DISPLAY_NAME = "Dancing Goat - Tracking";
        private const string CONSENT_SHORT_TEXT_EN = @"At Dancing Goat, we have exciting offers and news about our products and services 
                that we hope you'd like to hear about. To present you the offers that suit you the most, we need to know a few personal 
                details about you. We will gather some of your activities on our website (such as which pages you've visited, etc.) and 
                use them to personalize the website content and improve analytics about our visitors. In addition, we will store small 
                piecies of data in your browser cookies. We promise we will treat your data with respect, store it in a secured storage, 
                and won't release it to any third parties.";

        private const string CONSENT_SHORT_TEXT_ES = @"En Dancing Goat, tenemos noticias y ofertas interesantes sobre nuestros productos y 
                servicios de los que esperamos que le gustaría escuchar. Para presentarle las ofertas que más le convengan, necesitamos 
                conocer algunos detalles personales sobre usted. Reuniremos algunas de sus actividades en nuestro sitio web (como las 
                páginas que visitó, etc.) y las usaremos para personalizar el contenido del sitio web y mejorar el análisis de nuestros 
                visitantes. Además, almacenaremos pequeñas cantidades de datos en las cookies del navegador. Nos comprometemos a tratar 
                sus datos con respeto, almacenarlo en un almacenamiento seguro, y no lo lanzará a terceros.";

        private const string CONSENT_LONG_TEXT_EN = @"This is a sample consent declaration used for demonstration purposes only. 
                We strongly recommend forming a consent declaration suited for your website and consulting it with a lawyer.";
        private const string CONSENT_LONG_TEXT_ES = @"Esta es una declaración de consentimiento de muestra que se usa sólo para fines de demostración.
                Recomendamos encarecidamente formar una declaración de consentimiento adecuada para su sitio web y consultarla con un abogado.";


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="site">Site the data protection sample data will be generated for.</param>
        public TrackingConsentGenerator(ISiteInfo site)
        {
            mSite = site;
        }


        public void Generate()
        {
            CreateConsent();
            SetDefaultCookieLevel(mSite.SiteID);
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


        private void SetDefaultCookieLevel(int siteID)
        {
            SettingsKeyInfoProvider.SetValue("CMSDefaultCookieLevel", siteID, "essential");
        }
    }
}