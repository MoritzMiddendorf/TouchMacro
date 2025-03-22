using System;

namespace TouchMacro.Models
{
    /// <summary>
    /// Represents a single tap action in a macro
    /// </summary>
    public class MacroAction
    {
        /// <summary>
        /// Unique identifier for the action
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The ID of the macro this action belongs to
        /// </summary>
        public int MacroId { get; set; }
        
        /// <summary>
        /// X-coordinate of the tap
        /// </summary>
        public float X { get; set; }
        
        /// <summary>
        /// Y-coordinate of the tap
        /// </summary>
        public float Y { get; set; }
        
        /// <summary>
        /// Time in milliseconds that has elapsed since the previous action
        /// </summary>
        public long DelayMs { get; set; }
        
        /// <summary>
        /// Sequence number of this action within the macro
        /// </summary>
        public int SequenceNumber { get; set; }
    }
}