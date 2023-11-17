using System;
using System.Collections.Generic;
using System.Linq;

using CMS.ContactManagement;
using CMS.EmailLibrary;

namespace DancingGoat.Helpers.Generator
{
    public class SampleEmailContext
    {
        /// <summary>
        /// Represents email guid to uniquely identify each email.
        /// </summary>
        public Guid EmailGuid { get; }
    
    
        /// <summary>
        /// A recipient of the email.
        /// </summary>
        public ContactInfo Contact { get; }
    
    
        /// <summary>
        /// A contact group the email should be send to.
        /// </summary>
        public ContactGroupInfo RecipientList { get; }

    
        /// <summary>
        /// The email configuration of the email.
        /// </summary>
        public EmailConfigurationInfo Configuration { get; }
    
    
        /// <summary>
        /// The links to be injected in the email.
        /// </summary>
        public List<EmailLinkInfo> Links { get; }


        public SampleEmailContext(EmailConfigurationInfo configuration, ContactGroupInfo recipientList, ContactInfo contact)
        {
            EmailGuid = Guid.NewGuid();
            Configuration = configuration;
            Links = new List<EmailLinkInfo>();
            RecipientList = recipientList;
            Contact = contact;
        }
    }
}