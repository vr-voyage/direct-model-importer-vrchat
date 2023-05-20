# /!\ ALPHA SOFTWARE. CAN BREAK AND CHANGE COMPLETELY OVER TIME /!\

# Direct Model Importer

This is currently a Proof of Concept of how to regenerate meshes encoded in EXR
inside VRChat.

This package actually provide an encoder within Unity, to encode any Mesh as
a file.

You'll still have to upload the generated EXR file somewhere (Discord being
the easiest solution) and then enter the URL inside the panel within the game.

> You can actually test it in the Editor, during 'Play' mode too.

An exporter for Blender is also available here :

https://github.com/vr-voyage/direct-model-exporter-blender

# Encoding documentation

The data are supposed to be encoded in RGBA, using 32-bits floating point
for each channel.

I have no idea how to only use one channel at the moment, so four channels
(RGBA) are used. This actually hinders the decoder implementation, but I
have no clear idea how to avoid that, beside using shader tricks that would
make the whole thing even more complex.

## Version 2

### Metadata

The first 64 bytes provide the file metadata

* Float  0 == `(float) 0x00564f59` (**VOY**)
* Float  1 == `(float) 0x00454741` (**AGE**)
* Float  2 == Float.INFINITY (*Currently unchecked*)
* Float  3 == Float.NAN (*Currently unchecked*)
* Float  4 == **Version number**
* Float  8 == **Number of vertices stored**
* Float  9 == **Number of normals stored**
* Float 10 == **Number of UVs stored** (Only one UV map is stored at the moment)
* Float 11 == **Number of indices stored**

The rest is unused at the moment.

### Vertices (from index 64)

Vertices are stored in the RGB channels.
Alpha is ignored.

```csharp
        int metadataSize = 64;
        int nfloatsInColor = 4;
        int cursor = metadataSize / nfloatsInColor;
        for (int v = 0; v < nVertices; v++, cursor++)
        {
            Color currentCol = colors[cursor];
            Vector3 vertex = new Vector3(
                currentCol.r,
                currentCol.g,
                currentCol.b);
            vertices[v] = vertex;
        }
```

### Normals (after the vertices)

Normals are stored in the RGB channels.
Alpha is ignored.

```csharp
        for (int n = 0; n < nNormals; n++, cursor++)
        {
            Color currentCol = colors[cursor];
            Vector3 normal = new Vector3(
                currentCol.r,
                currentCol.g,
                currentCol.b);
            normals[n] = normal;
        }
```

### UVS (after the normals)

UV coordinates are stored in the RG channels.
Blue and Alpha are ignored.

```csharp
        for (int u = 0; u < nUVS; u++)
        {
            Color currentCol = colors[cursor];
            Vector2 uv = new Vector2(
                currentCol.r,
                currentCol.g);
            uvs[u] = uv;
            cursor++;
        }
```

### Indices (after the UV)

Indices are packed.
The easiest way is to read 'nIndices / (float values in Color)' values.  
Then, at the end, read the remaining parts depending on how much data
is left.

```csharp
    /* This rounds the value to a multiple of 4.
     * There are way smarter ways to do it, I'm just lazy
     */
    int alignedIndices = nIndices / nfloatsInColor * nfloatsInColor;
    int currentIndex = 0;
    for (; currentIndex < alignedIndices; currentIndex += 4, cursor++)
    {

        Color currentCol = colors[cursor];
        /*Debug.Log($"Index : {currentIndex} ({cursor*4}) - Indices : {(int)currentCol.r},{(int)currentCol.g},{(int)currentCol.b},{(int)currentCol.a}");*/
        indices[currentIndex + 0] = (int)currentCol.r;
        indices[currentIndex + 1] = (int)currentCol.g;
        indices[currentIndex + 2] = (int)currentCol.b;
        indices[currentIndex + 3] = (int)currentCol.a;
    }
    /*Debug.Log($"cursor after indices : {cursor - start} ({(cursor - start) * 4})");*/
    int remainingIndices = nIndices - alignedIndices;
    if (remainingIndices > 0)
    {
        Color currentCol = colors[cursor];
        if (remainingIndices >= 1)
        {
            indices[currentIndex] = (int)currentCol.r;
            currentIndex++;
        }
        if (remainingIndices >= 2)
        {
            indices[currentIndex] = (int)currentCol.g;
            currentIndex++;
        }
        if (remainingIndices == 3)
        {
            indices[currentIndex] = (int)currentCol.b;
            currentIndex++;
        }
    }
```