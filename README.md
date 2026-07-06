# 📖 Lector de Texto Global

Una aplicación de escritorio moderna y potente para Windows que te permite escuchar cualquier texto seleccionado en tu computadora usando atajos de teclado globales. También cuenta con soporte para dictado de voz y análisis de estadísticas de lectura.

## ✨ Características Principales
- **Interfaz Moderna:** Aplicación construida en C# WPF con soporte para modo oscuro, panel de estadísticas en tiempo real y perfiles de usuario.
- **Lectura instantánea:** Selecciona texto en cualquier programa, presiona tu atajo configurado y el sistema lo leerá en voz alta de inmediato.
- **Portapapeles seguro:** La aplicación extrae el texto del portapapeles y te devuelve automáticamente lo que tenías copiado previamente. ¡No interrumpe tu flujo de trabajo!
- **Dictado de Voz:** Integración nativa con capacidades de dictado.
- **Control de reproducción:** Pausa, reanuda y ajusta la velocidad de lectura (-10x a 10x) sobre la marcha.
- **Sincronización en la Nube:** Sistema de autenticación de Supabase (Email, Google, Apple) y soporte *offline-first* para tus estadísticas (rachas y tiempo ahorrado).

## ⌨️ Atajos de Teclado (Personalizables)
Por defecto, la aplicación utiliza los siguientes atajos (que ahora pueden configurarse libremente desde la pestaña de Ajustes de la interfaz visual):

- **`Win + Ctrl + Z`** : Leer el texto seleccionado / Pausar / Reanudar lectura.
  - *(Mantenlo presionado por un segundo para agregar textos a la cola de lectura).*
- **`Win + Ctrl + X`** : Bajar la velocidad de la voz.
- **`Win + Ctrl + A`** : Subir la velocidad de la voz.
- **`Win + Ctrl + S`** : Activar/Desactivar dictado de voz.

## ⚙️ Arquitectura
- **Frontend:** C# WPF (.NET 8) utilizando XAML para diseño responsivo.
- **Backend Auth:** Supabase C# SDK (`SupabaseManager.cs`). Soporte de OAuth interceptado localmente a través de `LocalAuthServer.cs`.
- **Motor Interno:** `User32.dll` para hotkeys y `System.Speech.Synthesis` para conversión de texto a voz.

## 🚀 Instalación y Uso
1. Asegúrate de tener instalado el SDK de .NET 8.
2. Clona el repositorio y navega al directorio `Aloud`.
3. Ejecuta `dotnet run` para iniciar la aplicación.
4. (Opcional) Configura tus credenciales de Supabase en `SupabaseManager.cs` para activar la nube.

---
*Desarrollado con la ayuda de Antigravity IDE.*
