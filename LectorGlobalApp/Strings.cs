using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace LectorGlobalApp
{
    public class Strings : INotifyPropertyChanged
    {
        private static Strings _instance = new Strings();
        public static Strings Instance => _instance;

        public enum TrialState { NotStarted, Active, Expired }
        private TrialState _currentTrialState = TrialState.NotStarted;
        private int _trialDaysLeft = 10;
        
        public void UpdateTrialState(TrialState state, int daysLeft = 0)
        {
            _currentTrialState = state;
            _trialDaysLeft = daysLeft;
            RefreshTrialTexts();
        }

        private void RefreshTrialTexts()
        {
            if (_currentLanguage == "es")
            {
                if (_currentTrialState == TrialState.NotStarted)
                {
                    TrialTitle = "Puedes acceder a la prueba gratuita por 10 días";
                    TrialSubtitle = "Disfruta de voces IA ilimitadas.";
                    TrialButton = "Iniciar Prueba";
                }
                else if (_currentTrialState == TrialState.Active)
                {
                    TrialTitle = $"Te quedan {_trialDaysLeft} días de prueba";
                    TrialSubtitle = "Actualiza a PRO para mantener el acceso ilimitado.";
                    TrialButton = "Actualizar a PRO";
                }
                else
                {
                    TrialTitle = "Tu prueba gratuita ha finalizado";
                    TrialSubtitle = "Puedes acceder a voces IA adquiriendo la suscripción.";
                    TrialButton = "Adquirir Suscripción";
                }
            }
            else 
            {
                if (_currentTrialState == TrialState.NotStarted)
                {
                    TrialTitle = "You can access the 10-day free trial";
                    TrialSubtitle = "Enjoy unlimited AI voices.";
                    TrialButton = "Start Trial";
                }
                else if (_currentTrialState == TrialState.Active)
                {
                    TrialTitle = $"{_trialDaysLeft} days of trial left";
                    TrialSubtitle = "Upgrade to PRO to keep unlimited access.";
                    TrialButton = "Upgrade to PRO";
                }
                else
                {
                    TrialTitle = "Your free trial has ended";
                    TrialSubtitle = "You can access AI voices by getting a subscription.";
                    TrialButton = "Get Subscription";
                }
            }
        }

        private string _currentLanguage = "es";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    RefreshTrialTexts();
                    OnPropertyChanged(null);
                }
            }
        }

        private string _trialTitle = "Puedes acceder a la prueba gratuita por 10 días";
        public string TrialTitle 
        { 
            get => _trialTitle; 
            set { _trialTitle = value; OnPropertyChanged(); } 
        }

        private string _trialSubtitle = "Disfruta de voces IA ilimitadas.";
        public string TrialSubtitle 
        { 
            get => _trialSubtitle; 
            set { _trialSubtitle = value; OnPropertyChanged(); } 
        }

        private string _trialButton = "Iniciar Prueba";
        public string TrialButton 
        { 
            get => _trialButton; 
            set { _trialButton = value; OnPropertyChanged(); } 
        }

        public string LblAtajo4Title => Get("LblAtajo4Title");
        public string LblAtajo4Desc => Get("LblAtajo4Desc");
        public string LblAtajo5Title => Get("LblAtajo5Title");
        public string LblAtajo5Desc => Get("LblAtajo5Desc");
        
        public string LblLecturaSubtitle => Get("LblLecturaSubtitle");
        public string LblDictadoSubtitle => Get("LblDictadoSubtitle");
        
        public string StatusListening => Get("StatusListening");
        public string StatusDictating => Get("StatusDictating");
        public string StatusTranscribing => Get("StatusTranscribing");

        // Login Window
        public string LblLoginTitle => Get("LblLoginTitle");
        public string LblLoginSubtitle => Get("LblLoginSubtitle");
        public string LblEmail => Get("LblEmail");
        public string LblPassword => Get("LblPassword");
        public string BtnLoginAction => Get("BtnLoginAction");
        public string LblCreateAccount => Get("LblCreateAccount");
        public string LblOr => Get("LblOr");
        public string BtnLogout => Get("BtnLogout");
        public string LblAccountTitle => Get("LblAccountTitle");

        private Dictionary<string, Dictionary<string, string>> _dict = new Dictionary<string, Dictionary<string, string>>()
        {
            { "es", new Dictionary<string, string>() {
                { "AppTitle", "Aloud" },
                { "MenuInicio", "  Inicio" },
                { "MenuVoces", "  Idiomas y Voces" },
                { "MenuAtajos", "  Atajos" },
                { "MenuInsights", "  Estadísticas" },
                { "MenuAjustes", "  Ajustes" },
                { "MenuSoporte", "  Soporte" },
                { "HeroTitle", "Haz que Windows suene como tú" },
                { "HeroSubtitle", "Selecciona cualquier texto y presiona Ctrl + Win + Z." },
                { "BtnTestVoice", "Test de Voz" },
                { "StatusActive", "Atajos Activos (Escuchando...)" },
                { "StatusBlocked", "Atajos Bloqueados por Windows" },
                { "LblVoice", "Voz Principal" },
                { "LblSpeed", "Velocidad de Lectura" },
                { "StatWordsSub", "palabras leídas hoy" },
                { "StatWpmSub", "velocidad promedio" },
                { "StatStreakSub", "en racha" },
                { "BtnFeedback", "Dejar Feedback 📝" },
                { "TabSettingsTitle", "Configuración y Ajustes" },
                { "LblLanguage", "Idioma de la Interfaz" },
                { "TabShortcutsTitle", "Atajos de Teclado" },
                { "ShortcutZ", "Ctrl + Win + Z : Leer / Pausar" },
                { "ShortcutX", "Ctrl + Win + X : Más Lento" },
                { "ShortcutA", "Ctrl + Win + A : Más Rápido" },
                { "TabVoicesTitle", "Idiomas y Voces" },
                { "TabInsightsTitle", "Estadísticas de Uso" },
                { "LblInicioTitle", "Reproducción Inteligente" },
                { "LblInicioDesc", "Selecciona cualquier texto en Windows y presiona tu atajo de teclado para escucharlo al instante." },
                { "LblLanguageSelect", "Idioma" },
                { "LblAvailableVoices", "Voces Disponibles" },
                { "BtnProbar", "Probar" },
                { "BtnSeleccionar", "Seleccionar" },
                { "LblAtajosDesc", "Elige tus atajos preferidos para usar Aloud." },
                { "LblLecturaSubtitle", "Lectura" },
                { "LblAtajo1Title", "Leer / Pausar" },
                { "LblAtajo1Desc", "Leer texto seleccionado o pausar lectura" },
                { "LblAtajo2Title", "Disminuir Velocidad" },
                { "LblAtajo2Desc", "Desacelerar la lectura" },
                { "LblAtajo3Title", "Aumentar Velocidad" },
                { "LblAtajo3Desc", "Acelera la lectura" },
                { "BtnResetHotkeys", "Restablecer por defecto" },
                { "LblWordsToday", "Palabras Hoy" },
                { "LblTotalProcessed", "Total procesadas" },
                { "LblTimeSaved", "Tiempo Ahorrado" },
                { "LblByListeningFast", "Por escuchar rápido" },
                { "LblActivations", "Activaciones" },
                { "LblReadingsStarted", "Lecturas iniciadas hoy" },
                { "LblCurrentStreak", "Racha Actual" },
                { "LblKeepUsing", "¡Sigue usando Aloud cada día!" },
                { "LblReadingHistory", "Historial de Lectura" },
                { "LblAppSettings", "Ajustes de la aplicación" },
                { "LblLaunchAtLogin", "Iniciar la app al arrancar" },
                { "LblAppLanguage", "Idioma de la Interfaz" },
                { "LblDarkMode", "Modo Oscuro" },
                { "LblAccount", "Cuenta" },
                { "LblUserProfile", "Perfil de Usuario" },
                { "LblLoginSync", "Inicia sesión para sincronizar tus configuraciones." },
                { "BtnLogin", "Iniciar Sesión" },
                { "LblPlansBilling", "Planes y Facturación" },
                { "LblCurrentPlan", "Plan Actual: Aloud Free" },
                { "LblFreePlanDesc", "Estás utilizando la versión gratuita con voces estándar del sistema." },
                { "BtnUpgradePro", "Mejorar a PRO" },
                { "LblSendFeedback", "Enviar Feedback" },
                { "LblDescribeError", "Describe el error, comentario o sugerencia con detalle" },
                { "BtnAttachPhoto", "📷 Adjuntar Foto" },
                { "LblNoPhoto", "Ninguna foto seleccionada" },
                { "BtnSendFeedbackAction", "Enviar Feedback" },
                { "LblDictadoSubtitle", "Dictado" },
                { "LblAtajo4Title", "Tocar palabra" },
                { "LblAtajo4Desc", "Mantén presionado para hablar y transcribir" },
                { "LblAtajo5Title", "Manos libres" },
                { "LblAtajo5Desc", "Toca una vez para empezar a grabar y de nuevo para detener" },
                { "StatusListening", "Escuchando..." },
                { "StatusDictating", "Dictando (Escuchando...)" },
                { "StatusTranscribing", "Transcribiendo..." },
                { "LblLoginTitle", "Iniciar Sesión" },
                { "LblLoginSubtitle", "Te damos la bienvenida a Aloud" },
                { "LblEmail", "Correo Electrónico" },
                { "LblPassword", "Contraseña" },
                { "BtnLoginAction", "Ingresar" },
                { "LblCreateAccount", "¿No tienes cuenta? Regístrate" },
                { "LblOr", "o continuar con" },
                { "BtnLogout", "Cerrar Sesión" },
                { "LblAccountTitle", "Mi Cuenta" }
            }},
            { "en", new Dictionary<string, string>() {
                { "AppTitle", "Aloud" },
                { "MenuInicio", "  Home" },
                { "MenuVoces", "  Languages & Voices" },
                { "MenuAtajos", "  Shortcuts" },
                { "MenuInsights", "  Statistics" },
                { "MenuAjustes", "  Settings" },
                { "MenuSoporte", "  Support" },
                { "HeroTitle", "Make Windows sound like you" },
                { "HeroSubtitle", "Select any text and press Ctrl + Win + Z." },
                { "BtnTestVoice", "Voice Test" },
                { "StatusActive", "Hotkeys Active (Listening...)" },
                { "StatusBlocked", "Hotkeys Blocked by Windows" },
                { "LblVoice", "Primary Voice" },
                { "LblSpeed", "Reading Speed" },
                { "StatWordsSub", "words read today" },
                { "StatWpmSub", "average speed" },
                { "StatStreakSub", "on streak" },
                { "BtnFeedback", "Leave Feedback 📝" },
                { "TabSettingsTitle", "Settings & Configuration" },
                { "LblLanguage", "Interface Language" },
                { "TabShortcutsTitle", "Keyboard Shortcuts" },
                { "ShortcutZ", "Ctrl + Win + Z : Read / Pause" },
                { "ShortcutX", "Ctrl + Win + X : Slower" },
                { "ShortcutA", "Ctrl + Win + A : Faster" },
                { "TabVoicesTitle", "Languages & Voices" },
                { "TabInsightsTitle", "Usage Statistics" },
                { "BtnSendFeedbackAction", "Send Feedback" },
                { "LblNoPhoto", "No photo selected" },
                { "BtnAttachPhoto", "📷 Attach Photo" },
                { "LblDescribeError", "Describe the error, comment or suggestion in detail" },
                { "LblSendFeedback", "Send Feedback" },
                { "BtnUpgradePro", "Upgrade to PRO" },
                { "LblFreePlanDesc", "You are using the free version with standard system voices." },
                { "LblCurrentPlan", "Current Plan: Aloud Free" },
                { "LblPlansBilling", "Plans and Billing" },
                { "BtnLogin", "Log In" },
                { "LblLoginSync", "Log in to sync your settings." },
                { "LblUserProfile", "User Profile" },
                { "LblAccount", "Account" },
                { "LblDarkMode", "Dark Mode" },
                { "LblAppLanguage", "App Language" },
                { "LblLaunchAtLogin", "Launch app at login" },
                { "LblAppSettings", "App Settings" },
                { "LblReadingHistory", "Reading History" },
                { "LblKeepUsing", "Keep using Aloud every day!" },
                { "LblCurrentStreak", "Current Streak" },
                { "LblReadingsStarted", "Readings started today" },
                { "LblActivations", "Activations" },
                { "LblByListeningFast", "By listening faster" },
                { "LblTimeSaved", "Time Saved" },
                { "LblTotalProcessed", "Total processed" },
                { "LblWordsToday", "Words Today" },
                { "BtnResetHotkeys", "Reset to default" },
                { "LblAtajo3Title", "Increase Speed" },
                { "LblAtajo3Desc", "Speeds up playback" },
                { "LblAtajo2Title", "Decrease Speed" },
                { "LblAtajo2Desc", "Slow down the reading speed" },
                { "LblAtajo1Title", "Read / Pause" },
                { "LblAtajo1Desc", "Read selected text or pause reading" },
                { "LblAtajosDesc", "Choose your preferred shortcuts for using Aloud." },
                { "LblLecturaSubtitle", "Reading" },
                { "BtnSeleccionar", "Select" },
                { "BtnProbar", "Test" },
                { "LblAvailableVoices", "Available Voices" },
                { "LblLanguageSelect", "Language" },
                { "LblInicioDesc", "Select any text in Windows and press your hotkey to hear it instantly." },
                { "LblInicioTitle", "Smart Playback" },
                { "LblDictadoSubtitle", "Dictation" },
                { "LblAtajo4Title", "Touch word" },
                { "LblAtajo4Desc", "Hold to speak and transcribe text" },
                { "LblAtajo5Title", "Hands free" },
                { "LblAtajo5Desc", "Tap once to start recording and tap again to stop" },
                { "StatusListening", "Listening..." },
                { "StatusDictating", "Dictating (Listening...)" },
                { "StatusTranscribing", "Transcribing..." },
                { "LblLoginTitle", "Log In" },
                { "LblLoginSubtitle", "Welcome back to Aloud" },
                { "LblEmail", "Email Address" },
                { "LblPassword", "Password" },
                { "BtnLoginAction", "Sign In" },
                { "LblCreateAccount", "Don't have an account? Sign up" },
                { "LblOr", "or continue with" },
                { "BtnLogout", "Log Out" },
                { "LblAccountTitle", "My Account" }
            }}
        };

        private string Get(string key)
        {
            if (_dict.ContainsKey(_currentLanguage) && _dict[_currentLanguage].ContainsKey(key))
                return _dict[_currentLanguage][key];
            return key;
        }

        public string AppTitle => Get("AppTitle");
        public string MenuInicio => Get("MenuInicio");
        public string MenuVoces => Get("MenuVoces");
        public string MenuAtajos => Get("MenuAtajos");
        public string MenuInsights => Get("MenuInsights");
        public string MenuAjustes => Get("MenuAjustes");
        public string MenuSoporte => Get("MenuSoporte");
        public string HeroTitle => Get("HeroTitle");
        public string HeroSubtitle => Get("HeroSubtitle");
        public string BtnTestVoice => Get("BtnTestVoice");
        public string StatusActive => Get("StatusActive");
        public string StatusBlocked => Get("StatusBlocked");
        public string LblVoice => Get("LblVoice");
        public string LblSpeed => Get("LblSpeed");
        public string StatWordsSub => Get("StatWordsSub");
        public string StatWpmSub => Get("StatWpmSub");
        public string StatStreakSub => Get("StatStreakSub");
        public string BtnFeedback => Get("BtnFeedback");
        public string TabSettingsTitle => Get("TabSettingsTitle");
        public string LblLanguage => Get("LblLanguage");
        public string TabShortcutsTitle => Get("TabShortcutsTitle");
        public string ShortcutZ => Get("ShortcutZ");
        public string ShortcutX => Get("ShortcutX");
        public string ShortcutA => Get("ShortcutA");
        public string TabVoicesTitle => Get("TabVoicesTitle");
        public string TabInsightsTitle => Get("TabInsightsTitle");

                public string LblInicioTitle => Get("LblInicioTitle");
        public string LblInicioDesc => Get("LblInicioDesc");
        public string LblLanguageSelect => Get("LblLanguageSelect");
        public string LblAvailableVoices => Get("LblAvailableVoices");
        public string BtnProbar => Get("BtnProbar");
        public string BtnSeleccionar => Get("BtnSeleccionar");
        public string LblAtajosDesc => Get("LblAtajosDesc");
        public string LblAtajo1Title => Get("LblAtajo1Title");
        public string LblAtajo1Desc => Get("LblAtajo1Desc");
        public string LblAtajo2Title => Get("LblAtajo2Title");
        public string LblAtajo2Desc => Get("LblAtajo2Desc");
        public string LblAtajo3Title => Get("LblAtajo3Title");
        public string LblAtajo3Desc => Get("LblAtajo3Desc");
        public string BtnResetHotkeys => Get("BtnResetHotkeys");
        public string LblWordsToday => Get("LblWordsToday");
        public string LblTotalProcessed => Get("LblTotalProcessed");
        public string LblTimeSaved => Get("LblTimeSaved");
        public string LblByListeningFast => Get("LblByListeningFast");
        public string LblActivations => Get("LblActivations");
        public string LblReadingsStarted => Get("LblReadingsStarted");
        public string LblCurrentStreak => Get("LblCurrentStreak");
        public string LblKeepUsing => Get("LblKeepUsing");
        public string LblReadingHistory => Get("LblReadingHistory");
        public string LblAppSettings => Get("LblAppSettings");
        public string LblLaunchAtLogin => Get("LblLaunchAtLogin");
        public string LblAppLanguage => Get("LblAppLanguage");
        public string LblDarkMode => Get("LblDarkMode");
        public string LblAccount => Get("LblAccount");
        public string LblUserProfile => Get("LblUserProfile");
        public string LblLoginSync => Get("LblLoginSync");
        public string BtnLogin => Get("BtnLogin");
        public string LblPlansBilling => Get("LblPlansBilling");
        public string LblCurrentPlan => Get("LblCurrentPlan");
        public string LblFreePlanDesc => Get("LblFreePlanDesc");
        public string BtnUpgradePro => Get("BtnUpgradePro");
        public string LblSendFeedback => Get("LblSendFeedback");
        public string LblDescribeError => Get("LblDescribeError");
        public string BtnAttachPhoto => Get("BtnAttachPhoto");
        public string LblNoPhoto => Get("LblNoPhoto");
        public string BtnSendFeedbackAction => Get("BtnSendFeedbackAction");

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

