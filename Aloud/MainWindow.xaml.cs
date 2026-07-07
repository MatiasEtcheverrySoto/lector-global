using System;
using Microsoft.Win32;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Data;
using System.Globalization;
using WindowsInput;
using WindowsInput.Native;

namespace LectorGlobalApp
{
    public partial class MainWindow : Window
    {
        // P/Invoke
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        const uint KEYEVENTF_KEYUP = 0x0002;

        const int MOD_CTRL = 0x0002;
        const int MOD_SHIFT = 0x0004;
        const int MOD_WIN = 0x0008;
        const int WM_HOTKEY = 0x0312;

        private SpeechSynthesizer synth;
        private IntPtr windowHandle;
        private HwndSource source;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private RawKeyboardHook rawHook;
        private bool forceExit = false;
        private LocalDictationManager dictationManager;
        private bool isDictationActive = false;
        private bool cancelInjection = false;
        private DictationOverlayWindow _overlayWindow;

        private string currentText = "";
        private Queue<string> playlist = new Queue<string>();
        private bool isProcessingHotkey = false;
        private bool isDarkMode = false; // Light mode default for Flow design
        private bool isSidebarOpen = true;
        private bool isConfiguringHotkey = false;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = Strings.Instance;
            
            synth = new SpeechSynthesizer();
            synth.SpeakCompleted += Synth_SpeakCompleted;

            dictationManager = new LocalDictationManager();
            dictationManager.OnPartialResult += DictationManager_OnPartialResult;
            dictationManager.OnResult += DictationManager_OnResult;
            dictationManager.OnAudioLevel += DictationManager_OnAudioLevel;
            dictationManager.OnError += (err) => {
                // Ignore or log error
            };

            LoadVoices();
            LoadStats();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            UpdateInsightsUI();
            LoadAppLanguages();
            // Register auto start logic and initialize checkbox
            CheckRegistryStartup();
            CheckRegistryConfig();

            AuthManager.OnAuthStateChanged += UpdateAuthUI;
            UpdateAuthUI();

            TxtHotkeyZ.PreviewKeyUp += HotkeyTextBox_PreviewKeyUp;
            TxtHotkeyX.PreviewKeyUp += HotkeyTextBox_PreviewKeyUp;
            TxtHotkeyA.PreviewKeyUp += HotkeyTextBox_PreviewKeyUp;
            TxtHotkeyD.PreviewKeyUp += HotkeyTextBox_PreviewKeyUp;
            TxtHotkeyS.PreviewKeyUp += HotkeyTextBox_PreviewKeyUp;
        }

        private void CheckRegistryConfig()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\AloudApp"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("DarkMode");
                        if (val != null)
                        {
                            bool isDark = (int)val == 1;
                            ChkDarkMode.IsChecked = isDark;
                            SetDarkMode(isDark, false);
                        }

                        var valSpeed = key.GetValue("Speed");
                        if (valSpeed != null && SldSpeed != null)
                        {
                            SldSpeed.Value = (int)valSpeed;
                        }

                        var valAppLang = key.GetValue("AppLang");
                        if (valAppLang != null && CmbLanguage != null && CmbLanguage.ItemsSource != null)
                        {
                            string langCode = (string)valAppLang;
                            foreach (AppLanguage item in CmbLanguage.ItemsSource)
                            {
                                if (item.Code == langCode)
                                {
                                    CmbLanguage.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                        else if (CmbLanguage != null && CmbLanguage.ItemsSource != null)
                        {
                            var list = CmbLanguage.ItemsSource as List<AppLanguage>;
                            if (list != null && list.Count > 0)
                                CmbLanguage.SelectedItem = list[0];
                        }

                        var valVoiceLang = key.GetValue("VoiceLang");
                        if (valVoiceLang != null && CmbFilterLanguage != null && CmbFilterLanguage.ItemsSource != null)
                        {
                            string vLang = (string)valVoiceLang;
                            foreach (string item in CmbFilterLanguage.ItemsSource)
                            {
                                if (item == vLang)
                                {
                                    CmbFilterLanguage.SelectedItem = item;
                                    break;
                                }
                            }
                        }

                        var valVoiceName = key.GetValue("VoiceName");
                        if (valVoiceName != null && CmbVoices != null && CmbVoices.ItemsSource != null)
                        {
                            string vName = (string)valVoiceName;
                            foreach (string item in CmbVoices.ItemsSource)
                            {
                                if (item == vName)
                                {
                                    CmbVoices.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveToRegistry(string keyName, object value)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\AloudApp"))
                {
                    key.SetValue(keyName, value);
                }
            }
            catch { }
        }

        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            isSidebarOpen = !isSidebarOpen;
            this.Tag = isSidebarOpen ? "Expanded" : "Collapsed";
            double targetWidth = isSidebarOpen ? 230 : 68;
            
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };
            
            SidebarBorder.BeginAnimation(FrameworkElement.WidthProperty, animation);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            windowHandle = new WindowInteropHelper(this).Handle;
            source = HwndSource.FromHwnd(windowHandle);

            LoadHotkeys();

            rawHook = new RawKeyboardHook();
            rawHook.OnPhysicalKeyPressed += (vkCode) => {
                if (isConfiguringHotkey) return false;

                int modifiers = 0;
                if (rawHook.CtrlDown) modifiers |= MOD_CTRL;
                if (rawHook.WinDown) modifiers |= MOD_WIN;
                if (rawHook.ShiftDown) modifiers |= MOD_SHIFT;
                if (rawHook.AltDown) modifiers |= 1; // 1 = MOD_ALT
                
                if (vkCode == hkZ_Key && modifiers == hkZ_Mod) { Dispatcher.InvokeAsync(() => HandleHotkey(1)); return true; }
                if (vkCode == hkX_Key && modifiers == hkX_Mod) { Dispatcher.InvokeAsync(() => HandleHotkey(2)); return true; }
                if (vkCode == hkA_Key && modifiers == hkA_Mod) { Dispatcher.InvokeAsync(() => HandleHotkey(3)); return true; }
                if (vkCode == hkD_Key && modifiers == hkD_Mod) { Dispatcher.InvokeAsync(() => HandleHotkey(4)); return true; }
                if (vkCode == hkS_Key && modifiers == hkS_Mod) { Dispatcher.InvokeAsync(() => HandleHotkey(5)); return true; }
                return false;
            };
        }

        private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (!e.Cancelled && playlist.Count > 0)
            {
                currentText = playlist.Dequeue();
                synth.SpeakAsync(currentText);
            }
        }

        private async Task RestartCurrentPlaybackAsync()
        {
            if (!string.IsNullOrWhiteSpace(currentText) && (synth.State == SynthesizerState.Speaking || synth.State == SynthesizerState.Paused))
            {
                bool wasPaused = (synth.State == SynthesizerState.Paused);
                synth.SpeakAsyncCancelAll();
                await Task.Delay(50);
                if (wasPaused) synth.Resume();
                synth.SpeakAsync(currentText);
            }
        }

        private async Task<string> SafeGetClipboardTextAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                try { if (System.Windows.Clipboard.ContainsText()) return System.Windows.Clipboard.GetText(); return null; }
                catch { await Task.Delay(50); }
            }
            return null;
        }

        private async Task SafeSetClipboardTextAsync(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try { System.Windows.Clipboard.SetText(text); return; }
                catch { await Task.Delay(50); }
            }
        }

        private async Task SafeClearClipboardAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                try { System.Windows.Clipboard.Clear(); return; }
                catch { await Task.Delay(50); }
            }
        }

        private async void HandleHotkey(int id)
        {
            if (isProcessingHotkey) return;
            isProcessingHotkey = true;

            try
            {
                if (id == 1) // Win+Ctrl+Z
                {
                    bool isLongPress = false;
                    int elapsed = 0;
                    while (elapsed < 350)
                    {
                        await Task.Delay(50);
                        elapsed += 50;
                        if ((GetAsyncKeyState(hkZ_Key) & 0x8000) == 0) break;
                    }
                    if (elapsed >= 350)
                    {
                        isLongPress = true;
                        var dummyTask = Task.Run(() => System.Console.Beep(1200, 100));
                    }

                    bool ctrlDown = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                    bool altDown = (GetAsyncKeyState(0x12) & 0x8000) != 0;
                    bool shiftDown = (GetAsyncKeyState(0x10) & 0x8000) != 0;
                    bool lWinDown = (GetAsyncKeyState(0x5B) & 0x8000) != 0;
                    bool rWinDown = (GetAsyncKeyState(0x5C) & 0x8000) != 0;

                    if (ctrlDown) keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (altDown) keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (shiftDown) keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (lWinDown) keybd_event(0x5B, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (rWinDown) keybd_event(0x5C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                    string backup = await SafeGetClipboardTextAsync();
                    await SafeClearClipboardAsync();
                    
                    System.Windows.Forms.SendKeys.SendWait("^{c}");
                    await Task.Delay(200);
                    
                    string newText = await SafeGetClipboardTextAsync() ?? "";
                    if (!string.IsNullOrEmpty(newText))
                    {
                        newText = new string(newText.Where(c => !char.IsSurrogate(c)).ToArray());
                        newText = System.Text.RegularExpressions.Regex.Replace(newText, @"[^\p{L}\p{Nd}\p{P}\p{Z}\s]", "");
                    }
                    
                    if (backup != null) await SafeSetClipboardTextAsync(backup);
                    else await SafeClearClipboardAsync();
                    
                    if (synth.State == SynthesizerState.Speaking || synth.State == SynthesizerState.Paused)
                    {
                        if (string.IsNullOrWhiteSpace(newText) || newText == currentText)
                        {
                            if (synth.State == SynthesizerState.Speaking) synth.Pause();
                            else synth.Resume();
                        }
                        else
                        {
                            if (isLongPress) playlist.Enqueue(newText);
                            else
                            {
                                playlist.Clear();
                                synth.SpeakAsyncCancelAll();
                                if (synth.State == SynthesizerState.Paused) synth.Resume();
                                currentText = newText;
                                UpdateStatsOnReading(currentText.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length);
                                synth.SpeakAsync(currentText);
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(newText))
                        {
                            playlist.Clear();
                            currentText = newText;
                            UpdateStatsOnReading(currentText.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length);
                            synth.SpeakAsync(currentText);
                        }
                    }
                }
                else if (id == 2) // Bajar velocidad
                {
                    if (synth.Rate > -10) { synth.Rate--; SldSpeed.Value = synth.Rate; }
                    await RestartCurrentPlaybackAsync();
                }
                else if (id == 3) // Subir velocidad
                {
                    if (synth.Rate < 10) { synth.Rate++; SldSpeed.Value = synth.Rate; }
                    await RestartCurrentPlaybackAsync();
                }
                else if (id == 4 || id == 5) // Dictado (Vosk AI)
                {
                    if (!isDictationActive)
                    {
                        isDictationActive = true;
                        cancelInjection = false;
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                            _overlayWindow = new DictationOverlayWindow();
                            _overlayWindow.Show();
                        });
                        
                        Task.Run(async () => {
                            bool initSuccess = await dictationManager.InitializeAsync();
                            if (initSuccess)
                            {
                                dictationManager.StartDictation();
                            }
                            else
                            {
                                isDictationActive = false;
                                await Task.Delay(2000);
                                System.Windows.Application.Current.Dispatcher.Invoke(() => { _overlayWindow?.HideAndClose(); });
                            }
                        });

                        if (id == 4)
                        {
                            // Esperar a soltar la tecla
                            while ((GetAsyncKeyState(hkD_Key) & 0x8000) != 0 ||
                                   (GetAsyncKeyState(0x11) & 0x8000) != 0 || 
                                   (GetAsyncKeyState(0x12) & 0x8000) != 0 || 
                                   (GetAsyncKeyState(0x10) & 0x8000) != 0 || 
                                   (GetAsyncKeyState(0x5B) & 0x8000) != 0 || 
                                   (GetAsyncKeyState(0x5C) & 0x8000) != 0)   
                            {
                                await Task.Delay(50);
                            }
                            
                            dictationManager.StopDictation();
                            isDictationActive = false;
                            System.Windows.Application.Current.Dispatcher.Invoke(() => { _overlayWindow?.HideAndClose(); });
                        }
                    }
                    else if (id == 5) // Toggle
                    {
                        dictationManager.StopDictation();
                        isDictationActive = false;
                        System.Windows.Application.Current.Dispatcher.Invoke(() => { _overlayWindow?.HideAndClose(); });
                    }
                }
            }
            finally
            {
                isProcessingHotkey = false;
            }
        }

        private string _lastPartialSent = "";
        private InputSimulator _inputSimulator = new InputSimulator();

        private void DictationManager_OnPartialResult(string text)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (cancelInjection) return;

                // Si el usuario mantiene pulsadas teclas modificadoras (ej. modo Push-to-Talk)
                // NO inyectamos el texto en tiempo real para evitar atajos de teclado accidentales.
                bool ctrlDown = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                bool altDown = (GetAsyncKeyState(0x12) & 0x8000) != 0;
                bool lWinDown = (GetAsyncKeyState(0x5B) & 0x8000) != 0;
                bool rWinDown = (GetAsyncKeyState(0x5C) & 0x8000) != 0;

                if (ctrlDown || altDown || lWinDown || rWinDown)
                {
                    return; // Retrasar la inyección hasta el OnResult final
                }

                if (!string.IsNullOrWhiteSpace(text) && text != _lastPartialSent)
                {
                    if (_lastPartialSent.Length > 0)
                    {
                        System.Windows.Forms.SendKeys.SendWait("{BACKSPACE " + _lastPartialSent.Length + "}");
                    }
                    
                    string safeText = text.Replace("{", "{{}")
                                          .Replace("}", "{}}")
                                          .Replace("+", "{+}")
                                          .Replace("^", "{^}")
                                          .Replace("%", "{%}")
                                          .Replace("~", "{~}")
                                          .Replace("(", "{(}")
                                          .Replace(")", "{)}");
                    System.Windows.Forms.SendKeys.SendWait(safeText);
                    _lastPartialSent = text;
                }
            });
        }

        private void DictationManager_OnAudioLevel(float level)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (_overlayWindow != null)
                {
                    _overlayWindow.CurrentAudioLevel = level;
                }
            });
        }

        private void DictationManager_OnResult(string text)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                if (!string.IsNullOrWhiteSpace(text) && !cancelInjection)
                {
                    // Esperar a que suelte las teclas modificadoras antes de inyectar el texto final
                    while ((GetAsyncKeyState(0x11) & 0x8000) != 0 || 
                           (GetAsyncKeyState(0x12) & 0x8000) != 0 || 
                           (GetAsyncKeyState(0x5B) & 0x8000) != 0 || 
                           (GetAsyncKeyState(0x5C) & 0x8000) != 0)
                    {
                        await Task.Delay(50);
                    }

                    if (_lastPartialSent.Length > 0)
                    {
                        System.Windows.Forms.SendKeys.SendWait("{BACKSPACE " + _lastPartialSent.Length + "}");
                        _lastPartialSent = "";
                    }

                    string finalSafe = (text + " ").Replace("{", "{{}")
                                                   .Replace("}", "{}}")
                                                   .Replace("+", "{+}")
                                                   .Replace("^", "{^}")
                                                   .Replace("%", "{%}")
                                                   .Replace("~", "{~}")
                                                   .Replace("(", "{(}")
                                                   .Replace(")", "{)}");
                    System.Windows.Forms.SendKeys.SendWait(finalSafe);
                    UpdateStatsOnDictation(text.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length);
                }
                else
                {
                    _lastPartialSent = ""; // Clear if empty result
                }
            });
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (synth != null && synth.State == SynthesizerState.Speaking)
                {
                    synth.Pause();
                }
            }
        }

        public class VoiceDisplayItem : System.ComponentModel.INotifyPropertyChanged
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string CultureName { get; set; }

            private bool _isSelected;
            public bool IsSelected
            {
                get { return _isSelected; }
                set
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("IsSelected"));
                }
            }
            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        }

        private List<VoiceDisplayItem> allVoices = new List<VoiceDisplayItem>();

        private class AppLanguage
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public override string ToString() => Name;
        }

        private void LoadAppLanguages()
        {
            if (CmbLanguage == null) return;
            var langs = new List<AppLanguage>
            {
                new AppLanguage { Name = "Español", Code = "es" },
                new AppLanguage { Name = "English", Code = "en" }
            };
            CmbLanguage.ItemsSource = langs;
        }

        private void LoadVoices()
        {
            if (CmbVoices == null) return;
            
            allVoices.Clear();
            var cultures = new HashSet<string>();
            var voiceNames = new List<string>();

            foreach (var voice in synth.GetInstalledVoices())
            {
                var info = voice.VoiceInfo;
                voiceNames.Add(info.Name);
                
                string cultureName = info.Culture.NativeName;
                cultures.Add(cultureName);
                
                allVoices.Add(new VoiceDisplayItem {
                    Name = info.Name,
                    Description = $"{info.Culture.NativeName} - {info.Description}",
                    CultureName = cultureName
                });
            }
            
            CmbVoices.ItemsSource = voiceNames;
            if (voiceNames.Count > 0) CmbVoices.SelectedItem = voiceNames[0];
            
            if (CmbFilterLanguage != null)
            {
                var culturesList = cultures.ToList();
                CmbFilterLanguage.ItemsSource = culturesList;
                if (culturesList.Count > 0) CmbFilterLanguage.SelectedItem = culturesList[0];
            }
        }

        private void CmbFilterLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFilterLanguage.SelectedItem == null || VoicesList == null) return;
            string selectedCulture = CmbFilterLanguage.SelectedItem.ToString();
            VoicesList.ItemsSource = allVoices.Where(v => v.CultureName == selectedCulture).ToList();
            SaveToRegistry("VoiceLang", selectedCulture);
        }

        private SpeechSynthesizer _testSynth;

        private void BtnTestSpecificVoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string voiceName)
            {
                // Parar cualquier lectura principal actual
                synth.SpeakAsyncCancelAll();

                // Parar y limpiar la prueba anterior
                if (_testSynth != null)
                {
                    _testSynth.SpeakAsyncCancelAll();
                    _testSynth.Dispose();
                }

                _testSynth = new SpeechSynthesizer();
                _testSynth.Rate = synth.Rate;
                _testSynth.SelectVoice(voiceName);
                _testSynth.SpeakAsync("Esta es una prueba de cómo suena esta voz.");
            }
        }

        private void VoiceRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string voiceName)
            {
                if (CmbVoices.ItemsSource != null)
                {
                    foreach (string item in CmbVoices.ItemsSource)
                    {
                        if (item == voiceName)
                        {
                            CmbVoices.SelectedItem = voiceName;
                            break;
                        }
                    }
                }
            }
        }

        private void BtnTrial_Click(object sender, RoutedEventArgs e)
        {
            var title = Strings.Instance.TrialTitle.ToLower();
            if (title.Contains("acceder") || title.Contains("access"))
            {
                Strings.Instance.UpdateTrialState(Strings.TrialState.Active, 9);
            }
            else if (title.Contains("quedan") || title.Contains("left"))
            {
                Strings.Instance.UpdateTrialState(Strings.TrialState.Expired, 0);
            }
            else
            {
                Strings.Instance.UpdateTrialState(Strings.TrialState.NotStarted, 10);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void UpdateAuthUI()
        {
            Dispatcher.Invoke(() => {
                if (AuthManager.CurrentUser != null)
                {
                    PanelLoggedOut.Visibility = Visibility.Collapsed;
                    PanelLoggedIn.Visibility = Visibility.Visible;
                    BtnLogin.Visibility = Visibility.Collapsed;
                    BtnLogout.Visibility = Visibility.Visible;

                    TxtUserName.Text = AuthManager.CurrentUser.DisplayName;
                    TxtUserEmail.Text = AuthManager.CurrentUser.Email;
                    
                    try {
                        ImgAvatar.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(AuthManager.CurrentUser.AvatarUrl));
                    } catch { }
                }
                else
                {
                    PanelLoggedOut.Visibility = Visibility.Visible;
                    PanelLoggedIn.Visibility = Visibility.Collapsed;
                    BtnLogin.Visibility = Visibility.Visible;
                    BtnLogout.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginWin = new LoginWindow(this);
            loginWin.ShowDialog();
        }

        private async void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            await AuthManager.Logout();
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                if (sender is System.Windows.Controls.Button btn) btn.Content = "☐";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                if (sender is System.Windows.Controls.Button btn) btn.Content = "❐";
            }
        }

        private void CmbVoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbVoices.SelectedItem != null && synth != null)
            {
                string voiceName = CmbVoices.SelectedItem.ToString();
                synth.SelectVoice(voiceName);
                SaveToRegistry("VoiceName", voiceName);
                
                if (allVoices != null)
                {
                    foreach (var v in allVoices)
                    {
                        v.IsSelected = (v.Name == voiceName);
                    }
                }
            }
        }

        private void SldSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (synth != null)
            {
                int rate = (int)e.NewValue;
                synth.Rate = rate;
                SaveToRegistry("Speed", rate);
                
                if (TxtSpeedValue != null)
                {
                    double[] exactMultipliers = { 0.33, 0.37, 0.41, 0.46, 0.51, 0.58, 0.65, 0.72, 0.80, 0.90, 1.00, 1.11, 1.24, 1.38, 1.54, 1.72, 1.92, 2.14, 2.37, 2.66, 2.96 };
                    double multiplier = exactMultipliers[rate + 10];
                    TxtSpeedValue.Text = string.Format("x{0:0.00}", multiplier);
                }
            }
        }

        private void BtnTestVoice_Click(object sender, RoutedEventArgs e)
        {
            synth.SpeakAsyncCancelAll();
            synth.SpeakAsync("Esta es una prueba de la voz seleccionada.");
        }

        private List<string> selectedImagePaths = new List<string>();
        
        private void TxtFeedback_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.V && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                if (System.Windows.Clipboard.ContainsImage())
                {
                    var imageSource = System.Windows.Clipboard.GetImage();
                    if (imageSource != null)
                    {
                        string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Feedback_Paste_" + Guid.NewGuid().ToString() + ".png");
                        using (var fileStream = new System.IO.FileStream(tempPath, System.IO.FileMode.Create))
                        {
                            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(imageSource));
                            encoder.Save(fileStream);
                        }
                        AddAttachmentToFeedback(tempPath);
                    }
                    e.Handled = true;
                }
                else if (System.Windows.Clipboard.ContainsFileDropList())
                {
                    var fileList = System.Windows.Clipboard.GetFileDropList();
                    foreach (string file in fileList)
                    {
                        AddAttachmentToFeedback(file);
                    }
                    e.Handled = true;
                }
            }
        }

        private void AddAttachmentToFeedback(string path)
        {
            if (selectedImagePaths.Count < 10 && !selectedImagePaths.Contains(path)) 
            {
                var fileInfo = new System.IO.FileInfo(path);
                if (fileInfo.Length > 15 * 1024 * 1024)
                {
                    System.Windows.MessageBox.Show($"El archivo {System.IO.Path.GetFileName(path)} supera el límite de 15MB.", "Archivo muy grande", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectedImagePaths.Add(path);
                UpdatePreviewImages();
            }
        }

        private void UpdatePreviewImages()
        {
            PanelPreviewImages.Children.Clear();
            foreach(string path in selectedImagePaths) 
            {
                string ext = System.IO.Path.GetExtension(path).ToLower();
                if (ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv")
                {
                    var border = new System.Windows.Controls.Border {
                        Background = (System.Windows.Media.Brush)FindResource("SidebarBorderBg"),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0,0,10,0),
                        Height = 80,
                        Width = 80,
                        ToolTip = path
                    };
                    var tb = new System.Windows.Controls.TextBlock {
                        Text = "🎥\nVideo",
                        Foreground = (System.Windows.Media.Brush)FindResource("TextMain"),
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                    border.Child = tb;
                    PanelPreviewImages.Children.Add(border);
                }
                else
                {
                    var img = new System.Windows.Controls.Image { 
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(path)), 
                        MaxHeight = 80, 
                        Margin = new Thickness(0,0,10,0),
                        ToolTip = path
                    };
                    PanelPreviewImages.Children.Add(img);
                }
            }
            PanelPreviewImages.Visibility = Visibility.Visible;
        }

        private void BtnCargarFoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Archivos multimedia (*.png;*.jpeg;*.jpg;*.mp4;*.mov;*.avi;*.mkv)|*.png;*.jpeg;*.jpg;*.mp4;*.mov;*.avi;*.mkv|Todos (*.*)|*.*";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach(string file in openFileDialog.FileNames) {
                    AddAttachmentToFeedback(file);
                }
            }
        }

        private void TxtFeedback_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (LblFeedbackPlaceholder != null)
            {
                LblFeedbackPlaceholder.Visibility = string.IsNullOrEmpty(TxtFeedback.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void BtnEnviarFeedbackAction_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtFeedback.Text)) return;
            
            BtnEnviarFeedbackAction.IsEnabled = false;
            BtnEnviarFeedbackAction.Content = Strings.Instance.BtnFeedbackSending;
            
            try 
            {
                string scriptUrl = "https://script.google.com/macros/s/AKfycbzwSHLnQ7FxCw9V7siTQccwwo-Sd5o9hlLj-U6KnzF3rEsrltN2pZdKWMFE-WWOUpGq_g/exec"; 
                var client = new HttpClient();
                
                object payload;
                if (selectedImagePaths.Count == 0)
                {
                    payload = new { mensaje = TxtFeedback.Text };
                }
                else if (selectedImagePaths.Count == 1)
                {
                    string path = selectedImagePaths[0];
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    payload = new {
                        mensaje = TxtFeedback.Text,
                        imagen = Convert.ToBase64String(bytes),
                        nombre = System.IO.Path.GetFileName(path)
                    };
                }
                else
                {
                    using (var memoryStream = new System.IO.MemoryStream())
                    {
                        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                        {
                            foreach (string path in selectedImagePaths)
                            {
                                var entry = archive.CreateEntry(System.IO.Path.GetFileName(path));
                                using (var entryStream = entry.Open())
                                using (var fileStream = System.IO.File.OpenRead(path))
                                {
                                    fileStream.CopyTo(entryStream);
                                }
                            }
                        }
                        payload = new {
                            mensaje = TxtFeedback.Text,
                            imagen = Convert.ToBase64String(memoryStream.ToArray()),
                            nombre = "Adjuntos.zip"
                        };
                    }
                }
                
                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                if (scriptUrl != "TU_URL_DE_GOOGLE_APPS_SCRIPT_AQUI")
                {
                    var response = await client.PostAsync(scriptUrl, content);
                }
                
                TxtFeedback.Text = "";
                selectedImagePaths.Clear();
                PanelPreviewImages.Children.Clear();
                PanelPreviewImages.Visibility = Visibility.Collapsed;
                
                BtnEnviarFeedbackAction.Content = Strings.Instance.BtnFeedbackSent;
                
                await Task.Delay(3000);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error al enviar: " + ex.Message);
            }
            finally
            {
                BtnEnviarFeedbackAction.IsEnabled = true;
                BtnEnviarFeedbackAction.Content = Strings.Instance.BtnSendFeedbackAction;
            }
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbLanguage.SelectedItem is AppLanguage item)
            {
                string lang = item.Code;
                Strings.Instance.CurrentLanguage = lang;
                SaveToRegistry("AppLang", lang);
            }
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
            {
                // Reset all button styles
                BtnNavInicio.Style = (Style)FindResource("SidebarButton");
                BtnNavVoces.Style = (Style)FindResource("SidebarButton");
                BtnNavAtajos.Style = (Style)FindResource("SidebarButton");
                BtnNavInsights.Style = (Style)FindResource("SidebarButton");
                BtnNavAjustes.Style = (Style)FindResource("SidebarButton");
                BtnNavFeedback.Style = (Style)FindResource("SidebarButton");
                
                // Set active style for clicked
                btn.Style = (Style)FindResource("SidebarButtonActive");

                // Hide all panels
                PanelInicio.Visibility = Visibility.Collapsed;
                PanelVoces.Visibility = Visibility.Collapsed;
                PanelAtajos.Visibility = Visibility.Collapsed;
                PanelInsights.Visibility = Visibility.Collapsed;
                PanelAjustes.Visibility = Visibility.Collapsed;
                PanelFeedback.Visibility = Visibility.Collapsed;

                // Show target panel
                string target = btn.Tag.ToString();
                if (target == "PanelInicio") PanelInicio.Visibility = Visibility.Visible;
                else if (target == "PanelVoces") PanelVoces.Visibility = Visibility.Visible;
                else if (target == "PanelAtajos") PanelAtajos.Visibility = Visibility.Visible;
                else if (target == "PanelInsights") 
                {
                    PanelInsights.Visibility = Visibility.Visible;
                    UpdateInsightsUI();
                }
                else if (target == "PanelAjustes") PanelAjustes.Visibility = Visibility.Visible;
                else if (target == "PanelFeedback") PanelFeedback.Visibility = Visibility.Visible;
            }
        }

        private void SldThemeToggle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Resources == null) return;
            isDarkMode = e.NewValue == 1;
            
            if (isDarkMode)
            {
                // Elegante Dark Mode (Negros profundos)
                this.Resources["WindowBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#09090B"));
                this.Resources["SidebarBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#09090B"));
                this.Resources["SidebarBorderBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27272A"));
                this.Resources["WindowBorderBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27272A"));
                this.Resources["CardBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#18181B"));
                this.Resources["TextMain"] = System.Windows.Media.Brushes.White;
                this.Resources["TextMuted"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A1A1AA"));
                this.Resources["SidebarActiveBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27272A"));
                this.Resources["SidebarHoverBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27272A"));
                this.Resources["HeroBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#18181B"));
                this.Resources["HeroText"] = System.Windows.Media.Brushes.White;
                this.Resources["HeroTextMuted"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A1A1AA"));
                this.Resources["TrialBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2E1065"));
                this.Resources["TrialBorderC"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4C1D95"));
                this.Resources["FeedbackBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27272A"));
                this.Resources["FeedbackText"] = System.Windows.Media.Brushes.White;
            }
            else
            {
                // Flow Light Mode
                this.Resources["WindowBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F3F4F6"));
                this.Resources["SidebarBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9FAFB"));
                this.Resources["SidebarBorderBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"));
                this.Resources["WindowBorderBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"));
                this.Resources["CardBg"] = System.Windows.Media.Brushes.White;
                this.Resources["TextMain"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#111827"));
                this.Resources["TextMuted"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280"));
                this.Resources["SidebarActiveBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"));
                this.Resources["SidebarHoverBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"));
                this.Resources["HeroBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#111827"));
                this.Resources["HeroText"] = System.Windows.Media.Brushes.White;
                this.Resources["HeroTextMuted"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF"));
                this.Resources["TrialBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FDF4FF"));
                this.Resources["TrialBorderC"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FBCFE8"));
                this.Resources["FeedbackBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F3F4F6"));
                this.Resources["FeedbackText"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#111827"));
            }
        }

        private int hkZ_Mod = MOD_CTRL | MOD_WIN; private int hkZ_Key = (int)System.Windows.Forms.Keys.Z;
        private int hkX_Mod = MOD_CTRL | MOD_WIN; private int hkX_Key = (int)System.Windows.Forms.Keys.C;
        private int hkA_Mod = MOD_CTRL | MOD_WIN; private int hkA_Key = (int)System.Windows.Forms.Keys.X;
        private int hkD_Mod = MOD_CTRL | MOD_WIN; private int hkD_Key = (int)System.Windows.Forms.Keys.U;
        private int hkS_Mod = MOD_CTRL | MOD_WIN; private int hkS_Key = (int)System.Windows.Forms.Keys.I;

        private void LoadHotkeys()
        {
            string file = System.IO.Path.Combine(Environment.CurrentDirectory, "lector_hotkeys.txt");
            if (System.IO.File.Exists(file))
            {
                var lines = System.IO.File.ReadAllLines(file);
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
                            else if (p[0] == "Dictate") { hkD_Mod = mod; hkD_Key = key; }
                            else if (p[0] == "ToggleDictate") { hkS_Mod = mod; hkS_Key = key; }
                        }
                    }
                }
            }
            
            // Migrate any old dictation hotkeys to the new Ctrl+Win+U and Ctrl+Win+I
            if (hkD_Key == (int)System.Windows.Forms.Keys.A || hkD_Key == (int)System.Windows.Forms.Keys.D)
            {
                hkD_Mod = MOD_CTRL | MOD_WIN; hkD_Key = (int)System.Windows.Forms.Keys.U;
                SaveHotkeys();
            }
            if (hkS_Key == (int)System.Windows.Forms.Keys.S)
            {
                hkS_Mod = MOD_CTRL | MOD_WIN; hkS_Key = (int)System.Windows.Forms.Keys.I;
                SaveHotkeys();
            }

            UpdateHotkeysText();
        }

        private string FormatHotkey(int mod, int key)
        {
            string s = "";
            if ((mod & MOD_CTRL) != 0) s += "Ctrl + ";
            if ((mod & MOD_WIN) != 0) s += "Win + ";
            if ((mod & 1) != 0) s += "Alt + ";       // MOD_ALT
            if ((mod & 4) != 0) s += "Shift + ";     // MOD_SHIFT
            
            if (key != 0)
                s += ((System.Windows.Forms.Keys)key).ToString();
            else if (s.EndsWith(" + "))
                s = s.Substring(0, s.Length - 3);

            if (string.IsNullOrEmpty(s)) s = "Presiona teclas...";

            return s;
        }

        private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            isConfiguringHotkey = true;
            if (sender is System.Windows.Controls.TextBox txt) txt.Text = "Presiona teclas...";
            
            // Unregister hotkeys while registering a new one to avoid conflicts
            for (int i = 1; i <= 5; i++) UnregisterHotKey(windowHandle, i);
        }
        
        private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            isConfiguringHotkey = false;
            UpdateHotkeysText();
            SaveHotkeys();
        }

        private void HotkeyTextBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int key = KeyInterop.VirtualKeyFromKey(e.Key == Key.System ? e.SystemKey : e.Key);
            bool isModifier = (key == 16 || key == 160 || key == 161 || // Shift
                               key == 17 || key == 162 || key == 163 || // Ctrl
                               key == 18 || key == 164 || key == 165 || // Alt
                               key == 91 || key == 92);                 // Win

            if (!isModifier)
            {
                if (sender is System.Windows.Controls.TextBox txt)
                {
                    txt.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }
        
        private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            int key = KeyInterop.VirtualKeyFromKey(e.Key == Key.System ? e.SystemKey : e.Key);
            
            bool isModifier = (key == 16 || key == 160 || key == 161 || // Shift
                               key == 17 || key == 162 || key == 163 || // Ctrl
                               key == 18 || key == 164 || key == 165 || // Alt
                               key == 91 || key == 92);                 // Win

            int mod = 0;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) mod |= MOD_CTRL;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows) || Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin) || key == 91 || key == 92) mod |= MOD_WIN;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) mod |= 1; // MOD_ALT
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) mod |= MOD_SHIFT;
            
            System.Windows.Controls.TextBox txt = sender as System.Windows.Controls.TextBox;

            if (isModifier)
            {
                txt.Text = FormatHotkey(mod, 0);
                return;
            }

            txt.Text = FormatHotkey(mod, key);

            if (txt == TxtHotkeyZ) { hkZ_Mod = mod; hkZ_Key = key; }
            else if (txt == TxtHotkeyX) { hkX_Mod = mod; hkX_Key = key; }
            else if (txt == TxtHotkeyA) { hkA_Mod = mod; hkA_Key = key; }
            else if (txt == TxtHotkeyD) { hkD_Mod = mod; hkD_Key = key; }
            else if (txt == TxtHotkeyS) { hkS_Mod = mod; hkS_Key = key; }

            UpdateHotkeysText();
        }
        
        private void PencilZ_Click(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(TxtHotkeyZ);
        }

        private void PencilX_Click(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(TxtHotkeyX);
        }

        private void PencilA_Click(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(TxtHotkeyA);
        }
        
        private void PencilD_Click(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(TxtHotkeyD);
        }

        private void PencilS_Click(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(TxtHotkeyS);
        }

        private void BtnResetHotkeys_Click(object sender, RoutedEventArgs e)
        {
            hkZ_Mod = MOD_CTRL | MOD_WIN; hkZ_Key = (int)System.Windows.Forms.Keys.Z;
            hkX_Mod = MOD_CTRL | MOD_WIN; hkX_Key = (int)System.Windows.Forms.Keys.C;
            hkA_Mod = MOD_CTRL | MOD_WIN; hkA_Key = (int)System.Windows.Forms.Keys.X;
            hkD_Mod = MOD_CTRL | MOD_WIN; hkD_Key = (int)System.Windows.Forms.Keys.U;
            hkS_Mod = MOD_CTRL | MOD_WIN; hkS_Key = (int)System.Windows.Forms.Keys.I;
            UpdateHotkeysText();
            SaveHotkeys();
        }

        private void SaveHotkeys()
        {
            string content = $"Read={hkZ_Mod},{hkZ_Key}\nSlower={hkX_Mod},{hkX_Key}\nFaster={hkA_Mod},{hkA_Key}\nDictate={hkD_Mod},{hkD_Key}\nToggleDictate={hkS_Mod},{hkS_Key}";
            System.IO.File.WriteAllText(System.IO.Path.Combine(Environment.CurrentDirectory, "lector_hotkeys.txt"), content);
            
            for (int i = 1; i <= 5; i++) UnregisterHotKey(windowHandle, i);
            RegisterHotKey(windowHandle, 1, hkZ_Mod, hkZ_Key);
            RegisterHotKey(windowHandle, 2, hkX_Mod, hkX_Key);
            RegisterHotKey(windowHandle, 3, hkA_Mod, hkA_Key);
            RegisterHotKey(windowHandle, 4, hkD_Mod, hkD_Key);
            RegisterHotKey(windowHandle, 5, hkS_Mod, hkS_Key);
        }

        private void UpdateHotkeysText()
        {
            if (TxtHotkeyZ != null) TxtHotkeyZ.Text = FormatHotkey(hkZ_Mod, hkZ_Key);
            if (TxtHotkeyX != null) TxtHotkeyX.Text = FormatHotkey(hkX_Mod, hkX_Key);
            if (TxtHotkeyA != null) TxtHotkeyA.Text = FormatHotkey(hkA_Mod, hkA_Key);
            if (TxtHotkeyD != null) TxtHotkeyD.Text = FormatHotkey(hkD_Mod, hkD_Key);
            if (TxtHotkeyS != null) TxtHotkeyS.Text = FormatHotkey(hkS_Mod, hkS_Key);

            if (TxtHotkeyZ != null) TxtHotkeyZ.Tag = $"{hkZ_Mod},{hkZ_Key}";
            if (TxtHotkeyX != null) TxtHotkeyX.Tag = $"{hkX_Mod},{hkX_Key}";
            if (TxtHotkeyA != null) TxtHotkeyA.Tag = $"{hkA_Mod},{hkA_Key}";
            if (TxtHotkeyD != null) TxtHotkeyD.Tag = $"{hkD_Mod},{hkD_Key}";
            if (TxtHotkeyS != null) TxtHotkeyS.Tag = $"{hkS_Mod},{hkS_Key}";
        }


        public class LectorStats
        {
            public int WordsToday { get; set; } = 0;
            public int WordsTotal { get; set; } = 0;
            public int ActivationsToday { get; set; } = 0;
            public int ActivationsTotal { get; set; } = 0;
            public int StreakDays { get; set; } = 0;
            public DateTime LastUsedDate { get; set; } = DateTime.MinValue;
            public double TimeSavedMinutes { get; set; } = 0;
            public int DictatedWordsToday { get; set; } = 0;
            public int DictatedWordsTotal { get; set; } = 0;
            public int DictationActivations { get; set; } = 0;
        }

        private LectorStats _stats = new LectorStats();
        
        private void LoadStats()
        {
            string file = System.IO.Path.Combine(Environment.CurrentDirectory, "lector_stats.json");
            if (System.IO.File.Exists(file))
            {
                try {
                    string json = System.IO.File.ReadAllText(file);
                    _stats = JsonSerializer.Deserialize<LectorStats>(json) ?? new LectorStats();
                } catch { }
            }
            
            if (_stats.LastUsedDate.Date < DateTime.Now.Date)
            {
                if (_stats.LastUsedDate.Date == DateTime.Now.Date.AddDays(-1)) {
                    // maintained streak
                } else if (_stats.LastUsedDate != DateTime.MinValue) {
                    _stats.StreakDays = 0; // lost streak
                }
                
                if (_stats.ActivationsToday > 0 || _stats.WordsToday > 0 || _stats.DictationActivations > 0)
                {
                    _stats.StreakDays++;
                }
                
                _stats.WordsToday = 0;
                _stats.ActivationsToday = 0;
                _stats.DictatedWordsToday = 0;
                _stats.DictationActivations = 0;
                _stats.LastUsedDate = DateTime.Now;
                SaveStats();
            }
        }
        
        private void SaveStats()
        {
            try {
                string file = System.IO.Path.Combine(Environment.CurrentDirectory, "lector_stats.json");
                string json = JsonSerializer.Serialize(_stats);
                System.IO.File.WriteAllText(file, json);
            } catch { }
        }
        
        private void UpdateStatsOnReading(int wordCount)
        {
            if (wordCount <= 0) return;
            _stats.WordsToday += wordCount;
            _stats.WordsTotal += wordCount;
            _stats.ActivationsToday++;
            _stats.ActivationsTotal++;
            _stats.LastUsedDate = DateTime.Now;
            
            double visualWpm = 200.0;
            double currentWpm = 200.0 + (synth.Rate * 20.0);
            if (currentWpm > visualWpm)
            {
                double visualMinutes = wordCount / visualWpm;
                double listeningMinutes = wordCount / currentWpm;
                _stats.TimeSavedMinutes += (visualMinutes - listeningMinutes);
            }
            
            SaveStats();
            
            this.Dispatcher.Invoke(() => {
                if (PanelInsights.Visibility == Visibility.Visible || PanelInicio.Visibility == Visibility.Visible)
                {
                    UpdateInsightsUI();
                }
            });
        }

        private void UpdateStatsOnDictation(int wordCount)
        {
            if (wordCount <= 0) return;
            _stats.DictatedWordsToday += wordCount;
            _stats.DictatedWordsTotal += wordCount;
            _stats.DictationActivations++;
            _stats.ActivationsTotal++;
            _stats.LastUsedDate = DateTime.Now;
            
            SaveStats();
            
            this.Dispatcher.Invoke(() => {
                if (PanelInsights.Visibility == Visibility.Visible || PanelInicio.Visibility == Visibility.Visible)
                {
                    UpdateInsightsUI();
                }
            });
        }
        
        private void UpdateInsightsUI()
        {
            if (TxtWordsToday != null) TxtWordsToday.Text = _stats.WordsToday.ToString("N0");
            if (TxtWordsTotal != null) TxtWordsTotal.Text = _stats.WordsTotal.ToString("N0");
            
            if (TxtTimeSaved != null) TxtTimeSaved.Text = $"{Math.Round(_stats.TimeSavedMinutes, 1)} min";
            
            if (TxtActivations != null) TxtActivations.Text = _stats.ActivationsTotal.ToString("N0");
            
            if (TxtStreak != null) 
            {
                TxtStreak.Text = _stats.StreakDays == 1 ? "1 Día" : $"{_stats.StreakDays} Días";
            }
            
            if (TxtDictatedWordsTotal != null) TxtDictatedWordsTotal.Text = _stats.DictatedWordsTotal.ToString("N0");
            if (TxtDictatedWordsToday != null) TxtDictatedWordsToday.Text = _stats.DictatedWordsToday.ToString("N0");
        }

        private void CheckRegistryStartup()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        string appName = "AloudApp";
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        
                        object val = key.GetValue(appName);
                        if (val == null)
                        {
                            // Activate by default as requested
                            key.SetValue(appName, $"\"{exePath}\"");
                            ChkLaunchAtLogin.IsChecked = true;
                        }
                        else
                        {
                            ChkLaunchAtLogin.IsChecked = val.ToString() == $"\"{exePath}\"";
                        }
                    }
                }
            }
            catch { }
        }

        private void ChkLaunchAtLogin_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    string appName = "AloudApp";
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    key?.SetValue(appName, $"\"{exePath}\"");
                }
            }
            catch { }
        }

        private void ChkLaunchAtLogin_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    string appName = "AloudApp";
                    key?.DeleteValue(appName, false);
                }
            }
            catch { }
        }

        private void ChkDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            SetDarkMode(true);
        }

        private void ChkDarkMode_Unchecked(object sender, RoutedEventArgs e)
        {
            SetDarkMode(false);
        }

        private void SetDarkMode(bool dark, bool saveToRegistry = true)
        {
            isDarkMode = dark;
            if (saveToRegistry)
            {
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\AloudApp"))
                    {
                        key.SetValue("DarkMode", dark ? 1 : 0);
                    }
                }
                catch { }
            }
            
            if (dark)
            {
                System.Windows.Application.Current.Resources["WindowBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#121212"));
                System.Windows.Application.Current.Resources["SidebarBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#121212"));
                System.Windows.Application.Current.Resources["SidebarBorderBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2A2A2A"));
                System.Windows.Application.Current.Resources["CardBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
                System.Windows.Application.Current.Resources["SidebarActiveBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2A2A2A"));
                System.Windows.Application.Current.Resources["SidebarHoverBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
                System.Windows.Application.Current.Resources["TextMain"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9FAFB"));
                System.Windows.Application.Current.Resources["TextMuted"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF"));
                System.Windows.Application.Current.Resources["WindowBorderBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2A2A2A"));
                System.Windows.Application.Current.Resources["TrialBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("Transparent"));
                System.Windows.Application.Current.Resources["TrialBorderC"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4B5563"));
                
                this.Resources["WindowBg"] = System.Windows.Application.Current.Resources["WindowBg"];
                this.Resources["SidebarBg"] = System.Windows.Application.Current.Resources["SidebarBg"];
                this.Resources["SidebarBorderBg"] = System.Windows.Application.Current.Resources["SidebarBorderBg"];
                this.Resources["CardBg"] = System.Windows.Application.Current.Resources["CardBg"];
                this.Resources["SidebarActiveBg"] = System.Windows.Application.Current.Resources["SidebarActiveBg"];
                this.Resources["SidebarHoverBg"] = System.Windows.Application.Current.Resources["SidebarHoverBg"];
                this.Resources["TextMain"] = System.Windows.Application.Current.Resources["TextMain"];
                this.Resources["TextMuted"] = System.Windows.Application.Current.Resources["TextMuted"];
                this.Resources["WindowBorderBg"] = System.Windows.Application.Current.Resources["WindowBorderBg"];
                this.Resources["TrialBg"] = System.Windows.Application.Current.Resources["TrialBg"];
                this.Resources["TrialBorderC"] = System.Windows.Application.Current.Resources["TrialBorderC"];
            }
            else
            {
                System.Windows.Application.Current.Resources["WindowBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F3F4F6"));
                System.Windows.Application.Current.Resources["SidebarBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9FAFB"));
                System.Windows.Application.Current.Resources["SidebarBorderBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"));
                System.Windows.Application.Current.Resources["CardBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF"));
                System.Windows.Application.Current.Resources["SidebarActiveBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"));
                System.Windows.Application.Current.Resources["SidebarHoverBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F3F4F6"));
                System.Windows.Application.Current.Resources["TextMain"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#111827"));
                System.Windows.Application.Current.Resources["TextMuted"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280"));
                System.Windows.Application.Current.Resources["WindowBorderBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"));
                System.Windows.Application.Current.Resources["TrialBg"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FDF4FF"));
                System.Windows.Application.Current.Resources["TrialBorderC"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FBCFE8"));

                this.Resources["WindowBg"] = System.Windows.Application.Current.Resources["WindowBg"];
                this.Resources["SidebarBg"] = System.Windows.Application.Current.Resources["SidebarBg"];
                this.Resources["SidebarBorderBg"] = System.Windows.Application.Current.Resources["SidebarBorderBg"];
                this.Resources["CardBg"] = System.Windows.Application.Current.Resources["CardBg"];
                this.Resources["SidebarActiveBg"] = System.Windows.Application.Current.Resources["SidebarActiveBg"];
                this.Resources["SidebarHoverBg"] = System.Windows.Application.Current.Resources["SidebarHoverBg"];
                this.Resources["TextMain"] = System.Windows.Application.Current.Resources["TextMain"];
                this.Resources["TextMuted"] = System.Windows.Application.Current.Resources["TextMuted"];
                this.Resources["WindowBorderBg"] = System.Windows.Application.Current.Resources["WindowBorderBg"];
                this.Resources["TrialBg"] = System.Windows.Application.Current.Resources["TrialBg"];
                this.Resources["TrialBorderC"] = System.Windows.Application.Current.Resources["TrialBorderC"];
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            rawHook?.Dispose();
            
            if (synth != null)
            {
                synth.Dispose();
            }
            if (trayIcon != null)
            {
                trayIcon.Dispose();
            }
            base.OnClosed(e);
        }
    }

    public class HotkeyToTokensConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            if (string.IsNullOrEmpty(s)) return new string[0];
            if (s == Strings.Instance.StatusListening) return new string[] { s };
            return s.Split(new[] { " + " }, StringSplitOptions.None);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}