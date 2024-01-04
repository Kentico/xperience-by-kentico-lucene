using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using CMS.DataEngine;
using CMS.Helpers;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Writer used to transform data into XML format.
    /// </summary>
    internal sealed class XmlPersonalDataWriter : IPersonalDataWriter
    {
        private readonly StringBuilder stringBuilder;
        private readonly XmlWriter xmlWriter;


        /// <summary>
        /// Initializes instance of <see cref="XmlPersonalDataWriter"/>
        /// </summary>
        public XmlPersonalDataWriter()
        {
            stringBuilder = new StringBuilder();
            xmlWriter = XmlWriter.Create(stringBuilder, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true });
        }


        /// <summary>
        /// Writes XML start tag.
        /// </summary>
        /// <param name="sectionName">Name of the section in machine readable format. Represents name of the tag.</param>
        /// <param name="sectionDisplayName">Name of the section in human readable format. This parameter is ignored.</param>
        public void WriteStartSection(string sectionName, string sectionDisplayName)
        {
            xmlWriter.WriteStartElement(TransformElementName(sectionName));
        }


        /// <summary>
        /// Replaces underscore in object names with dot.
        /// </summary>
        /// <param name="originalName">Name to transform.</param>
        /// <returns>Transformed name.</returns>
        private string TransformElementName(string originalName)
        {
            return originalName.Replace('.', '_');
        }


        /// <summary>
        /// Writes data out of <paramref name="baseInfo"/>'s specified <paramref name="columns"/>.
        /// </summary>
        /// <remarks>
        /// Omits columns with missing human readable name.
        /// </remarks>
        /// <param name="baseInfo"><see cref="BaseInfo"/> instance to write.</param>
        /// <param name="columns">Columns to write <paramref name="baseInfo"/>'s data from.</param>
        /// <param name="valueTransformationFunction">Use this parameter to transform values from database.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseInfo"/> is null.</exception>
        public void WriteBaseInfo(BaseInfo baseInfo, List<CollectedColumn> columns, Func<string, object, object> valueTransformationFunction = null)
        {
            if (baseInfo == null)
            {
                throw new ArgumentNullException(nameof(baseInfo));
            }

            foreach (var columnTuple in columns)
            {
                var columnName = columnTuple.Name;
                if (string.IsNullOrWhiteSpace(columnTuple.DisplayName))
                {
                    continue;
                }

                var value = baseInfo.GetValue(columnName);
                if (value == null)
                {
                    continue;
                }

                if (valueTransformationFunction != null)
                {
                    value = valueTransformationFunction(columnName, value);
                }

                xmlWriter.WriteStartElement(columnName);
                xmlWriter.WriteValue(XmlHelper.ConvertToString(value, value.GetType()));
                xmlWriter.WriteEndElement();
            }
        }


        /// <summary>
        /// Writes section with value on one line, enclosed in start and end section elements.
        /// </summary>
        /// <param name="sectionName">Name of the section in machine readable format.</param>
        /// <param name="sectionDisplayName">Name of the section in human readable format.</param>
        /// <param name="value">Value of element.</param>
        public void WriteSectionValue(string sectionName, string sectionDisplayName, string value)
        {
            xmlWriter.WriteStartElement(sectionName);
            xmlWriter.WriteString(value);
            xmlWriter.WriteEndElement();
        }


        /// <summary>
        /// Writes XML end tag.
        /// </summary>
        public void WriteEndSection()
        {
            xmlWriter.WriteEndElement();
        }


        /// <summary>
        /// Gets result of previous write calls.
        /// </summary>
        /// <returns>XML string containing formatted data.</returns>
        public string GetResult()
        {
            xmlWriter.Flush();

            return stringBuilder.ToString();
        }


        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="XmlPersonalDataWriter"/> class.
        /// </summary>
        public void Dispose()
        {
            xmlWriter.Dispose();
        }
    }
}
