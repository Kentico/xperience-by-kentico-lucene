//--------------------------------------------------------------------------------------------------
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
using CMS;
using CMS.Base;
using CMS.Helpers;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;

[assembly: RegisterDocumentType(Reference.CLASS_NAME, typeof(Reference))]

namespace CMS.DocumentEngine.Types.DancingGoatCore
{
	/// <summary>
	/// Represents a content item of type Reference.
	/// </summary>
	public partial class Reference : TreeNode
	{
		#region "Constants and variables"

		/// <summary>
		/// The name of the data class.
		/// </summary>
		public const string CLASS_NAME = "DancingGoatCore.Reference";


		/// <summary>
		/// The instance of the class that provides extended API for working with Reference fields.
		/// </summary>
		private readonly ReferenceFields mFields;

		#endregion


		#region "Properties"

		/// <summary>
		/// ReferenceID.
		/// </summary>
		[DatabaseIDField]
		public int ReferenceID
		{
			get
			{
				return ValidationHelper.GetInteger(GetValue("ReferenceID"), 0);
			}
			set
			{
				SetValue("ReferenceID", value);
			}
		}


		/// <summary>
		/// ReferenceDescription.
		/// </summary>
		[DatabaseField]
		public string ReferenceDescription
		{
			get
			{
				return ValidationHelper.GetString(GetValue("ReferenceDescription"), @"");
			}
			set
			{
				SetValue("ReferenceDescription", value);
			}
		}


		/// <summary>
		/// ReferenceText.
		/// </summary>
		[DatabaseField]
		public string ReferenceText
		{
			get
			{
				return ValidationHelper.GetString(GetValue("ReferenceText"), @"");
			}
			set
			{
				SetValue("ReferenceText", value);
			}
		}


		/// <summary>
		/// Gets an object that provides extended API for working with Reference fields.
		/// </summary>
		[RegisterProperty]
		public ReferenceFields Fields
		{
			get
			{
				return mFields;
			}
		}


		/// <summary>
		/// Provides extended API for working with Reference fields.
		/// </summary>
		[RegisterAllProperties]
		public partial class ReferenceFields : AbstractHierarchicalObject<ReferenceFields>
		{
			/// <summary>
			/// The content item of type Reference that is a target of the extended API.
			/// </summary>
			private readonly Reference mInstance;


			/// <summary>
			/// Initializes a new instance of the <see cref="ReferenceFields" /> class with the specified content item of type Reference.
			/// </summary>
			/// <param name="instance">The content item of type Reference that is a target of the extended API.</param>
			public ReferenceFields(Reference instance)
			{
				mInstance = instance;
			}


			/// <summary>
			/// ReferenceID.
			/// </summary>
			public int ID
			{
				get
				{
					return mInstance.ReferenceID;
				}
				set
				{
					mInstance.ReferenceID = value;
				}
			}


			/// <summary>
			/// ReferenceDescription.
			/// </summary>
			public string Description
			{
				get
				{
					return mInstance.ReferenceDescription;
				}
				set
				{
					mInstance.ReferenceDescription = value;
				}
			}


			/// <summary>
			/// ReferenceText.
			/// </summary>
			public string Text
			{
				get
				{
					return mInstance.ReferenceText;
				}
				set
				{
					mInstance.ReferenceText = value;
				}
			}


			/// <summary>
			/// ReferenceImage.
			/// </summary>
			public IEnumerable<TreeNode> Image
			{
				get
				{
					return (mInstance.LinkedContentItems["ReferenceImage"]);
				}
			}
		}

		#endregion


		#region "Constructors"

		/// <summary>
		/// Initializes a new instance of the <see cref="Reference" /> class.
		/// </summary>
		public Reference() : base(CLASS_NAME)
		{
			mFields = new ReferenceFields(this);
		}

		#endregion
	}
}