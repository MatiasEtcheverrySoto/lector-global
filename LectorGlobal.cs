using System;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LectorGlobal
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new HiddenContext());
        }
    }

    class HiddenContext : ApplicationContext
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private SpeechSynthesizer synth;
        private DummyForm form;

        const int MOD_CTRL = 0x0002;
        const int MOD_WIN = 0x0008;
        const int WM_HOTKEY = 0x0312;
        
        // Cambio a ruta relativa usando Environment.CurrentDirectory
        string logFile = Path.Combine(Environment.CurrentDirectory, "lector_log.txt");
        string speedFile = Path.Combine(Environment.CurrentDirectory, "lector_speed.txt");
        string currentText = "";
        Queue<string> playlist = new Queue<string>();

        private void Log(string msg)
        {
            try { File.AppendAllText(logFile, DateTime.Now.ToString() + ": " + msg + "\n"); } catch {}
        }

        public HiddenContext()
        {
            Log("Iniciando aplicacion con estado inteligente...");
            form = new DummyForm(this);
            
            int hkZ_Mod = MOD_CTRL | MOD_WIN;
            int hkZ_Key = (int)Keys.Z;
            int hkX_Mod = MOD_CTRL | MOD_WIN;
            int hkX_Key = (int)Keys.X;
            int hkA_Mod = MOD_CTRL | MOD_WIN;
            int hkA_Key = (int)Keys.A;

            string hotkeyFile = Path.Combine(Environment.CurrentDirectory, "lector_hotkeys.txt");
            if (File.Exists(hotkeyFile))
            {
                var lines = File.ReadAllLines(hotkeyFile);
                foreach (var line in lines)
                {
                    var p = line.Split('=');
                    if (p.Length == 2)
                    {
                        var vals = p[1].Split(',');
                        if (vals.Length == 2 && int.TryParse(vals[0], out int mod) && int.TryParse(vals[1], out int key))
                        {
                            if (p[0] == "Read") { hkZ_Mod = mod; hkZ_Key = key; }
                            else if (p[0] == "Slower") { hkX_Mod = mod; hkX_Key = key; }
                            else if (p[0] == "Faster") { hkA_Mod = mod; hkA_Key = key; }
                        }
                    }
                }
            }
            
            // Register hotkeys
            bool r1 = RegisterHotKey(form.Handle, 1, hkZ_Mod, hkZ_Key); // Read/Pause/Swap
            bool r2 = RegisterHotKey(form.Handle, 2, hkX_Mod, hkX_Key); // Speed Down
            bool r3 = RegisterHotKey(form.Handle, 3, hkA_Mod, hkA_Key); // Speed Up (A instead of V)
            
            if (!r1 || !r2 || !r3) {
                Log("¡ERROR CRÍTICO! Windows bloqueó los atajos. Z=" + r1 + " X=" + r2 + " A=" + r3 + ". ¿Hay otro programa o copia de seguridad usándolos?");
            } else {
                Log("Atajos registrados correctamente.");
            }
            
            synth = new SpeechSynthesizer();
            
            bool vozAsignada = false;
            foreach (var voice in synth.GetInstalledVoices())
            {
                string vName = voice.VoiceInfo.Name.ToLower();
                if (vName.Contains("raul") || vName.Contains("pablo"))
                {
                    synth.SelectVoice(voice.VoiceInfo.Name);
                    vozAsignada = true;
                    break;
                }
            }
            
            if (!vozAsignada)
            {
                foreach (var voice in synth.GetInstalledVoices())
                {
                    string vName = voice.VoiceInfo.Name.ToLower();
                    if (vName.Contains("sabina") || voice.VoiceInfo.Culture.Name.StartsWith("es"))
                    {
                        synth.SelectVoice(voice.VoiceInfo.Name);
                        break;
                    }
                }
            }
            synth.Rate = 1;
            if (File.Exists(speedFile))
            {
                int savedRate;
                if (int.TryParse(File.ReadAllText(speedFile), out savedRate))
                {
                    if (savedRate >= -10 && savedRate <= 10) synth.Rate = savedRate;
                }
            }
            
            synth.SpeakCompleted += (s, e) => {
                if (!e.Cancelled && playlist.Count > 0)
                {
                    currentText = playlist.Dequeue();
                    synth.SpeakAsync(currentText);
                }
            };
        }

        private void RestartCurrentPlayback()
        {
            if (!string.IsNullOrWhiteSpace(currentText) && (synth.State == SynthesizerState.Speaking || synth.State == SynthesizerState.Paused))
            {
                bool wasPaused = (synth.State == SynthesizerState.Paused);
                synth.SpeakAsyncCancelAll();
                Thread.Sleep(50);
                if (wasPaused) synth.Resume();
                synth.SpeakAsync(currentText);
            }
        }

        private string SafeGetClipboardText()
        {
            for (int i = 0; i < 5; i++)
            {
                try { if (Clipboard.ContainsText()) return Clipboard.GetText(); return null; }
                catch { Thread.Sleep(50); }
            }
            return null;
        }

        private void SafeSetClipboardText(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try { Clipboard.SetText(text); return; }
                catch { Thread.Sleep(50); }
            }
        }

        private void SafeClearClipboard()
        {
            for (int i = 0; i < 5; i++)
            {
                try { Clipboard.Clear(); return; }
                catch { Thread.Sleep(50); }
            }
        }

        private bool isProcessingHotkey = false;

        public async void HandleHotkey(int id)
        {
            if (isProcessingHotkey) return;
            isProcessingHotkey = true;

            try
            {
                Log("Hotkey presionado: " + id);
                if (id == 1) // Win+Ctrl+Z
                {
                    bool isLongPress = false;
                    int elapsed = 0;
                    // Esperar a que suelte Z, usando await para no congelar la voz
                    while (elapsed < 350)
                    {
                        await Task.Delay(50);
                        elapsed += 50;
                        if ((GetAsyncKeyState((int)Keys.Z) & 0x8000) == 0)
                        {
                            break; // Z soltada
                        }
                    }
                    if (elapsed >= 350)
                    {
                        isLongPress = true;
                        // Emitir un pitido corto de confirmación sin interrumpir el hilo ni la voz
                        var dummyTask = Task.Run(() => System.Console.Beep(1200, 100));
                    }

                    // Esperar a que suelten los modificadores para el SendKeys
                    while ((GetAsyncKeyState(0x11) & 0x8000) != 0 || // Ctrl
                           (GetAsyncKeyState(0x5B) & 0x8000) != 0 || // LWin
                           (GetAsyncKeyState(0x5C) & 0x8000) != 0 || // RWin
                           (GetAsyncKeyState((int)Keys.Z) & 0x8000) != 0)
                    {
                        await Task.Delay(50);
                    }

                string backup = SafeGetClipboardText();
                SafeClearClipboard();
                
                SendKeys.SendWait("^{c}");
                Thread.Sleep(200);
                
                string newText = SafeGetClipboardText() ?? "";
                if (!string.IsNullOrEmpty(newText))
                {
                    // Filtramos emojis (surrogates) y caracteres que no sean letras, numeros, puntuacion o espacios
                    newText = new string(newText.Where(c => !char.IsSurrogate(c)).ToArray());
                    newText = System.Text.RegularExpressions.Regex.Replace(newText, @"[^\p{L}\p{Nd}\p{P}\p{Z}\s]", "");
                }
                
                // Restaurar el portapapeles original
                if (backup != null) SafeSetClipboardText(backup);
                else SafeClearClipboard();
                
                if (synth.State == SynthesizerState.Speaking || synth.State == SynthesizerState.Paused)
                {
                    // Está leyendo algo. Verificamos si hay texto nuevo
                    if (string.IsNullOrWhiteSpace(newText) || newText == currentText)
                    {
                        // No seleccionó nada nuevo, o seleccionó lo mismo -> Pausar/Reanudar
                        if (synth.State == SynthesizerState.Speaking) synth.Pause();
                        else synth.Resume();
                    }
                    else
                    {
                        if (isLongPress)
                        {
                            // Encolar en nuestra propia lista para que no se borre al cambiar la velocidad
                            playlist.Enqueue(newText);
                        }
                        else
                        {
                            // Seleccionó texto nuevo -> Cortar anterior, vaciar cola y empezar nuevo
                            playlist.Clear();
                            synth.SpeakAsyncCancelAll();
                            if (synth.State == SynthesizerState.Paused) synth.Resume();
                            currentText = newText;
                            synth.SpeakAsync(currentText);
                        }
                    }
                }
                else
                {
                    // No estaba leyendo -> Empezar a leer
                    if (!string.IsNullOrWhiteSpace(newText))
                    {
                        playlist.Clear();
                        currentText = newText;
                        synth.SpeakAsync(currentText);
                    }
                }
            }
            else if (id == 2) // Win+Ctrl+X (Bajar velocidad)
            {
                if (synth.Rate > -10) synth.Rate--;
                Log("Velocidad: " + synth.Rate);
                try { File.WriteAllText(speedFile, synth.Rate.ToString()); } catch {}
                RestartCurrentPlayback();
            }
            else if (id == 3) // Win+Ctrl+A (Subir velocidad)
            {
                if (synth.Rate < 10) synth.Rate++;
                Log("Velocidad: " + synth.Rate);
                try { File.WriteAllText(speedFile, synth.Rate.ToString()); } catch {}
                RestartCurrentPlayback();
            }
        }
        finally
        {
            isProcessingHotkey = false;
        }
    }

        protected override void Dispose(bool disposing)
        {
            UnregisterHotKey(form.Handle, 1);
            UnregisterHotKey(form.Handle, 2);
            UnregisterHotKey(form.Handle, 3);
            synth.Dispose();
            base.Dispose(disposing);
        }

        class DummyForm : Form
        {
            private HiddenContext context;
            public DummyForm(HiddenContext ctx)
            {
                context = ctx;
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Minimized;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    int id = m.WParam.ToInt32();
                    context.HandleHotkey(id);
                }
                base.WndProc(ref m);
            }
            
            protected override void SetVisibleCore(bool value)
            {
                base.SetVisibleCore(false);
            }
        }
    }
}
