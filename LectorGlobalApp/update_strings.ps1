$stringsPath = "C:\Antigravity_proyectos\Lector e IDE\LectorGlobalApp\Strings.cs"
$xamlPath = "C:\Antigravity_proyectos\Lector e IDE\LectorGlobalApp\MainWindow.xaml"

$newEs = @{
    "LblInicioTitle" = "Reproducción Inteligente"
    "LblInicioDesc" = "Selecciona cualquier texto en Windows y presiona tu atajo de teclado para escucharlo al instante."
    "LblLanguageSelect" = "Idioma"
    "LblAvailableVoices" = "Voces Disponibles"
    "BtnProbar" = "Probar"
    "BtnSeleccionar" = "Seleccionar"
    "LblAtajosDesc" = "Elige tus atajos preferidos para usar Aloud."
    "LblAtajo1Title" = "Leer Selección / Pausar"
    "LblAtajo1Desc" = "Mantenlo para decir algo corto (o leer la selección)"
    "LblAtajo2Title" = "Disminuir Velocidad"
    "LblAtajo2Desc" = "Reduce la velocidad de lectura"
    "LblAtajo3Title" = "Aumentar Velocidad"
    "LblAtajo3Desc" = "Acelera la lectura"
    "BtnResetHotkeys" = "Restablecer por defecto"
    "LblWordsToday" = "Palabras Hoy"
    "LblTotalProcessed" = "Total procesadas"
    "LblTimeSaved" = "Tiempo Ahorrado"
    "LblByListeningFast" = "Por escuchar rápido"
    "LblActivations" = "Activaciones"
    "LblReadingsStarted" = "Lecturas iniciadas hoy"
    "LblCurrentStreak" = "Racha Actual"
    "LblKeepUsing" = "¡Sigue usando Aloud cada día!"
    "LblReadingHistory" = "Historial de Lectura"
    "LblAppSettings" = "Ajustes de la aplicación"
    "LblLaunchAtLogin" = "Iniciar la app al arrancar"
    "LblAppLanguage" = "Idioma de la Interfaz"
    "LblDarkMode" = "Modo Oscuro"
    "LblAccount" = "Cuenta"
    "LblUserProfile" = "Perfil de Usuario"
    "LblLoginSync" = "Inicia sesión para sincronizar tus configuraciones."
    "BtnLogin" = "Iniciar Sesión"
    "LblPlansBilling" = "Planes y Facturación"
    "LblCurrentPlan" = "Plan Actual: Aloud Free"
    "LblFreePlanDesc" = "Estás utilizando la versión gratuita con voces estándar del sistema."
    "BtnUpgradePro" = "Mejorar a PRO"
    "LblSendFeedback" = "Enviar Feedback"
    "LblDescribeError" = "Describe el error, comentario o sugerencia con detalle"
    "BtnAttachPhoto" = "📷 Adjuntar Foto"
    "LblNoPhoto" = "Ninguna foto seleccionada"
    "BtnSendFeedbackAction" = "Enviar Feedback"
}

$newEn = @{
    "LblInicioTitle" = "Smart Playback"
    "LblInicioDesc" = "Select any text in Windows and press your hotkey to hear it instantly."
    "LblLanguageSelect" = "Language"
    "LblAvailableVoices" = "Available Voices"
    "BtnProbar" = "Test"
    "BtnSeleccionar" = "Select"
    "LblAtajosDesc" = "Choose your preferred shortcuts for using Aloud."
    "LblAtajo1Title" = "Read Selection / Pause"
    "LblAtajo1Desc" = "Hold to say something short (or read selection)"
    "LblAtajo2Title" = "Decrease Speed"
    "LblAtajo2Desc" = "Slow down the reading speed"
    "LblAtajo3Title" = "Increase Speed"
    "LblAtajo3Desc" = "Speed up the reading"
    "BtnResetHotkeys" = "Reset to default"
    "LblWordsToday" = "Words Today"
    "LblTotalProcessed" = "Total processed"
    "LblTimeSaved" = "Time Saved"
    "LblByListeningFast" = "By listening faster"
    "LblActivations" = "Activations"
    "LblReadingsStarted" = "Readings started today"
    "LblCurrentStreak" = "Current Streak"
    "LblKeepUsing" = "Keep using Aloud every day!"
    "LblReadingHistory" = "Reading History"
    "LblAppSettings" = "App Settings"
    "LblLaunchAtLogin" = "Launch app at login"
    "LblAppLanguage" = "App Language"
    "LblDarkMode" = "Dark Mode"
    "LblAccount" = "Account"
    "LblUserProfile" = "User Profile"
    "LblLoginSync" = "Log in to sync your settings."
    "BtnLogin" = "Log In"
    "LblPlansBilling" = "Plans and Billing"
    "LblCurrentPlan" = "Current Plan: Aloud Free"
    "LblFreePlanDesc" = "You are using the free version with standard system voices."
    "BtnUpgradePro" = "Upgrade to PRO"
    "LblSendFeedback" = "Send Feedback"
    "LblDescribeError" = "Describe the error, comment or suggestion in detail"
    "BtnAttachPhoto" = "📷 Attach Photo"
    "LblNoPhoto" = "No photo selected"
    "BtnSendFeedbackAction" = "Send Feedback"
}

$strings = Get-Content -Raw -Encoding UTF8 $stringsPath

foreach ($key in $newEs.Keys) {
    if (-not $strings.Contains($key)) {
        $val = $newEs[$key]
        $strings = $strings.Replace('{ "TabInsightsTitle", "Estadísticas de Uso" }', "{ ""TabInsightsTitle"", ""Estadísticas de Uso"" },`r`n                { ""$key"", ""$val"" }")
    }
}

foreach ($key in $newEn.Keys) {
    if (-not $strings.Contains($key)) {
        $val = $newEn[$key]
        $strings = $strings.Replace('{ "TabInsightsTitle", "Usage Statistics" }', "{ ""TabInsightsTitle"", ""Usage Statistics"" },`r`n                { ""$key"", ""$val"" }")
    }
}

$props = ""
foreach ($key in $newEs.Keys) {
    if (-not $strings.Contains("public string $key =>")) {
        $props += "        public string $key => Get(`"$key`");`r`n"
    }
}

$strings = $strings.Replace("public event PropertyChangedEventHandler PropertyChanged;", "$props`r`n        public event PropertyChangedEventHandler PropertyChanged;")
Set-Content -Path $stringsPath -Value $strings -Encoding UTF8

$xaml = Get-Content -Raw -Encoding UTF8 $xamlPath

$replacements = @{
    'Text="Reproducción Inteligente"' = 'Text="{Binding LblInicioTitle}"'
    'Text="Selecciona cualquier texto en Windows y presiona tu atajo de teclado para escucharlo al instante."' = 'Text="{Binding LblInicioDesc}"'
    'Text="Idioma"' = 'Text="{Binding LblLanguageSelect}"'
    'Text="Voces Disponibles"' = 'Text="{Binding LblAvailableVoices}"'
    'Content="Probar"' = 'Content="{Binding BtnProbar}"'
    'Content="Seleccionar"' = 'Content="{Binding BtnSeleccionar}"'
    'Text="Choose your preferred shortcuts for using Aloud."' = 'Text="{Binding LblAtajosDesc}"'
    'Text="Leer Selección / Pausar"' = 'Text="{Binding LblAtajo1Title}"'
    'Text="Hold to say something short (or read selection)"' = 'Text="{Binding LblAtajo1Desc}"'
    'Text="Disminuir Velocidad"' = 'Text="{Binding LblAtajo2Title}"'
    'Text="Slow down the reading speed"' = 'Text="{Binding LblAtajo2Desc}"'
    'Text="Aumentar Velocidad"' = 'Text="{Binding LblAtajo3Title}"'
    'Text="Speed up the reading"' = 'Text="{Binding LblAtajo3Desc}"'
    'Content="Reset to default"' = 'Content="{Binding BtnResetHotkeys}"'
    'Text="🗣️ Palabras Hoy"' = 'Text="{Binding LblWordsToday, StringFormat=''🗣️ \{0\}''}"'
    'Text="Total procesadas"' = 'Text="{Binding LblTotalProcessed}"'
    'Text="⏱️ Tiempo Ahorrado"' = 'Text="{Binding LblTimeSaved, StringFormat=''⏱️ \{0\}''}"'
    'Text="Por escuchar rápido"' = 'Text="{Binding LblByListeningFast}"'
    'Text="⚡ Activaciones"' = 'Text="{Binding LblActivations, StringFormat=''⚡ \{0\}''}"'
    'Text="Lecturas iniciadas hoy"' = 'Text="{Binding LblReadingsStarted}"'
    'Text="🔥 Racha Actual"' = 'Text="{Binding LblCurrentStreak, StringFormat=''🔥 \{0\}''}"'
    'Text="¡Sigue usando Aloud cada día!"' = 'Text="{Binding LblKeepUsing}"'
    'Text="📚 Historial de Lectura"' = 'Text="{Binding LblReadingHistory, StringFormat=''📚 \{0\}''}"'
    'Text="App settings"' = 'Text="{Binding LblAppSettings}"'
    'Text="Launch app at login"' = 'Text="{Binding LblLaunchAtLogin}"'
    'Text="App Language"' = 'Text="{Binding LblAppLanguage}"'
    'Text="Modo Oscuro / Dark Mode"' = 'Text="{Binding LblDarkMode}"'
    'Text="Account"' = 'Text="{Binding LblAccount}"'
    'Text="Perfil de Usuario"' = 'Text="{Binding LblUserProfile}"'
    'Text="Inicia sesión para sincronizar tus configuraciones."' = 'Text="{Binding LblLoginSync}"'
    'Content="Iniciar Sesión"' = 'Content="{Binding BtnLogin}"'
    'Text="Plans and Billing"' = 'Text="{Binding LblPlansBilling}"'
    'Text="Plan Actual: Aloud Free"' = 'Text="{Binding LblCurrentPlan}"'
    'Text="Estás utilizando la versión gratuita con voces estándar del sistema."' = 'Text="{Binding LblFreePlanDesc}"'
    'Content="Mejorar a PRO"' = 'Content="{Binding BtnUpgradePro}"'
    'Text="Enviar Feedback"' = 'Text="{Binding LblSendFeedback}"'
    'Text="Describe el error, comentario o sugerencia con detalle"' = 'Text="{Binding LblDescribeError}"'
    'Content="📷 Adjuntar Foto"' = 'Content="{Binding BtnAttachPhoto}"'
    'Text="Ninguna foto seleccionada"' = 'Text="{Binding LblNoPhoto}"'
}

foreach ($key in $replacements.Keys) {
    $xaml = $xaml.Replace($key, $replacements[$key])
}

# The feedback button in XAML has 'Content="Enviar Feedback"', but the title is 'Text="Enviar Feedback"'
$xaml = $xaml.Replace('Content="Enviar Feedback"', 'Content="{Binding BtnSendFeedbackAction}"')

Set-Content -Path $xamlPath -Value $xaml -Encoding UTF8
Write-Output "Hecho!"
