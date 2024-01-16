using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using CMS.Base;
using CMS.DataEngine;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Class used to transform data into human readable format.
    /// </summary>
    internal sealed class HumanReadablePersonalDataWriter : IPersonalDataWriter
    {
        private static readonly string DECIMAL_PRECISION = new string('#', 26);
        private static readonly string DECIMAL_FORMAT = "{0:0.00" + DECIMAL_PRECISION + "}";

        private readonly StringBuilder stringBuilder;
        private int indentationLevel;
        private bool ignoreNewLine;


        /// <summary>
        /// Culture used to format values.
        /// </summary>
        public CultureInfo Culture { get; set; } = new CultureInfo(SystemContext.SYSTEM_CULTURE_NAME);


        /// <summary>
        /// Initializes instance of <see cref="HumanReadablePersonalDataWriter"/>
        /// </summary>
        public HumanReadablePersonalDataWriter()
        {
            stringBuilder = new StringBuilder();
            indentationLevel = 0;
            ignoreNewLine = false;
        }


        /// <summary>
        /// Writes start of a section using <paramref name="sectionDisplayName"/> followed by colon and increments indentation.
        /// </summary>
        /// <param name="sectionName">Name of the section in machine readable format. This parameter is ignored.</param>
        /// <param name="sectionDisplayName">Name of the section in human readable format.</param>
        public void WriteStartSection(string sectionName, string sectionDisplayName)
        {
            ignoreNewLine = false;
            Indent();

            stringBuilder.AppendLine(sectionDisplayName + ": ");
            indentationLevel++;
        }


        /// <summary>
        /// Writes appropriate indentation.
        /// </summary>
        private void Indent()
        {
            stringBuilder.Append('\t', indentationLevel);
        }


        /// <summary>
        /// Writes data out of <paramref name="baseInfo"/>'s specified <paramref name="columns"/>.
        /// </summary>
        /// <remarks>
        /// Omits columns with missing human readable name, ID and GUID columns.
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

            foreach (var column in columns)
            {
                var columnName = column.Name;
                var columnDisplayName = column.DisplayName;
                if (string.IsNullOrWhiteSpace(columnDisplayName) || columnName.Equals(baseInfo.TypeInfo.IDColumn, StringComparison.Ordinal) || columnName.Equals(baseInfo.TypeInfo.GUIDColumn, StringComparison.Ordinal))
                {
                    continue;
                }

                object value = baseInfo.GetValue(columnName);
                if (value == null)
                {
                    continue;
                }

                if (valueTransformationFunction != null)
                {
                    value = valueTransformationFunction(columnName, value);
                }

                WriteKeyValue(columnDisplayName, value);
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
            Indent();
            stringBuilder.AppendFormat("{0}: {1}", sectionDisplayName, value);
            stringBuilder.AppendLine();
        }


        /// <summary>
        /// Writes in-line key name followed by column and value.
        /// </summary>
        /// <param name="keyName">Key name to write.</param>
        /// <param name="value">Value associated with key.</param>
        private void WriteKeyValue(string keyName, object value)
        {
            Indent();
            stringBuilder.AppendFormat("{0}: ", keyName);

            string format = "{0}";

            if (value is decimal)
            {
                format = DECIMAL_FORMAT;
            }

            stringBuilder.AppendFormat(Culture, format, value);
            stringBuilder.AppendLine();

            ignoreNewLine = true;
        }


        /// <summary>
        /// Ends section with new line, decreases indentation level.
        /// </summary>
        /// <remarks>
        /// Does not write new line if the last written character was new line.
        /// </remarks>
        public void WriteEndSection()
        {
            indentationLevel--;
            if (!ignoreNewLine)
            {
                Indent();
                stringBuilder.AppendLine();
                ignoreNewLine = true;
            }
        }


        /// <summary>
        /// Gets result of previous write calls.
        /// </summary>
        /// <returns>String containing formatted data.</returns>
        public string GetResult()
        {
            return stringBuilder.ToString();
        }


        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="HumanReadablePersonalDataWriter"/> class.
        /// </summary>
        /// <remarks>
        /// This is a void action in <see cref="HumanReadablePersonalDataWriter"/> as the class uses no disposable resources.
        /// </remarks>
        public void Dispose()
        {
        }
    }
}
