using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SQLite;
using TouchMacro.Models;

namespace TouchMacro.Services
{
    /// <summary>
    /// Service for handling SQLite database operations for storing and retrieving macros
    /// </summary>
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly ILogger<DatabaseService> _logger;
        
        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;
            
            // Get the path to the database file
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "touchmacro.db");
            _logger.LogInformation($"Database path: {dbPath}");
            
            // Create the database connection
            _database = new SQLiteAsyncConnection(dbPath);
            
            // Create the tables if they don't exist
            _database.CreateTableAsync<Macro>().Wait();
            _database.CreateTableAsync<MacroAction>().Wait();
            
            _logger.LogInformation("Database initialized successfully");
        }
        
        #region Macro Methods
        
        /// <summary>
        /// Gets all macros ordered by creation date (newest first)
        /// </summary>
        public async Task<List<Macro>> GetAllMacrosAsync()
        {
            _logger.LogInformation("Getting all macros");
            return await _database.Table<Macro>()
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }
        
        /// <summary>
        /// Gets a specific macro by ID, including all its actions
        /// </summary>
        public async Task<Macro?> GetMacroWithActionsAsync(int id)
        {
            _logger.LogInformation($"Getting macro with ID: {id}");
            
            // Get the macro
            var macro = await _database.Table<Macro>()
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();
                
            if (macro != null)
            {
                // Get the actions for this macro
                macro.Actions = await _database.Table<MacroAction>()
                    .Where(a => a.MacroId == id)
                    .OrderBy(a => a.SequenceNumber)
                    .ToListAsync();
            }
            
            return macro;
        }
        
        /// <summary>
        /// Saves a new macro with its actions
        /// </summary>
        public async Task<int> SaveMacroAsync(Macro macro)
        {
            _logger.LogInformation($"Saving macro: {macro.Name}");
            
            // Set creation date if not already set
            if (macro.CreatedAt == default)
            {
                macro.CreatedAt = DateTime.Now;
            }
            
            // Set action count
            macro.ActionCount = macro.Actions.Count;
            
            // Begin transaction
            await _database.RunInTransactionAsync(tran => {
                // Save the macro first to get its ID
                var conn = tran; // Updated from tran.Connection;
                conn.Insert(macro);
                
                // Now save each action with the macro ID
                foreach (var action in macro.Actions)
                {
                    action.MacroId = macro.Id;
                    conn.Insert(action);
                }
            });
            
            _logger.LogInformation($"Saved macro with ID: {macro.Id}");
            return macro.Id;
        }
        
        /// <summary>
        /// Updates an existing macro's name
        /// </summary>
        public async Task<int> UpdateMacroNameAsync(Macro macro)
        {
            _logger.LogInformation($"Updating macro name: {macro.Id} to {macro.Name}");
            return await _database.UpdateAsync(macro);
        }
        
        /// <summary>
        /// Deletes a macro and all its actions
        /// </summary>
        public async Task<int> DeleteMacroAsync(int id)
        {
            _logger.LogInformation($"Deleting macro: {id}");
            
            // Begin transaction
            await _database.RunInTransactionAsync(tran => {
                var conn = tran; // Updated from tran.Connection;
                
                // Delete all actions for this macro
                conn.Execute("DELETE FROM MacroAction WHERE MacroId = ?", id);
                
                // Delete the macro
                conn.Execute("DELETE FROM Macro WHERE Id = ?", id);
            });
            
            return 1;
        }
        
        #endregion
    }
}