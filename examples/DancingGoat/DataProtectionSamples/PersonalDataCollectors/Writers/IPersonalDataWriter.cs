using System;
using System.Collections.Generic;

using CMS.DataEngine;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Defines interface of a writer used to format personal data.
    /// </summary>
    internal interface IPersonalDataWriter : IDisposable
    {
        /// <summary>
        /// Writes start of a section.
        /// </summary>
        /// <param name="sectionName">Name of the section in machine readable format.</param>
        /// <param name="sectionDisplayName">Name of the section in human readable format.</param>
        void WriteStartSection(string sectionName, string sectionDisplayName);


        /// <summary>
        /// Writes data out of <paramref name="baseInfo"/>'s specified <paramref name="columns"/>.
        /// </summary>
        /// <param name="baseInfo"><see cref="BaseInfo"/> instance to write.</param>
        /// <param name="columns"><paramref name="baseInfo"/>'s columns to write data from.</param>
        /// <param name="valueTransformationFunction ">Use this function to transform values from database.</param>
        void WriteBaseInfo(BaseInfo baseInfo, List<CollectedColumn> columns, Func<string, object, object> valueTransformationFunction = null);


        /// <summary>
        /// Writes section with value on one line, enclosed in start and end section elements.
        /// </summary>
        /// <param name="sectionName">Name of the section in machine readable format.</param>
        /// <param name="sectionDisplayName">Name of the section in human readable format.</param>
        /// <param name="value">Value of element.</param>
        void WriteSectionValue(string sectionName, string sectionDisplayName, string value);


        /// <summary>
        /// Writes end of a section.
        /// </summary>
        void WriteEndSection();


        /// <summary>
        /// Gets result of previous write calls.
        /// </summary>
        /// <returns>String containing formatted data.</returns>
        string GetResult();
    }
}
