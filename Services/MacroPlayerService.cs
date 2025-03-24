using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TouchMacro.Models;

namespace TouchMacro.Services
{
    /// <summary>
    /// Service for playing back (simulating) recorded macros
    /// </summary>
    public class MacroPlayerService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<MacroPlayerService> _logger;
        private CancellationTokenSource? _cts;
        private bool _isPlaying = false;
        
        public MacroPlayerService(DatabaseService databaseService, ILogger<MacroPlayerService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets whether playback is currently active
        /// </summary>
        public bool IsPlaying => _isPlaying;
        
        /// <summary>
        /// Event fired when an action should be executed
        /// </summary>
        public event EventHandler<(MacroAction Current, MacroAction? Previous)>? OnActionRequested;
        
        /// <summary>
        /// Event fired when playback starts
        /// </summary>
        public event EventHandler? PlaybackStarted;
        
        /// <summary>
        /// Event fired when playback stops
        /// </summary>
        public event EventHandler? PlaybackStopped;
        
        /// <summary>
        /// Plays a macro with the given ID
        /// </summary>
        public async Task PlayMacroAsync(int macroId)
        {
            if (_isPlaying)
            {
                _logger.LogWarning("Tried to play macro when already playing");
                return;
            }
            
            try
            {
                // Load the macro with all its actions
                var macro = await _databaseService.GetMacroWithActionsAsync(macroId);
                if (macro == null || macro.Actions.Count == 0)
                {
                    _logger.LogWarning($"Macro with ID {macroId} not found or has no actions");
                    return;
                }
                
                _logger.LogInformation($"Starting playback of macro '{macro.Name}' with {macro.ActionCount} actions");
                
                _isPlaying = true;
                _cts = new CancellationTokenSource();
                
                PlaybackStarted?.Invoke(this, EventArgs.Empty);
                
                MacroAction? previousAction = null;
                
                // Play each action with the appropriate delay
                for (int i = 0; i < macro.Actions.Count; i++)
                {
                    var action = macro.Actions[i];
                    
                    // Wait for the delay time
                    if (action.DelayMs > 0)
                    {
                        await Task.Delay((int)action.DelayMs, _cts.Token);
                    }
                    
                    // Check if playback was cancelled
                    if (_cts.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    _logger.LogInformation($"Executing action {i+1}/{macro.ActionCount}: {action.ActionType} at ({action.X}, {action.Y})");
                    
                    // Request the action execution
                    OnActionRequested?.Invoke(this, (action, previousAction));
                    
                    // Special handling for drag segments - may need additional delay
                    if (action.ActionType == ActionType.DragMove || action.ActionType == ActionType.DragEnd)
                    {
                        // Add a small delay to ensure the drag motion completes
                        await Task.Delay(Math.Max(50, (int)action.DurationMs / 5), _cts.Token);
                    }
                    
                    // Update previous action for the next iteration
                    previousAction = action;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Playback was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during macro playback");
            }
            finally
            {
                _isPlaying = false;
                PlaybackStopped?.Invoke(this, EventArgs.Empty);
            }
        }
        
        /// <summary>
        /// Stops the current playback
        /// </summary>
        public void StopPlayback()
        {
            if (!_isPlaying)
            {
                return;
            }
            
            _logger.LogInformation("Stopping macro playback");
            _cts?.Cancel();
        }
    }
}