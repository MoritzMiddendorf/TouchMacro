using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TouchMacro.Models;

namespace TouchMacro.Services
{
    /// <summary>
    /// Service for recording tap and drag macros
    /// </summary>
    public class MacroRecorderService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<MacroRecorderService> _logger;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Stopwatch _dragDurationStopwatch = new Stopwatch();
        private List<MacroAction> _currentActions = new List<MacroAction>();
        private bool _isRecording = false;
        private int _sequenceNumber = 0;
        private bool _isDragging = false;
        private float _lastDragX = 0;
        private float _lastDragY = 0;
        
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
            _isDragging = false;
        }
        
        /// <summary>
        /// Records a touch down event (beginning of tap or drag)
        /// </summary>
        public void RecordTouchDown(float x, float y)
        {
            if (!_isRecording)
            {
                _logger.LogWarning("Tried to record touch down when not recording");
                return;
            }
            
            // Calculate time since last action
            long delayMs = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            
            // Start tracking for possible drag
            _isDragging = true;
            _lastDragX = x;
            _lastDragY = y;
            _dragDurationStopwatch.Reset();
            _dragDurationStopwatch.Start();
            
            _logger.LogInformation($"Recording touch down at ({x}, {y}) with delay {delayMs}ms");
            
            // Create and add the action as a drag start
            var action = new MacroAction
            {
                X = x,
                Y = y,
                DelayMs = delayMs,
                SequenceNumber = _sequenceNumber++,
                ActionType = ActionType.DragStart
            };
            
            _currentActions.Add(action);
        }
        
        /// <summary>
        /// Records a touch move event (drag)
        /// </summary>
        public void RecordTouchMove(float x, float y)
        {
            if (!_isRecording || !_isDragging)
            {
                return;
            }
            
            // Only record if we've moved a significant distance (avoid recording tiny movements)
            float distanceX = Math.Abs(x - _lastDragX);
            float distanceY = Math.Abs(y - _lastDragY);
            
            if (distanceX > 5 || distanceY > 5)
            {
                // Calculate time since last action
                long delayMs = _stopwatch.ElapsedMilliseconds;
                _stopwatch.Restart();
                
                _logger.LogInformation($"Recording drag move to ({x}, {y}) with delay {delayMs}ms");
                
                // Create and add the action as a drag move
                var action = new MacroAction
                {
                    X = x,
                    Y = y,
                    DelayMs = delayMs,
                    SequenceNumber = _sequenceNumber++,
                    ActionType = ActionType.DragMove
                };
                
                _currentActions.Add(action);
                
                // Update last position
                _lastDragX = x;
                _lastDragY = y;
            }
        }
        
        /// <summary>
        /// Records a touch up event (end of tap or drag)
        /// </summary>
        public void RecordTouchUp(float x, float y)
        {
            if (!_isRecording)
            {
                _logger.LogWarning("Tried to record touch up when not recording");
                return;
            }
            
            // Calculate time since last action
            long delayMs = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            
            // Determine if this was a tap or a drag
            bool wasDragging = _isDragging;
            _isDragging = false;
            _dragDurationStopwatch.Stop();
            long durationMs = _dragDurationStopwatch.ElapsedMilliseconds;
            
            // Check if any drag moves were recorded (more than just start and end)
            bool hadDragMoves = false;
            if (wasDragging && _currentActions.Count >= 1)
            {
                // Check if we recorded any drag moves between start and end
                var lastAction = _currentActions[_currentActions.Count - 1];
                hadDragMoves = lastAction.ActionType == ActionType.DragMove;
            }
            
            if (wasDragging && (hadDragMoves || durationMs > 200))
            {
                // This was a drag
                _logger.LogInformation($"Recording drag end at ({x}, {y}) with delay {delayMs}ms, duration {durationMs}ms");
                
                // Create and add the action as a drag end
                var action = new MacroAction
                {
                    X = x,
                    Y = y,
                    DelayMs = delayMs,
                    SequenceNumber = _sequenceNumber++,
                    ActionType = ActionType.DragEnd,
                    DurationMs = durationMs
                };
                
                _currentActions.Add(action);
            }
            else
            {
                // This was a tap - convert the previous DragStart to a Tap
                if (_currentActions.Count > 0)
                {
                    var startAction = _currentActions[_currentActions.Count - 1];
                    if (startAction.ActionType == ActionType.DragStart)
                    {
                        startAction.ActionType = ActionType.Tap;
                        _logger.LogInformation($"Converted drag start to tap at ({startAction.X}, {startAction.Y})");
                    }
                    else
                    {
                        // Add as a tap action
                        _logger.LogInformation($"Recording tap at ({x}, {y}) with delay {delayMs}ms");
                        
                        var action = new MacroAction
                        {
                            X = x,
                            Y = y,
                            DelayMs = delayMs,
                            SequenceNumber = _sequenceNumber++,
                            ActionType = ActionType.Tap
                        };
                        
                        _currentActions.Add(action);
                    }
                }
            }
        }
        
        /// <summary>
        /// Records a simple tap (convenience method for touch down+up)
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
                SequenceNumber = _sequenceNumber++,
                ActionType = ActionType.Tap
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
            _dragDurationStopwatch.Stop();
            _isRecording = false;
            _isDragging = false;
            
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
            _dragDurationStopwatch.Stop();
            _isRecording = false;
            _isDragging = false;
            _currentActions.Clear();
        }
    }
}