using System;
using SQLite;

namespace TouchMacro.Models
{
    /// <summary>
    /// Action types that can be recorded and played back
    /// </summary>
    public enum ActionType
    {
        Tap = 0,
        DragStart = 1,
        DragMove = 2,
        DragEnd = 3
    }

    /// <summary>
    /// Represents a single action in a macro (tap or drag)
    /// </summary>
    [Table("MacroAction")]
    public class MacroAction
    {
        /// <summary>
        /// Unique identifier for the action
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// The ID of the macro this action belongs to
        /// </summary>
        [Indexed]
        public int MacroId { get; set; }
        
        /// <summary>
        /// X-coordinate of the action
        /// </summary>
        public float X { get; set; }
        
        /// <summary>
        /// Y-coordinate of the action
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
        
        /// <summary>
        /// Type of action (tap, drag start, drag move, drag end)
        /// </summary>
        public ActionType ActionType { get; set; } = ActionType.Tap;
        
        /// <summary>
        /// Duration in milliseconds (for drag actions)
        /// </summary>
        public long DurationMs { get; set; } = 0;
    }
}