# Quixel Asset Importer for Unity
A tool for the Unity engine that allows you to import, build, and texture zipped assets with a single click. UQImporter detects your render pipeline and dynamically adjusts to ensure your assets are properly imported for a drag-and-drop experience.

![UQImporter-Demo](https://github.com/user-attachments/assets/b5947c24-1be8-442b-bfec-5a261ca27fef)

### Features
The following items are fully supported in the current build.
* Fully import supported zipped assets with a single click
* 8k textures
* Multithreaded texture generation
* Render pipeline detection (built-in, URP, HDRP)
* Opaque and transparent materials (transparency requires HDRP, other pipeline support is planned)
* Mask map and transparency map generation

 ### Limitations
Support for the following items is not currently available, but may be added in the future.
* Nested renderers
* Multi-model, single-texture assets
* Multi-model, multi-texture assets
* Double-sided materials require HDRP (other pipeline support is planned)
* LODs.
* Multi-asset importing (only 1 asset can be imported at a time)
* FBX only

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
5. Click Import Asset

## Config

These variables control how UQImporter handles asset imports. Modify them as needed to adjust the import behavior.

```csharp
// Path to the local install of UQImporter.
string pathToUQImporter;  

// Path assets will be imported to. Changing this also updates the default text in the Destination text box.
string defaultDestinationPath;  

// If true, the asset will be imported to a folder with its name.
bool useNameForDestinationFolder;  

// If true, the asset's material will be set to double-sided.
bool doubleSidedMaterial;  

// Keys that UQImporter should look for when importing and assigning textures.
string[] textureKeys;  

// If true, a message will be logged to the console showing the time it took to import.
bool logCompletionTime;  

// If true, the imported asset will be pinged in the Project window.
bool pingImportedAsset;  

// If true, enables debugging mode.
bool logContext;  

// If true, UQImporter will create folders and sort imported items.
bool cleanDirectory;  

// If true, the zip file of the asset will be deleted upon import.
bool deleteZipFile;  

// If true, multithreading will be enabled where applicable.
bool enableMultithreading;  
```

NOTE: You can ping your local config file by clicking {More>Ping config file} in the UQImporter window.  
NOTE: You can create a new config file by clicking {More>Create new config file} (this will delete your current config file).

### Non-Quixel assets:
Non-Quixel assets have limited support with UQImporter. You must ensure your textures use keywords defined in the config.json file in order for UQImporter to accurately recognize them.
