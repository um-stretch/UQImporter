# Quixel Importer for Unity
A tool for the Unity engine that allows you to import, build, and texture zipped assets with a single click. UQImporter auto-detects your render pipeline and dynamically adjusts to ensure the asset is properly imported for a single click, drag-and-drop experience.

![UQImporter-Demo](https://github.com/user-attachments/assets/b5947c24-1be8-442b-bfec-5a261ca27fef)

### Features
The following items are fully supported in the current build.
* Fully import zipped assets with a single click
* Supports 8k textures
* Auto-detects render pipeline (built-in, URP, HDRP)
* Supports opaque and transparent materials (transparency requires HDRP, other pipeline support is planned)
* Automatically generates mask map and transparency map

 ### Limitations
Support for the following items is not currently available, but may be added in the future.
* Nested renderers
* Multi-model, single-texture assets
* Multi-model, multi-texture assets
* LODs.
* Multi-asset importing (only 1 asset can be imported at a time)

### Upcoming Features
The following items are either in-development, or plan to be added.
* Support for LODs
* Multi-asset importing
* Support for non-zipped files
* Support for nested renderers

## Tutorial
1. Import UQImporter into your project
2. Open the UQImporter window with menu item: Tools/UQImporter
3. Select a zipped asset to be imported
4. (Optional) Change its name and import destination
5. Click Import

## Config
The following items can be edited in the config.json file to adjust UQImport to your needs. 
```
pathToUQImporter - The path to the local install of UQImporter.
defualtDestinationPath - The default path assets will be imported to. Changing this also changes the default text in the Destination text box.
useNameForDestinationFolder - If true, the asset will be imported to a folder with its name.
doubleSidedMaterial - If true, the asset's material will be set to double-sided.
textureKeys - Keys that UQImporter should look for when importing and assigning textures.
logCompletionTime - If true, a messaged will be logged to the console showing the time it took to import.
pingImportedAsset - If true, the imported asset will be pinged in the Project window.
logContext - If true, will enable debugging mode.
cleanDirectory - If true, UQImporter will create folders and sort imported items.
deleteZipFile - If true, the zip file of the asset will be deleted upon import.
enableMultithreading - If true, multithreading will be enabled where applicable.
```

NOTE: You can ping your local config file by clicking {More>Ping config file} in the UQImporter window.  
NOTE: You can create a new config file by clicking {More>Create new config file} (this will delete your current config file).

### Non-Quixel assets:
Non-Quixel assets have limited support with UQImporter. You must ensure your textures use keywords defined in the config.json file in order for UQImporter to accurately recognize them.
