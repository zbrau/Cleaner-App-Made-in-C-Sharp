# Ver Releases para descargar el instalador 


# AppCleanerPro (NovaDash Design)

AppCleanerPro es una solución integral de optimización, limpieza y seguridad para sistemas operativos Windows, desarrollada sobre el framework .NET 10. La aplicación combina herramientas de diagnóstico avanzado con una interfaz moderna basada en NovaDash Design para ofrecer un control total sobre el rendimiento y la privacidad del equipo.

![App Status](https://img.shields.io/badge/Status-Development-blueviolet?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows-0078d7?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/Framework-.NET%2010-512bd4?style=for-the-badge&logo=dotnet)

## Características y Funcionalidades Completas

### Módulo de Limpieza y Optimización
*   Dashboard: Panel de control con resumen en tiempo real del estado de salud del sistema.
*   Limpieza de Sistema: Escaneo y eliminación de archivos temporales, caché de aplicaciones, logs y basura residual de Windows.
*   Limpiador de Registro: Identifica y repara extensiones de archivo nulas, asociaciones corruptas y entradas obsoletas en el Registro de Windows.
*   RAM Booster: Monitoreo dinámico del consumo de memoria y liberación de recursos en tiempo real para mejorar la respuesta del sistema.

### Análisis de Almacenamiento
*   Analizador de Disco: Herramienta de exploración profunda para identificar carpetas que consumen mayor espacio.
*   Localizador de Archivos Gigantes: Filtra y muestra los 100 archivos de mayor volumen para facilitar la liberación selectiva de espacio.
*   Buscador de Clones: Algoritmo de comparación bit a bit para detectar archivos duplicados exactos, independientemente de su nombre.
*   Disk Treemap: Visualización gráfica y proporcional del uso del disco duro para una comprensión intuitiva del almacenamiento.

### Seguridad y Privacidad Avanzada
*   Destructor de Archivos (Shredder): Eliminación permanente de datos sensibles mediante algoritmos de sobreescritura, impidiendo su recuperación mediante software forense.
*   Escáner de Privacidad: Identifica rastros de actividad, archivos de texto con credenciales expuestas y riesgos potenciales de seguridad local.
*   Monitor de Red (TCP/UDP): Supervisión en tiempo real de todas las conexiones entrantes y salientes para detectar tráfico sospechoso.
*   Gestor de Firewall: Interfaz simplificada para la administración de reglas y políticas de tráfico de red.

### Herramientas de Gestión de Sistema
*   Administrador de Procesos: Control avanzado de procesos activos con capacidad para detener servicios bloqueados y optimizar el consumo de CPU.
*   Gestor de Arranque: Administración de las aplicaciones y servicios que inician automáticamente con el sistema operativo para acelerar el encendido del PC.
*   Desinstalador de Programas: Eliminación completa de aplicaciones, incluyendo la limpieza de registros y carpetas residuales post-desinstalación.
*   Información del Sistema: Diagnóstico detallado del hardware (CPU, GPU, RAM) y especificaciones técnicas del software instalado.

### Administración y Soporte
*   Historial de Limpieza: Registro detallado de todas las acciones de mantenimiento realizadas por el usuario.
*   Configuración: Personalización del comportamiento de la aplicación, temas visuales y preferencias de escaneo.
*   Aviso Legal y de Seguridad: Sección dedicada a la transparencia técnica, detallando el uso de privilegios de administrador y la interacción con el registro para mayor confianza del usuario.

## Requisitos Técnicos y Seguridad
La aplicación ha sido diseñada para interactuar con capas profundas de Windows (Microsoft.Win32 y System.Diagnostics), lo que garantiza una limpieza profunda pero requiere las siguientes consideraciones:

*   Privilegios Elevados: El ejecutable debe ejecutarse como administrador para gestionar procesos del sistema y acceder a directorios protegidos.
*   Análisis Heurístico: Debido a que la aplicación lee procesos activos y modifica el Registro sin contar con firma digital comercial en fase de desarrollo, herramientas como Windows Defender pueden marcarla como sospechosa por precaución. El software es 100% seguro y está libre de código malicioso.

## Especificaciones de Desarrollo
*   Lenguaje: C#
*   Interfaz: WPF (Windows Presentation Foundation)
*   Renderizado: XAML con aceleración por hardware
*   Target: .NET 10.0-windows

---
AppCleanerPro: Profesionalismo, transparencia y potencia en el mantenimiento de Windows.
