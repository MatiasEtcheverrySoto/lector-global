using System;
using System.IO;
using System.Threading.Tasks;
using Vosk;
using NAudio.Wave;
using System.Text.Json;

namespace LectorGlobalApp
{
    public class LocalDictationManager : IDisposable
    {
        private VoskRecognizer _recognizer;
        private WaveInEvent _waveIn;
        private Model _model;
        
        private string _modelPath;
        private bool _isDictating = false;

        public event Action<string> OnPartialResult;
        public event Action<string> OnResult;
        public event Action<string> OnModelDownloadProgress;
        public event Action OnModelReady;
        public event Action<string> OnError;

        public LocalDictationManager()
        {
            Vosk.Vosk.SetLogLevel(-1); // Disable Vosk logging to console
            
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            _modelPath = Path.Combine(appFolder, "vosk-model-small-es-0.42");
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (_model == null)
                {
                    if (!Directory.Exists(_modelPath))
                    {
                        OnError?.Invoke($"Error: No se encontró el modelo en '{_modelPath}'");
                        return false;
                    }

                    _model = new Model(_modelPath);
                    OnModelReady?.Invoke();
                }
                return true;
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vosk_error.txt"), ex.ToString());
                OnError?.Invoke($"Error al inicializar la IA: {ex.Message}");
                return false;
            }
        }

        public void StartDictation()
        {
            if (_isDictating) return;
            if (_model == null)
            {
                OnError?.Invoke("El modelo aún no está listo.");
                return;
            }

            _recognizer = new VoskRecognizer(_model, 16000.0f);
            
            _waveIn = new WaveInEvent();
            _waveIn.WaveFormat = new WaveFormat(16000, 1);
            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.RecordingStopped += WaveIn_RecordingStopped;
            
            _isDictating = true;
            _waveIn.StartRecording();
        }

        public void StopDictation()
        {
            if (!_isDictating) return;
            
            _isDictating = false;
            _waveIn?.StopRecording();
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_isDictating) return;

            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                string result = _recognizer.Result();
                ProcessJsonResult(result, true);
            }
            else
            {
                string partialResult = _recognizer.PartialResult();
                ProcessJsonResult(partialResult, false);
            }
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (_recognizer != null)
            {
                string finalResult = _recognizer.FinalResult();
                ProcessJsonResult(finalResult, true);
                
                _recognizer.Dispose();
                _recognizer = null;
            }
            
            _waveIn?.Dispose();
            _waveIn = null;
        }

        private void ProcessJsonResult(string json, bool isFinal)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    if (isFinal)
                    {
                        if (doc.RootElement.TryGetProperty("text", out JsonElement textEl))
                        {
                            string text = textEl.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                OnResult?.Invoke(text);
                            }
                        }
                    }
                    else
                    {
                        if (doc.RootElement.TryGetProperty("partial", out JsonElement partialEl))
                        {
                            string partialText = partialEl.GetString();
                            if (!string.IsNullOrWhiteSpace(partialText))
                            {
                                OnPartialResult?.Invoke(partialText);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignorar errores de parseo
            }
        }

        public void Dispose()
        {
            StopDictation();
            _model?.Dispose();
        }
    }
}
