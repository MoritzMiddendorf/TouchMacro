using System;
using System.Collections.Generic;

namespace TouchMacro.Models
{
    /// <summary>
    /// Represents a complete macro with multiple tap actions
    /// </summary>
    public class Macro
    {
        /// <summary>
        /// Unique identifier for the macro
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Name of the macro
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Date and time when the macro was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Number of actions in this macro
        /// </summary>
        public int ActionCount { get; set; }
        
        /// <summary>
        /// List of actions that make up this macro (not stored in database directly)
        /// </summary>
        public List<MacroAction> Actions { get; set; } = new List<MacroAction>();
    }
}