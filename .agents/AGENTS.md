# 🤖 Guía para Agentes IA - Proyecto Lector de Texto Global

## 📌 Contexto del Proyecto
Este proyecto es un **lector de texto en segundo plano** para Windows (LectorGlobal). Permite al usuario seleccionar cualquier texto en el sistema, presionar un atajo de teclado y escucharlo mediante síntesis de voz (`System.Speech.Synthesis`), restaurando el contenido original del portapapeles una vez capturado.

## 📁 Arquitectura y Ubicación de Archivos
El proyecto ahora es una **aplicación de escritorio C# WPF** bajo el directorio `LectorGlobalApp`.
- **UI (Frontend):** Se diseña completamente en archivos XAML (ej. `MainWindow.xaml`, `LoginWindow.xaml`).
- **Lógica (Backend Local):** Código en los archivos de code-behind (ej. `MainWindow.xaml.cs`).
- **Archivo Ejecutable/Proyecto Principal:** `LectorGlobalApp.csproj` (se compila a `Aloud.exe`).

## 🛠️ Tecnologías Utilizadas
- **WPF (.NET 8):** Para la interfaz de usuario moderna.
- **Supabase (C# SDK):** Backend en la nube para autenticación y base de datos de usuarios (`SupabaseManager.cs`).
- **Local HTTP Listener (`LocalAuthServer.cs`):** Para interceptar la autenticación de OAuth (Google/Apple) redireccionando desde el navegador al puerto local `54321`.
- **User32.dll:** Hotkeys globales nativos de Windows para interactuar con la app en segundo plano.

## ⚠️ Consideraciones para Futuros Cambios (REGLAS OBLIGATORIAS)
1. **Regla Crítica de Compilación:** Al ejecutar comandos como `dotnet build` o `dotnet run`, el archivo compilado `Aloud.exe` puede quedar bloqueado si la aplicación sigue en ejecución o colgada en segundo plano.
   - 👉 **ACCIÓN OBLIGATORIA:** SIEMPRE debes ejecutar `taskkill /F /IM Aloud.exe` o `Stop-Process -Name "Aloud" -Force -ErrorAction SilentlyContinue` antes de realizar un `dotnet build`.
2. **Offline-First (Estadísticas y Ajustes):** El usuario requiere que la app funcione sin conexión. Todos los cambios de estadísticas deben guardarse **inmediatamente** en el sistema de archivos (ej. `lector_stats.json`), e intentar sincronizarse con la nube en segundo plano solo si existe conexión (vía `SupabaseManager`).
3. **No rompas el Hotkey Hook:** Cuidado con bloquear el `Dispatcher` o el Thread principal de WPF, ya que esto congelaría la escucha de atajos de teclado globales. Las tareas pesadas deben usar `async/await`.
