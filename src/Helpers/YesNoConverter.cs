﻿using System;
using System.ComponentModel;
using System.Globalization;

namespace Salaros.Configuration
{
    public class YesNoConverter : BooleanConverter
    {
        protected readonly string yes, no;

        /// <summary>
        /// Initializes a new instance of the <see cref="YesNoConverter" /> class.
        /// </summary>
        /// <param name="yes">The yes.</param>
        /// <param name="no">The no.</param>
        /// <exception cref="ArgumentException">
        /// yes
        /// or
        /// no
        /// or
        /// </exception>
        /// <inheritdoc />
        public YesNoConverter(string yes = "yes", string no = "no")
        {
            if (string.IsNullOrWhiteSpace(yes)) throw new ArgumentException(nameof(yes));
            if (string.IsNullOrWhiteSpace(no)) throw new ArgumentException(nameof(no));
            if (Equals(yes, no))
                throw new ArgumentException($"Yes ({yes}) and No ({no}) values must be two different values!");

            this.yes = yes;
            this.no = no;
        }

        /// <inheritdoc />
        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. If <see langword="null" /> is passed, the current culture is assumed.</param>
        /// <param name="value">The <see cref="object" /> to convert.</param>
        /// <param name="destinationType">The <see cref="Type" /> to convert the <paramref name="value" /> parameter to.</param>
        /// <returns>
        /// An <see cref="object" /> that represents the converted value.
        /// </returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return value is bool boolean && destinationType == typeof(string)
                ? boolean ? yes : no
                : base.ConvertTo(context, culture, value, destinationType);
        }

        /// <inheritdoc />
        /// <summary>
        /// Converts the given value object to a Boolean object.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="culture">A <see cref="CultureInfo" /> that specifies the culture to which to convert.</param>
        /// <param name="value">The <see cref="object" /> to convert.</param>
        /// <returns>
        /// An <see cref="object" /> that represents the converted <paramref name="value" />.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var booleanText = value as string;
            if (no.Equals(booleanText, StringComparison.OrdinalIgnoreCase))
                return false;

            if (yes.Equals(booleanText, StringComparison.OrdinalIgnoreCase))
                return true;

            return null;
        }

        /// <summary>
        /// Gets the value of Yes / True.
        /// </summary>
        /// <value>
        /// The yes.
        /// </value>
        public string Yes => yes;

        /// <summary>
        /// Gets the value of No / False.
        /// </summary>
        /// <value>
        /// The no.
        /// </value>
        public string No => no;
    }
}
