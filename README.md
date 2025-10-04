# Particle Grass System
Inspired by grass system of Ghost Of Tsushima. Only compatible with URP. HDRP is now WIP.

## Features

1. Paticle system, which creates individual mesh for each blade of grass
2. Wind system and quadratic bezier curve calculation for each blade, which gives realistic bend of grass
3. Shader Graph integration, so the shader is fully customizable
4. Grass editor, full artistic customization of grass, up to 256 types of grass available to create and edit
5. Interaction system with multiple types of interactors (each with it's own behaviour and effect on grass)

![1004_1](https://github.com/user-attachments/assets/897cf577-8339-4432-9b33-4c179b8bf028)

https://github.com/user-attachments/assets/6bad84f0-2839-4bd4-ae69-52048d0c1b50

## Performance

I have tested the system on my Macbook 2023 with M2 chip. It takes about 7.2 milliseconds overall in worst case (1M+ grass blades rendered), 2.2 ms for compute shaders and 5 ms for indirect calls. 8 ms averall if scan enabled (in worst case when it only does frustum culling and no size culling). I have also tested the system on a PC with Nvidia Geforce 3070 TI. It takes about 2.4 milliseconds on average without sync compute shaders and 2.1 with them. 

## Principle Of Work

First step is subdividing grass surface (a component which is defined by Heightmap and Bounds) into chunks. Chunk system allows us to bypass the Structured Buffer size limits, 
helps with LODs and culling, it also allows us to make grass density dynamic. Chunks closer to camera are subdivided by principle of an octree. In the end we also frustum cull the chunks.

<img width="1470" height="757" alt="chunks_showcase" src="https://github.com/user-attachments/assets/a56154ae-e3b6-4c92-b2ff-434d6b5cdc5c" />

Next, for each chunk we dispatch a compute shader. Compute shaders are called in batches of variable size N (batch size can be changed, although it doesn't really 
affect performance if sync compute shaders are not available on the platform, so it should be as low as possible). 
Realtime calculation of grass data with compute shaders allows us not to save data statically, which would occupy a 
huge amount of graphics memory. Batching allows us only to have N buffers, one for each chunk in batch.\
Each grass blade takes 13 * 4 = 52 bytes of memory. For density 100 it is 100 * 100 * 52 = 520000 bytes ~ 0.5 MB per each chunk.

```
struct GrassData
{
  float3 position;
  float2 bitangent;
  float tilt;
  float bend;
  float scale;
  float width;
  float windIntensity;
  float phaseOffset;
  uint type;
  uint hash;
};

```
We are not culling the blades with size of 0 or additionally frustum cull blades in compute shader. I have tested the system with blelloch scan and the performance impact in 90% of cases is negative. 
It takes too long to scan and compact invisible blades, so it doesnt give any boost in performance. Chunks frustum culling is enough to make system performant.

We randomly offset the position on XZ plane. Sample type from external texture, which belongs to grass surface. Each type of grass has artistic parameters, 
which affect blades clumping and other parameters. We randomly set blades tilt, bend, scale and width (all affected by ranges, set up in artistic buffer, tilt and bend also affected by wind strength). 
We also generate bitangent of the blade. It is affected by wind (strength of wind affect is controlled by artistic parameter) and artistic parameters of the blade (define the range of rotation based on wind direction for surface). 
We set up a random hash and phase offset. Hash allows us to add variations in fragment shader. Phase offset is used for wind bobbing of the tip of the blade, to make each
blades movement unique. Wind texture is generated for each surface using 4 octaves of perlin noise beforehand.

![ezgif-3aab2fe08873e0](https://github.com/user-attachments/assets/5bd2252a-374e-49d7-ba6c-db54bdf54e59)

Next we call Render Mesh Instanced Indirect draw call for each chunk in batch. In vertex shader we rebuild the mesh, using Quadratic Bezier curve. We sample vertex position, tangent
and rebuild the normal as a cross product of previous 2 vectors (it is not used in lighting, but is used for wind bobbing of the tip). Then we offset the vertex by bitangent we have
generated in compute shader for blade.

<img width="438" height="378" alt="Screenshot 2025-10-04 at 13 56 08" src="https://github.com/user-attachments/assets/c8773b53-e8f5-4a7b-9e78-80e8097037cc" />

We also rotate the blade (only bitangent, not the bezier curve) slightly towards camera. It allows us to make grass field look more filled with grass without adding grass blades. And prevents blades from looking completely
flat for viewer at 90 degrees angle.

<img width="1324" height="678" alt="Screenshot 2025-10-04 at 13 33 24" src="https://github.com/user-attachments/assets/6737d1cc-49b9-4c3e-854b-8fbc4d9b5547" />

Next we add wind bobbing. It is simply a combination of 2 sine waves with generated phase offset and different wavelength and amplitude (from artistic buffer).

![ezgif-85ff3fac9b80c1](https://github.com/user-attachments/assets/9b157528-836c-4aab-81ae-336708d60461)

We also add a fade of tip bobbing and basic movement (tilt, bend and bitangent rotation) at some distance (from artistic buffer), otherwise distant grass flickers.

In fragment shader we sample the mask texture, common for all types of grass. Then we take color variations from artistic buffer. For lighting we take normal texture of our terrain 
(if it is a terrain). It gives us stylized lighting, although PBR lighting can also be made, but requiers custom SSS realization, as we don't have it in URP, otherwise grass doesn't really look realistic.



