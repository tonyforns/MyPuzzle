# Arquitectura

MyPuzzle se implementa en Unity como una escena de mesa 2D. La generación del puzzle es un proceso puro (datos adentro, datos afuera) y la escena solo instancia las piezas y responde al input.

## Principios

- Una pieza es un componente. Un grupo es otro. La regla de encaje vive en un sistema aparte, no dentro del arrastre.
- La configuración de dificultad es un `ScriptableObject`, para poder probar presets sin tocar código.
- El generador no conoce la UI ni el `Transform`. Devuelve una descripción de grilla y de contorno. Otro paso construye los meshes y los coloca en la mesa.
- La primera versión mantiene las piezas en un plano. El orden visual es el `sortingOrder` del renderer, no la física 3D.

## Módulos

```
Imagen elegida
    → ImageImportService
    → PuzzleGenerator
    → PieceMeshBuilder
    → TableLayout
    → PuzzleSession
         ├── PuzzlePiece
         ├── PieceGroup
         └── SnapSystem
```

| Módulo | Responsabilidad |
| --- | --- |
| `ImageImportService` | Abre el archivo, valida formato y tamaño, crea la `Texture2D` y calcula el rectángulo final respetando la relación de aspecto |
| `PuzzleSettings` | `ScriptableObject` con presets de grilla, umbral de encaje, margen de la mesa y grosor visual del canto |
| `PuzzleGenerator` | Construye la grilla, asigna pestaña o hueco a cada lado interno y describe el contorno de cada celda |
| `PieceMeshBuilder` | Convierte el contorno en un mesh 2D con UVs sobre la textura completa, más el canto |
| `TableLayout` | Reparte las piezas en la zona libre sin centros demasiado juntos y deja libre el marco de armado |
| `PuzzleSession` | Dueña de la partida: imagen, grilla, lista de piezas, grupos y estado (armando o completo) |
| `PuzzlePiece` | Identidad de celda `(columna, fila)`, referencia al grupo, orden de dibujo y punto de anclaje |
| `PieceGroup` | Conjunto de piezas ya unidas. Guarda la traslación del bloque. Al arrastrar, mueve a todos sus miembros |
| `SnapSystem` | Al soltar, busca vecinos de grilla dentro del umbral y fusiona grupos |
| `DragController` | Traduce mouse y tacto a “tomar / mover / soltar” sobre el plano de la mesa |
| `PuzzleHud` | Selector de imagen, presets, vista previa, referencia atenuada y botones de reinicio |

## Modelo de la grilla

La grilla es rectangular: `columnas × filas`. Cada celda tiene cuatro lados: norte, este, sur, oeste.

Cada lado interno se guarda una sola vez, en la pieza de menor índice, con un valor:

- `Tab`: la pieza dueña del lado sobresale.
- `Blank`: la pieza dueña del lado se mete. La vecina recibe el complemento.

Los lados del marco exterior son `Edge` (rectos) y no participan del encaje.

El contorno de una pieza es una polilínea cerrada alrededor de su celda:

- Lado `Edge`: segmento recto.
- Lado `Tab` o `Blank`: curva de cuello y cabeza, espejada si es hueco.

La curva es la misma para todas las piezas de la partida, escalada al tamaño de celda. Así los encastres son intercambiables a la vista y encajan en datos.

Identidad de pieza: `(columna, fila)`. Dos piezas son vecinas si comparten un lado de esa grilla. Esa relación es la única que autoriza el encaje. La forma parecida no alcanza.

## Mesh y textura

Cada pieza usa la textura completa de la imagen y UVs que corresponden a su contorno, incluidas las pestañas. Así no hace falta recortar una textura por pieza.

El mesh tiene dos partes:

1. **Cara:** el contorno triangulado, con la imagen.
2. **Canto:** una franja extruida unos píxeles hacia abajo, con un color de cartón, para leer el grosor cuando las piezas se superponen.

La sombra es un sprite suave hijo de la pieza, desfasado, que no recibe raycasts.

## Grupos y arrastre

Al empezar, cada pieza es su propio grupo.

`DragController` hace un raycast al plano de la mesa. Si acierta una pieza, toma su `PieceGroup` completo, sube el `sortingOrder` del grupo y sigue el puntero hasta soltar.

Al soltar, `SnapSystem`:

1. Recorre los bordes libres del grupo (lados que todavía no tienen vecino dentro del mismo grupo).
2. Para cada borde, busca la pieza que la grilla marca como vecina.
3. Si esa vecina está en otro grupo, mide la distancia entre anclas y compara la orientación.
4. Si cumple el umbral, fusiona los dos grupos y alinea el que se acaba de soltar al sistema del otro.
5. Repite por si el movimiento dejó varios bordes listos a la vez (una pieza puede cerrar una esquina contra dos vecinas).

La fusión es transitiva: A unida a B, y ese bloque unido a C, producen un solo `PieceGroup`.

`PuzzleSession` escucha el tamaño del grupo mayor. Cuando es igual a `columnas × filas`, pasa al estado completo y el HUD muestra el cierre.

## Dispersión inicial

`TableLayout` coloca centros al azar en la zona de piezas con dos restricciones:

- Separación mínima entre centros, para que se pueda tomar cada pieza.
- Margen respecto del marco de armado y del borde de la cámara.

Si no encuentra sitio tras un número fijo de intentos, reduce la separación y vuelve a probar. Si aun así no entra, apila con un desplazamiento chico. La partida siempre arranca con todas las piezas visibles.

La semilla del azar (encastres y posiciones) se guarda en la sesión para poder repetir una mezcla durante las pruebas.

## Escena y carpetas previstas

```
Assets/
  Scenes/
    Table.unity
  Scripts/
    Puzzle/
      PuzzleSession.cs
      PuzzleGenerator.cs
      PuzzlePiece.cs
      PieceGroup.cs
      SnapSystem.cs
    View/
      PieceMeshBuilder.cs
      TableLayout.cs
      DragController.cs
    UI/
      PuzzleHud.cs
      ImageImportService.cs
    Settings/
      PuzzleSettings.cs
  Puzzle/Settings/
    Easy.asset
    Medium.asset
    Hard.asset
    Expert.asset
  Art/
    TableBackground.png
    PieceShadow.png
```

`Table.unity` contiene la cámara ortográfica, el fondo de la mesa, el marco de armado, un `PuzzleSession` y el canvas del HUD. Las piezas se crean en tiempo de ejecución.

## Orden de implementación

El corte por fases, las pruebas de cada una y el orden de trabajo están en el [plan de implementación](plan-de-implementacion.md). La regla corta: el modelo de la partida existe antes que la escena, las piezas rectangulares se arrastran y encajan antes de dibujar la curva, y la elección de imagen cierra el loop.
