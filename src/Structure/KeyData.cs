namespace Salaros.Config.Ini
{
    /// <summary>
    /// This class is used to represent an abstract key data.
    /// Collections of KeyData entries can be used to store default settings
    /// of the application.
    /// </summary>
    public class KeyData<TValue> : IIniKeyValuePair<TValue>
    {
        public KeyData(string sectionName, string keyName, TValue defaultValue)
        {
            SectionName = sectionName;
            Key = keyName;
            Value = defaultValue;
        }

        #region IIniKeyValuePair implementation

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public TValue Value
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of the section.
        /// </summary>
        /// <value>The name of the section.</value>
        public string SectionName
        {
            get;
            private set;
        }

        #endregion
    }
}

