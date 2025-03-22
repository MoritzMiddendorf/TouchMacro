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
        /// Event fired when a tap should be simulated
        /// </summary>
        public event EventHandler<MacroAction>? OnTapRequested;
        
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
                    
                    _logger.LogInformation($"Executing action {i+1}/{macro.ActionCount}: Tap at ({action.X}, {action.Y})");
                    
                    // Request the tap
                    OnTapRequested?.Invoke(this, action);
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