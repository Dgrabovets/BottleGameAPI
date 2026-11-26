namespace BottleGame.Application.Common
{
    /// <summary>
    /// Sets the text of the log prefix of a class instead of the one made from the class name
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class LogPrefixAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// Text of the prefix without brackets
        /// </summary>
        public string Text { get; }

        #endregion Properties

        /// <summary>
        /// Initializes a new instance of <see cref="LogPrefixAttribute"/>
        /// </summary>
        /// <param name="text">Text of the prefix without brackets</param>
        public LogPrefixAttribute(string text)
        {
            Text = text;
        }
    }
}
