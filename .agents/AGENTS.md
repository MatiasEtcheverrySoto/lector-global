# 🤖 Guía para Agentes IA - Proyecto Lector de Texto Global

## 📌 Contexto del Proyecto
Este proyecto es un **lector de texto en segundo plano** para Windows (LectorGlobal). Permite al usuario seleccionar cualquier texto en el sistema, presionar un atajo de teclado y escucharlo mediante síntesis de voz (`System.Speech.Synthesis`), restaurando el contenido original del portapapeles una vez capturado.

## 📁 Arquitectura y Ubicación de Archivos
Actualmente, los archivos principales del código fuente se encuentran **fuera** de esta carpeta:
- **Script VBS de entrada:** `C:\Antigravity_proyectos\LectorTexto.vbs` (Lanza el script de PowerShell de forma oculta).
- **Script de PowerShell:** `C:\Users\matie\.gemini\antigravity-ide\brain\204567db-dde4-46c8-ab3f-2490ab12144f\scratch\RunLector.ps1` (Detiene instancias anteriores, compila en memoria el C# y ejecuta la aplicación).
- **Código Fuente C#:** `C:\Users\matie\.gemini\antigravity-ide\brain\204567db-dde4-46c8-ab3f-2490ab12144f\scratch\LectorGlobal.cs` (Contiene la lógica core).
- **Log de Ejecución:** `C:\Users\matie\.gemini\antigravity-ide\brain\204567db-dde4-46c8-ab3f-2490ab12144f\scratch\lector_log.txt`

## 🛠️ Tecnologías Utilizadas
- **C# y Windows Forms:** Para el bucle de mensajes (Message Loop) necesario para escuchar los Global Hotkeys de Windows.
- **PowerShell:** Para compilar el código C# "al vuelo" mediante `Add-Type` y ejecutarlo en memoria, evitando compilar un `.exe` permanentemente.
- **VBScript:** Para ejecutar el PowerShell de forma totalmente invisible (sin ventana de consola).

## 🎮 Funcionamiento Clave (LectorGlobal.cs)
- **Hotkeys Globales (User32.dll):**
  - `Win + Ctrl + Z`: Leer el texto seleccionado / Pausar / Reanudar. (Si la tecla `Z` se mantiene presionada por ~800ms, emite un "beep" y **encola** el texto sin detener el actual).
  - `Win + Ctrl + X`: Bajar la velocidad de lectura.
  - `Win + Ctrl + A`: Subir la velocidad de lectura.
- **Manejo del Portapapeles:**
  1. Guarda el estado actual del portapapeles.
  2. Simula `Ctrl+C` (`SendKeys.SendWait`) para copiar la selección.
  3. Filtra emojis y caracteres extraños (usando Regex y chequeo de surrogates) que trabarían al motor de voz.
  4. Restaura el texto original al portapapeles para no interrumpir el flujo de trabajo del usuario.
- **Gestión de Voz (`SpeechSynthesizer`):** Intenta priorizar voces instaladas localmente llamadas "Raul" o "Pablo", y si no, "Sabina" o cualquier voz en español.

## ⚠️ Consideraciones para Futuros Cambios
1. **No rompas el Hotkey Hook:** La clase `DummyForm` oculta es necesaria para recibir los mensajes nativos de Windows (`WndProc`) y procesar los atajos. El thread necesita `Application.Run` para que los hooks sigan activos.
2. **Paths Absolutos Harcodeados:** El script `RunLector.ps1` y `LectorGlobal.cs` usan paths absolutos harcodeados a la carpeta oculta de Gemini (ver arriba). Si decides traer el código a esta carpeta del proyecto, **debes actualizar rigurosamente las rutas** en `RunLector.ps1` (`$source = Get-Content ...`), en `LectorGlobal.cs` (`string logFile = ...`) y en `LectorTexto.vbs`.
3. **Manejo Asíncrono de Eventos:** Ten cuidado con los bloqueos. Al cambiar de velocidad, el código cancela la lectura y la reinicia desde donde estaba. La lectura de hotkeys verifica si la tecla está presionada usando `GetAsyncKeyState`.
