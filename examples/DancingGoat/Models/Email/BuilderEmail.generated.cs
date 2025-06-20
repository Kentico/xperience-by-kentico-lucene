﻿//--------------------------------------------------------------------------------------------------
// <auto-generated>
//
//     This code was generated by code generator tool.
//
//     To customize the code use your own partial class. For more info about how to use and customize
//     the generated code see the documentation at https://docs.xperience.io/.
//
// </auto-generated>
//--------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CMS.ContentEngine;
using CMS.EmailLibrary;

namespace DancingGoat.Models
{
	/// <summary>
	/// Represents an email of type <see cref="BuilderEmail"/>.
	/// </summary>
	[RegisterContentTypeMapping(CONTENT_TYPE_NAME)]
	public partial class BuilderEmail : IEmailFieldsSource
	{
		/// <summary>
		/// Code name of the content type.
		/// </summary>
		public const string CONTENT_TYPE_NAME = "DancingGoat.BuilderEmail";


		/// <summary>
		/// Represents system properties for an email item.
		/// </summary>
		[SystemField]
		public EmailFields SystemFields { get; set; }


		/// <summary>
		/// EmailSubject.
		/// </summary>
		public string EmailSubject { get; set; }


		/// <summary>
		/// EmailPreviewText.
		/// </summary>
		public string EmailPreviewText { get; set; }


		/// <summary>
		/// BannerLogo.
		/// </summary>
		public IEnumerable<Image> BannerLogo { get; set; }


		/// <summary>
		/// SocialPlatforms.
		/// </summary>
		public IEnumerable<SocialLink> SocialPlatforms { get; set; }
	}
}
