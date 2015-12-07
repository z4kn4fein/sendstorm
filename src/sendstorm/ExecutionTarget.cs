namespace Sendstorm
{
    /// <summary>
    /// The execution target used in the subscriptions.
    /// </summary>
    public enum ExecutionTarget
    {
        /// <summary>
        /// Represents the broadcast thread execution target.
        /// </summary>
        BroadcastThread,

        /// <summary>
        /// Represents the background thread execution target.
        /// </summary>
        BackgroundThread,

        /// <summary>
        /// Represents the Ui thread execution target.
        /// </summary>
        UiThread
    }
}
