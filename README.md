# DSP_CustomBuildings
An example how to add a custom model with your mesh and animations

In the Editor folder you can find needed scripts to bake MeshDataAsset and Verta file. 
Export Asset bundles script used to force unity to build asset bundles. 
BetterMeshDataAsset makes sure that if you open MeshDataAsset in inspector nothing will break(Also is used to bake mesh data from Mesh object). 
AnimationBakerWindow is the baker.

## Steps on making this all work
1. Create unity project and either put Assembly-CSharp to reference in scripts or use ThunderKit to automate that.
2. Extract needed assets(Models, animations, prefabs, etc)
3. Fix extracted prefab so that there is no missing scripts and all meshes and materials(Although i'm still unable to compile ingame's shaders) are there
4. Make sure that animation clip works, by dragging fixed prefab into preview window(It should animate it)
5. Open baker Window->Verta Animation Baker and select prefabs root, enter name and hit bake. You will get MeshDataAsset and .verta file. Both are needed to make this work
6. Write code and import everything in.

Project view:
![Project view](https://i.imgur.com/RULexSP.png)

## Installation
1. Download and unpack [BepInEx](https://github.com/BepInEx/BepInEx/releases) into game root directory
2. Download and install [LDBTool](https://dsp.thunderstore.io/package/xiaoye97/LDBTool/)
3. Download and install [DSP_CustomBuildings](https://github.com/kremnev8/DSP_CustomBuildings/releases)