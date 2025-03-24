using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TouchMacro.Models;

namespace TouchMacro.Services
{
    /// <summary>
    /// Service for recording tap macros
    /// </summary>
    public class MacroRecorderService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<MacroRecorderService> _logger;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private List<MacroAction> _currentActions = new List<MacroAction>();
        private bool _isRecording = false;
        private int _sequenceNumber = 0;
        
        public MacroRecorderService(DatabaseService databaseService, ILogger<MacroRecorderService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets or sets whether recording is currently active
        /// </summary>
        public bool IsRecording
        {
            get => _isRecording;
            set => _isRecording = value;
        }
        
        /// <summary>
        /// Starts recording a new macro
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording)
            {
                _logger.LogWarning("Tried to start recording when already recording");
                return;
            }
            
            _logger.LogInformation("Starting macro recording");
            _currentActions = new List<MacroAction>();
            _sequenceNumber = 0;
            _stopwatch.Reset();
            _stopwatch.Start();
            _isRecording = true;
        }
        
        /// <summary>
        /// Records a tap at the specified coordinates
        /// </summary>
        public void RecordTap(float x, float y)
        {
            if (!_isRecording)
            {
                _logger.LogWarning("Tried to record tap when not recording");
                return;
            }
            
            // Calculate time since last action
            long delayMs = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            
            _logger.LogInformation($"Recording tap at ({x}, {y}) with delay {delayMs}ms");
            
            // Create and add the action
            var action = new MacroAction
            {
                X = x,
                Y = y,
                DelayMs = delayMs,
                SequenceNumber = _sequenceNumber++
            };
            
            _currentActions.Add(action);
        }
        
        /// <summary>
        /// Stops recording and saves the macro with the given name
        /// </summary>
        public async Task<int> StopRecordingAndSaveAsync(string name)
        {
            if (!_isRecording)
            {
                _logger.LogWarning("Tried to stop recording when not recording");
                return -1;
            }
            
            _logger.LogInformation($"Stopping recording with {_currentActions.Count} actions");
            _stopwatch.Stop();
            _isRecording = false;
            
            // If no actions were recorded, return
            if (_currentActions.Count == 0)
            {
                _logger.LogWarning("No actions were recorded");
                return -1;
            }
            
            // Create and save the macro
            var macro = new Macro
            {
                Name = name,
                CreatedAt = DateTime.Now,
                ActionCount = _currentActions.Count,
                Actions = _currentActions
            };
            
            return await _databaseService.SaveMacroAsync(macro);
        }
        
        /// <summary>
        /// Cancels the current recording without saving
        /// </summary>
        public void CancelRecording()
        {
            if (!_isRecording)
            {
                return;
            }
            
            _logger.LogInformation("Canceling recording");
            _stopwatch.Stop();
            _isRecording = false;
            _currentActions.Clear();
        }
    }
}