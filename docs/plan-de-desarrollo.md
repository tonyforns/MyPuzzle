# Plan de desarrollo

Este es el orden de construcción. El [plan de implementación](plan-de-implementacion.md) define las mecánicas. Acá cada fase se parte en tareas, con los archivos que se crean y la comprobación que la cierra. Se implementa una fase completa antes de abrir la siguiente.

## Entorno

- Unity **6000.3.13f1**, plantilla **2D (URP)**. Es la versión de Unity 6 más nueva instalada en esta máquina.
- Paquetes: Input System y Test Framework (este último ya viene con el editor).
- Cámara ortográfica, plano de juego en XY. Una celda mide 1 unidad de mundo. Un puzzle 4×3 ocupa 4×3 unidades.
- La escena de trabajo es `Assets/Scenes/Table.unity`.

## Regla de corte

`MyPuzzle.Core` no referencia `UnityEngine` (`noEngineReferences: true`). Ahí viven las reglas. `PuzzleSettings` es el único `ScriptableObject`, y solo guarda una copia de `PuzzleConfig` para el inspector. La sesión lee `PuzzleConfig`, no el asset.

| Assembly | Referencias |
| --- | --- |
| `MyPuzzle.Core` | Ninguna de motor |
| `MyPuzzle.View` | Core, Unity, Input System, URP |
| `MyPuzzle.Tests` | Core y Test Framework. Pruebas en modo Edit, sin escena |

## Entregas

| Entrega | Fases | Qué se puede hacer al cerrarla |
| --- | --- | --- |
| 1. Mesa con la foto partida | 0, 1, 2 | Play muestra la imagen de prueba reconstruida en su sitio |
| 2. Piezas que se mueven | 3, 4, 5 | Esas piezas se reparten y se arrastran |
| 3. Rompecabezas jugable | 6 | Se encajan y la partida termina |
| 4. Aspecto de cartón y loop | 7, 8 | Curva, marco y elección de una foto del disco |

## Fase 0. Proyecto y contrato de datos

1. Crear el proyecto Unity 2D (URP) en la raíz del repositorio, con la escena `Table`.
2. Activar Input System como input activo.
3. Crear los tres `.asmdef` con los límites de la tabla de arriba.
4. En Core, agregar los tipos:
   - `CellId`, `Direction`, `EdgeKind`, `EdgeId`
   - `Pose` (posición X Y y cuartos de giro)
   - `PuzzleConfig` (columnas, filas, tamaño de celda, umbral, márgenes, nombre del preset)
   - `PuzzleDefinition`, `GroupState`, `PuzzleState`, `SessionPhase`
   - `GridTopology`
   - `PuzzleStateFactory.CreateEmpty(config, seed)` : un grupo por celda, giro 0, pose en la solución
5. En View, agregar `PuzzleSettings : ScriptableObject` y los assets `Easy`, `Medium`, `Hard`, `Expert`.
6. Prueba `EmptyStateTests`: un 4×3 produce 12 grupos, cada uno con una celda, giro 0 y fase `Armando`.

**Cierra cuando** la prueba pasa en el Test Runner y los cuatro assets aparecen en el inspector con sus grillas (4×3, 6×4, 8×6, 10×8).

## Fase 1. Generar la grilla

1. `PuzzleGenerator.Create(config, seed)` devuelve la definición con un registro por lado interno.
2. Cada celda expone su contorno rectangular, en el orden norte → este → sur → oeste, con el encastre ya resuelto para esa pieza (la vecina ve el complemento).
3. Prueba `PuzzleGeneratorTests` sobre 4×3 y 10×8:
   - un solo dueño por lado interno
   - pestaña contra hueco
   - marco recto
   - la misma semilla repite el resultado
   - semillas distintas cambian al menos un encastre

**Cierra cuando** esas pruebas pasan sin crear GameObjects.

## Fase 2. Ver la imagen reconstruida

1. Textura de prueba `Assets/Art/TestImage.png`, con relación de aspecto distinta de 1:1, importada como sprite y también legible como `Texture2D`.
2. `PieceMeshBuilder.Build(contorno, columnas, filas, celda)` genera el mesh de la cara con UVs sobre la textura completa.
3. `PuzzleSession` en la escena `Table` lee el preset Easy, genera con una semilla fija, instancia una pieza por celda y aplica la pose de la solución.
4. Material URP sin iluminación para que el color de la textura no se lave.
5. Cámara encuadrada al rectángulo 4×3 con un margen.

**Cierra cuando** en Play se reconoce la imagen completa, sin huecos entre celdas y sin estirarla.

## Fase 3. Grupos como única forma de mover

1. `GroupOperations.Move(state, groupId, destino)` devuelve un estado nuevo.
2. `GroupOperations.Merge(state, sourceId, targetId)` fusiona en el grupo destino y recalcula los offsets de grilla desde el ancla.
3. La vista no escribe posiciones por su cuenta: cada frame aplica la pose derivada del estado.
4. Prueba `GroupOperationsTests`: mover no cambia miembros ni giro. Fusionar dos vecinas deja un grupo, y la pose de ese grupo ubica ambas celdas en su lugar de grilla.
5. Atajo temporal en la escena (tecla de debug) que mueve el grupo de la celda `(0, 0)` una unidad a la derecha, para ver el modelo en Play.

**Cierra cuando** la prueba pasa y, en Play, esa pieza se desplaza junta con su estado.

## Fase 4. Arrastrar

1. `DragController` con Input System: puntero sobre el plano Z = 0.
2. Órdenes de sesión: `Tomar(punto)`, `Mover(punto)`, `Soltar()`.
3. Al tomar, el grupo sube el orden de dibujo. Al soltar, lo conserva por encima de lo que quedó debajo.
4. El grupo no puede salir del rectángulo visible de la cámara.
5. El controlador no consulta columnas, filas ni vecinos.

**Cierra cuando** en Play se toma cualquier pieza de la solución, se mueve y se suelta, y la pose del grupo coincide con lo que se ve.

## Fase 5. Repartir en la mesa

1. `TableLayout.Scatter(definition, seed, marco, zona)` devuelve la pose inicial de cada grupo.
2. Separación mínima entre centros, margen al marco, reintento con menos separación y, si no entra, desplazamiento chico.
3. `PuzzleConfig` gana el flag `EmpezarResuelto`. Los assets de desarrollo lo dejan en verdadero hasta cerrar la fase 6. El flujo normal lo deja en falso.
4. Prueba `TableLayoutTests`: misma semilla, mismas posiciones. Todas las posiciones caen dentro de la zona. Un 10×8 entra en la zona prevista.

**Cierra cuando** en Play, con `EmpezarResuelto` apagado, las 12 piezas salen sueltas y una segunda ejecución con la misma semilla las deja iguales.

## Fase 6. Encajar y terminar

1. `SnapSystem.Resolve(state, groupId, umbral)` devuelve el estado después de soltar.
2. `Soltar` de la sesión llama a `Resolve` y, si la fase pasa a `Completo`, ignora tomas nuevas.
3. Pruebas `SnapSystemTests`:
   - vecina más lejos que el umbral sigue separada
   - vecina dentro del umbral queda en el mismo grupo y alineada
   - una celda cercana que no es vecina no se une
   - dos grupos de varias piezas se fusionan por un lado compartido
   - cerrar la última pieza deja la fase en `Completo`
4. En la escena, con `EmpezarResuelto` activo: separar dos piezas, acercarlas y ver el encaje. Luego armar el 4×3 hasta el final.

**Cierra cuando** esas pruebas pasan y en Play el 4×3 se puede terminar, momento en el que el arrastre deja de responder.

## Fase 7. Aspecto de cartón

1. `PieceOutline` reemplaza el rectángulo por la curva de cuello y cabeza. `Tab` sale, `Blank` entra. El registro de lados de la fase 1 no cambia.
2. El mesh suma el canto. La sombra es un sprite hijo y no recibe el raycast.
3. Marco del área de armado y referencia atenuada, con una tecla o un toggle para ocultarla.
4. Prueba nueva: el contorno cierra, y la curva de una pestaña es el espejo del hueco vecino. Las pruebas de la fase 1 siguen pasando.

**Cierra cuando** el 4×3 se ve como rompecabezas y el encaje de la fase 6 sigue uniendo las mismas vecinas.

## Fase 8. Loop de partida

1. `ImageImportService` abre PNG o JPG, rechaza lo que no puede decodificar y calcula el rectángulo del marco sin deformar la foto.
2. `PuzzleHud`: vista previa, cuatro presets, mezclar (nueva semilla) y otra imagen.
3. Al completar, centrar el grupo en el marco, ocultar cantos y mostrar la textura continua con una transición corta.
4. Mezclar y cambiar imagen destruyen las piezas de la vista y las vuelven a crear desde un estado nuevo.

**Cierra cuando** una foto elegida desde el disco recorre generar, mezclar, arrastrar, encajar, terminar y volver a empezar.

## Primera sesión de código

La sesión 1 implementa la fase 0 entera y se detiene en su condición de cierre. No incluye el generador de encastres ni meshes.

Archivos de esa sesión:

```
Assets/Scripts/Core/MyPuzzle.Core.asmdef
Assets/Scripts/Core/CellId.cs
Assets/Scripts/Core/Direction.cs
Assets/Scripts/Core/EdgeKind.cs
Assets/Scripts/Core/EdgeId.cs
Assets/Scripts/Core/Pose.cs
Assets/Scripts/Core/PuzzleConfig.cs
Assets/Scripts/Core/PuzzleDefinition.cs
Assets/Scripts/Core/GroupState.cs
Assets/Scripts/Core/PuzzleState.cs
Assets/Scripts/Core/GridTopology.cs
Assets/Scripts/Core/PuzzleStateFactory.cs
Assets/Scripts/View/MyPuzzle.View.asmdef
Assets/Scripts/View/PuzzleSettings.cs
Assets/Puzzle/Settings/Easy.asset
Assets/Puzzle/Settings/Medium.asset
Assets/Puzzle/Settings/Hard.asset
Assets/Puzzle/Settings/Expert.asset
Assets/Tests/MyPuzzle.Tests.asmdef
Assets/Tests/EmptyStateTests.cs
Assets/Scenes/Table.unity
```

Al cerrar la fase 0 se sigue con la fase 1 en la sesión siguiente.
