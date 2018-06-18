using System;
using System.ComponentModel;
using System.Globalization;

// ReSharper disable once CheckNamespace

namespace Salaros.Config
{
    public class YesNoConverter : BooleanConverter
    {
        protected readonly string yes, no;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Salaros.Config.YesNoConverter" /> class.
        /// </summary>
        /// <param name="yes">The yes.</param>
        /// <param name="no">The no.</param>
        public YesNoConverter(string yes = "yes", string no = "no")
        {
            this.yes = yes;
            this.no = no;
        }

        /// <inheritdoc />
        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" />. If <see langword="null" /> is passed, the current culture is assumed.</param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert.</param>
        /// <param name="destinationType">The <see cref="T:System.Type" /> to convert the <paramref name="value" /> parameter to.</param>
        /// <returns>
        /// An <see cref="T:System.Object" /> that represents the converted value.
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
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" /> that specifies the culture to which to convert.</param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert.</param>
        /// <returns>
        /// An <see cref="T:System.Object" /> that represents the converted <paramref name="value" />.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var booleanText = value as string;
            return no.Equals(booleanText, StringComparison.OrdinalIgnoreCase) 
                ? false 
                : yes.Equals(booleanText, StringComparison.OrdinalIgnoreCase)
                ? true 
                : base.ConvertFrom(context, culture, value);
        }
    }
}
