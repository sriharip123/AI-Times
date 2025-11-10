namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Defines the execution mode for the application
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>
        /// Normal mode - process JSON input
        /// </summary>
        Normal,

        /// <summary>
        /// Diagnostic mode - run diagnostic commands
        /// </summary>
        Diagnostic,

        /// <summary>
        /// Help mode - display help information
        /// </summary>
        Help
    }
}
