using System;
using System.Linq;
using System.Threading.Tasks;

using CMS.EmailLibrary;

namespace DancingGoat.Helpers.Generator
{
    public static  class DancingGoatEmailLibrarySampleGeneratorExtensions
    {
        private static readonly Guid AfterSubscriptionConfigurationPageGuid = Guid.Parse("F43AE10A-DA93-4A2F-99A6-D1B5E60AFBE6");
        private static readonly Guid AfterUnsubscriptionConfigurationPageGuid = Guid.Parse("9CC38A16-F679-4C51-AE27-F5176A6849B0");


        /// <summary>
        /// Generates an email sample for testing and debugging the Dancing Goat site.
        /// </summary>
        public static async Task GenerateDancingGoatMailSample(this EmailLibrarySampleGenerator generator)
        {
            var contactsThatSubmittedSubscriptionForm = generator.GenerateContacts(47).ToList();
            var contactsThatConfirmedSubscription = contactsThatSubmittedSubscriptionForm.SkipLast(2).ToList();

            var activeContacts = contactsThatConfirmedSubscription.Take(30).ToList();
            var softBouncedContacts = contactsThatConfirmedSubscription.Skip(30).Take(10).ToList();
            var hardBouncedContacts = contactsThatConfirmedSubscription.Skip(40).ToList();

            var autoresponderTemplate = await generator.GenerateTemplateAsync(EmailType.FormAutoresponder);
            var confirmationTemplate = await generator.GenerateTemplateAsync(EmailType.Confirmation);
            var regularTemplate = await generator.GenerateTemplateAsync(EmailType.Regular);

            var subscriptionFormAutoresponder = await generator.GenerateConfigurationAsync(autoresponderTemplate);
            var subscriptionConfirmation = await generator.GenerateConfigurationAsync(confirmationTemplate, "Subscription Confirmation");
            var unsubscriptionConfirmation = await generator.GenerateConfigurationAsync(confirmationTemplate, "Unsubscription Confirmation");
            var regularEmail = await generator.GenerateConfigurationAsync(regularTemplate);
            
            var recipientList = await generator.GenerateRecipientListAsync(subscriptionConfirmation, AfterSubscriptionConfigurationPageGuid,
                unsubscriptionConfirmation, AfterUnsubscriptionConfigurationPageGuid);

            var autoresponderEmails = await generator.SimulateEmailSend(subscriptionFormAutoresponder, contactsThatSubmittedSubscriptionForm, recipientList);
            
            // It's intentionally called twice with slightly different numbers to create non-unique hits.
            generator.GenerateEmailStatisticsHits(autoresponderEmails.SkipLast(1), autoresponderEmails.SkipLast(2));
            generator.GenerateEmailStatisticsHits(autoresponderEmails.SkipLast(2), autoresponderEmails.SkipLast(3));
            
            generator.AddContactsToRecipientList(contactsThatConfirmedSubscription, recipientList);

            var confirmationEmails = await generator.SimulateEmailSend(subscriptionConfirmation, contactsThatConfirmedSubscription, recipientList);
            generator.GenerateEmailStatisticsHits(confirmationEmails.SkipLast(1));
            generator.GenerateEmailStatisticsHits(confirmationEmails.SkipLast(2));

            var deliveredRegularEmails = await generator.SimulateEmailSend(regularEmail, activeContacts, recipientList);
            generator.GenerateEmailStatisticsHits(deliveredRegularEmails.SkipLast(1).Concat(deliveredRegularEmails.SkipLast(2)),
                deliveredRegularEmails.SkipLast(2).Concat(deliveredRegularEmails.SkipLast(3)));
            generator.GenerateEmailStatisticsHits(deliveredRegularEmails.SkipLast(2).Concat(deliveredRegularEmails.SkipLast(3)),
                deliveredRegularEmails.SkipLast(3).Concat(deliveredRegularEmails.SkipLast(4)));

            // Generate sent hits for soft bounced contacts and no hits for hard bounced
            await generator.SimulateEmailSend(regularEmail, softBouncedContacts, recipientList);
            await generator.SimulateEmailSend(regularEmail, hardBouncedContacts, recipientList);
            generator.GenerateSoftEmailBounces(regularEmail, softBouncedContacts);
            generator.GenerateHardEmailBounces(regularEmail, hardBouncedContacts);
        }
    }
}
