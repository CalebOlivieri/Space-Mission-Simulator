Simulador de Misiones Espaciales (WPF)

Proyecto WPF minimal en C# que implementa: 
- Modelos orientados a objetos: Star, Planet, Moon, Ship, Mission.
- Patrón MVVM con `MainViewModel` y `RelayCommand`.
- Visualización simple usando `Canvas` y `ItemsControl`.

Cómo compilar y ejecutar (Windows con .NET 7 SDK instalado):

Abrir PowerShell en la carpeta del proyecto y ejecutar:

```powershell
cd "c:\Users\angel\OneDrive\Documents\Universidad\Proyecto COMP2400\SpaceMissionSimulator"
dotnet build
dotnet run --project SpaceMissionSimulator.csproj
```

Controles en la UI:
- Añadir Estrella / Planeta / Luna / Nave.
- Iniciar / Pausar simulación.
- Lista lateral muestra objetos y misiones.

Siguientes mejoras posibles:
- Interacción de selección y edición de propiedades.
- Más realismo físico (velocidades orbitales calculadas por gravitación).
- Arrastrar objetos en el canvas y zoom/pan.
