# 📖 Lector de Texto Global

Un potente lector de texto en segundo plano para Windows que te permite escuchar cualquier texto seleccionado en cualquier aplicación usando atajos de teclado globales.

## ✨ Características Principales
- **Lectura instantánea:** Selecciona texto en cualquier programa (Chrome, Word, Bloc de notas, etc.), presiona el atajo y el sistema lo leerá en voz alta de inmediato.
- **Portapapeles seguro:** Copia el texto para leerlo temporalmente y luego devuelve al portapapeles exactamente lo que tenías copiado antes de usar el lector. ¡No interrumpirá tu trabajo!
- **Control de reproducción en vivo:** Puedes pausar, reanudar y ajustar la velocidad de la voz en tiempo real sin tener que abrir ninguna interfaz.
- **Sistema de colas:** ¿Quieres armar una lista de lectura? Mantén presionado el atajo de lectura por un segundo para agregar textos a una cola para que se lean de forma continua.
- **Invisible:** Se ejecuta completamente en segundo plano, sin molestas ventanas de consola.

## ⌨️ Atajos de Teclado

*Nota: Asegúrate de seleccionar el texto que deseas que el programa lea antes de presionar el atajo principal.*

- **`Win + Ctrl + Z`** : Leer el texto seleccionado / Pausar / Reanudar lectura.
  - *(Si mantienes presionado este atajo por casi un segundo, escucharás un "beep" y el texto seleccionado se agregará a la cola de lectura en lugar de interrumpir la voz actual).*
- **`Win + Ctrl + X`** : Bajar la velocidad de la voz.
- **`Win + Ctrl + A`** : Subir la velocidad de la voz.

## ⚙️ ¿Cómo funciona?
El sistema es altamente eficiente y no requiere instalación formal, estando compuesto por tres elementos:
1. **VBScript (`LectorTexto.vbs`):** Se encarga de iniciar todo el proceso de forma silenciosa para que no veas ventanas negras al iniciar.
2. **PowerShell (`RunLector.ps1`):** Lee el código fuente del lector, lo compila directamente en la memoria RAM y lo ejecuta.
3. **C# (`LectorGlobal.cs`):** Es el corazón de la aplicación. Escucha los atajos de teclado a nivel del sistema, interactúa con el portapapeles y envía el texto al motor de síntesis de voz (Text-to-Speech) de Windows.

## 🚀 Instalación y Uso
Actualmente, el proyecto se inicializa ejecutando el archivo `LectorTexto.vbs` (ubicado temporalmente en `C:\Antigravity_proyectos\LectorTexto.vbs`). Una vez que el script se ha ejecutado, puedes seleccionar texto y empezar a usar los atajos de teclado libremente.

---
*Desarrollado con la ayuda de Antigravity IDE.*
